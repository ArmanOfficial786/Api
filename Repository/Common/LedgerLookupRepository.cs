using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using NexgenCosysReport.DbContext;
using NexgenCosysReport.Dtos.RequestDtos.Common;
using NexgenCosysReport.Inteface.ServiceInterface.Common;
using System.Data;
using System.Diagnostics;

namespace NexgenCosysReport.Repository.Common
{
    public class LedgerLookupRepository : ILedgerLookupRepository
    {
        private readonly AppDbContext _context;
        private readonly IDateConverterService _dateConverter;
        private readonly ILogger<LedgerLookupRepository> _logger;

        public LedgerLookupRepository(AppDbContext context, IDateConverterService dateConverter, ILogger<LedgerLookupRepository> logger)
        {
            _context = context;
            _dateConverter = dateConverter;
            _logger = logger;
        }

        // Mirrors CAcoAccountType.GetAll() — feeds ddlLedgerHead in the webform.
        public async Task<List<LedgerHeadRowDto>> GetLedgerHeadsAsync()
        {
            var connectionString = _context.Database.GetConnectionString();
            await using var connection = new SqlConnection(connectionString);

            const string sql = @"
                SELECT AcoAccountTypeId, AccountType
                FROM AcoAccountType
                ORDER BY AccountType";

            var rows = await connection.QueryAsync<LedgerHeadRowDto>(sql);
            return rows.ToList();
        }

        // --------------------------------------------------------------
        // Shared filter builder — matches CAcoVoucherPosting.GetVoucherLedgerDetailsMainLedger's
        // SqlFilterExp construction (date range, branch, account type), minus the
        // fiscal-year-spanning "forNewSp" branch (see method-level note in the caller).
        // --------------------------------------------------------------
        private async Task<string> BuildSqlFilterExp(string fromDate, string toDate, string? branchId, int accountTypeId)
        {
            var stopwatch = Stopwatch.StartNew();
            var filter = string.Empty;

            try
            {
                if (!string.IsNullOrEmpty(fromDate) && !string.IsNullOrEmpty(toDate)
                    && fromDate != "-1" && toDate != "-1")
                {
                    var dateStopwatch = Stopwatch.StartNew();
                    string fromDateAd = await _dateConverter.BsToAdStringAsync(fromDate);
                    string toDateAd = await _dateConverter.BsToAdStringAsync(toDate);
                    dateStopwatch.Stop();
                    _logger.LogInformation("Date conversion: {FromDate} -> {FromDateAd}, {ToDate} -> {ToDateAd} in {ElapsedMs}ms", 
                        fromDate, fromDateAd, toDate, toDateAd, dateStopwatch.ElapsedMilliseconds);

                    if (!string.IsNullOrEmpty(fromDateAd) && !string.IsNullOrEmpty(toDateAd))
                    {
                        filter += $" AND v.VoucherOn BETWEEN '{fromDateAd}' AND '{toDateAd}'";
                    }
                }

                if (!string.IsNullOrEmpty(branchId) && branchId != "-1" && branchId != "string"
                    && long.TryParse(branchId, out var parsedBranchId))
                {
                    filter += $" AND v.UsmOfficeId = {parsedBranchId}";
                }

                if (accountTypeId != -1)
                {
                    filter += $" AND l.AcoAccountTypeId = {accountTypeId}";
                }

                stopwatch.Stop();
                _logger.LogInformation("BuildSqlFilterExp completed in {ElapsedMs}ms. Final filter: {Filter}", 
                    stopwatch.ElapsedMilliseconds, filter);

                return filter;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "BuildSqlFilterExp failed after {ElapsedMs}ms", stopwatch.ElapsedMilliseconds);
                throw;
            }
        }

        // Feeds ddlLedgerName — level 0 in the webform: account-type filter only,
        // returns distinct MainLedger values. sp_6_56_GetVoucherLedgerDetailsMainLedger
        // can return duplicate MainLedger rows (one per SubLedger), so this dedups in C#
        // since I don't have the SP body to confirm whether it already does so.
        public async Task<List<LedgerNameRowDto>> GetLedgerNamesAsync(LedgerNameRequestDto request)
        {
            var stopwatch = Stopwatch.StartNew();
            try
            {
                _logger.LogInformation("GetLedgerNamesAsync called with: FromDate={FromDate}, ToDate={ToDate}, BranchId={BranchId}, AccountTypeId={AccountTypeId}", 
                    request.FromDate, request.ToDate, request.BranchId, request.AccountTypeId);

                var filterStopwatch = Stopwatch.StartNew();
                var sqlFilterExp = await BuildSqlFilterExp(request.FromDate, request.ToDate, request.BranchId, request.AccountTypeId);
                filterStopwatch.Stop();
                _logger.LogInformation("BuildSqlFilterExp completed in {ElapsedMs}ms. Filter: {Filter}", 
                    filterStopwatch.ElapsedMilliseconds, sqlFilterExp);

                var connectionString = _context.Database.GetConnectionString();
                await using var connection = new SqlConnection(connectionString);

                var parameters = new DynamicParameters();
                parameters.Add("@SqlFilterExp", sqlFilterExp);
                parameters.Add("@SqlFilterExpFilter", string.Empty);
                parameters.Add("@SqlFilterExpOrderBy", string.Empty);
                parameters.Add("@Level", 0);

                var queryStopwatch = Stopwatch.StartNew();
                var rows = await connection.QueryAsync<LedgerNameRowDto>(
                    "sp_6_56_GetVoucherLedgerDetailsMainLedger",
                    parameters,
                    commandType: CommandType.StoredProcedure,
                    commandTimeout: 120
                );
                queryStopwatch.Stop();
                _logger.LogInformation("Stored procedure executed in {ElapsedMs}ms. Rows returned: {RowCount}", 
                    queryStopwatch.ElapsedMilliseconds, rows.Count());

                var processStopwatch = Stopwatch.StartNew();
                var result = rows
                    .Where(r => !string.IsNullOrEmpty(r.MainLedger))
                    .Select(r => r.MainLedger!)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(m => m)
                    .Select(m => new LedgerNameRowDto { MainLedger = m })
                    .ToList();
                processStopwatch.Stop();
                _logger.LogInformation("C# processing completed in {ElapsedMs}ms. Final result count: {ResultCount}", 
                    processStopwatch.ElapsedMilliseconds, result.Count);

                stopwatch.Stop();
                _logger.LogInformation("GetLedgerNamesAsync completed in {TotalMs}ms", stopwatch.ElapsedMilliseconds);

                return result;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "GetLedgerNamesAsync failed after {ElapsedMs}ms", stopwatch.ElapsedMilliseconds);
                throw;
            }
        }

        // Feeds ddlSubLedgerName — level 1 in the webform: account-type filter plus
        // "MainLedger = @MainLedger", returns distinct SubLedger1 values.
        // Feeds ddlSubLedgerName — level 1 in the webform: account-type filter plus
        // "MainLedger = @MainLedger", returns distinct SubLedger1 values.
        public async Task<List<SubLedgerNameRowDto>> GetSubLedgerNamesAsync(SubLedgerNameRequestDto request)
        {
            var stopwatch = Stopwatch.StartNew();
            try
            {
                _logger.LogInformation("GetSubLedgerNamesAsync called with: FromDate={FromDate}, ToDate={ToDate}, BranchId={BranchId}, AccountTypeId={AccountTypeId}, MainLedger={MainLedger}", 
                    request.FromDate, request.ToDate, request.BranchId, request.AccountTypeId, request.MainLedger);

                var filterStopwatch = Stopwatch.StartNew();
                var sqlFilterExp = await BuildSqlFilterExp(request.FromDate, request.ToDate, request.BranchId, request.AccountTypeId);
                filterStopwatch.Stop();

                var escapedMainLedger = (request.MainLedger ?? string.Empty).Replace("'", "''");
                var sqlFilterExpFilter = $" And MainLedger = N'{escapedMainLedger}'";
                filterStopwatch.Stop();
                _logger.LogInformation("BuildSqlFilterExp completed in {ElapsedMs}ms. Filter: {Filter}, FilterExp: {FilterExp}", 
                    filterStopwatch.ElapsedMilliseconds, sqlFilterExp, sqlFilterExpFilter);

                var connectionString = _context.Database.GetConnectionString();
                await using var connection = new SqlConnection(connectionString);

                var parameters = new DynamicParameters();
                parameters.Add("@SqlFilterExp", sqlFilterExp);
                parameters.Add("@SqlFilterExpFilter", sqlFilterExpFilter);
                parameters.Add("@SqlFilterExpOrderBy", string.Empty);
                parameters.Add("@Level", 1);

                var queryStopwatch = Stopwatch.StartNew();
                var rows = await connection.QueryAsync<SubLedgerNameRowDto>(
                    "sp_6_56_GetVoucherLedgerDetailsMainLedger",
                    parameters,
                    commandType: CommandType.StoredProcedure,
                    commandTimeout: 120
                );
                queryStopwatch.Stop();
                _logger.LogInformation("Stored procedure executed in {ElapsedMs}ms. Rows returned: {RowCount}", 
                    queryStopwatch.ElapsedMilliseconds, rows.Count());

                var processStopwatch = Stopwatch.StartNew();
                var result = rows
                    .Where(r => !string.IsNullOrEmpty(r.SubLedger1))
                    .Select(r => r.SubLedger1!)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(s => s)
                    .Select(s => new SubLedgerNameRowDto { SubLedger1 = s })
                    .ToList();
                processStopwatch.Stop();
                _logger.LogInformation("C# processing completed in {ElapsedMs}ms. Final result count: {ResultCount}", 
                    processStopwatch.ElapsedMilliseconds, result.Count);

                stopwatch.Stop();
                _logger.LogInformation("GetSubLedgerNamesAsync completed in {TotalMs}ms", stopwatch.ElapsedMilliseconds);

                return result;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "GetSubLedgerNamesAsync failed after {ElapsedMs}ms", stopwatch.ElapsedMilliseconds);
                throw;
            }
        }
    }
}