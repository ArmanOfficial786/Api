using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using NexgenCosysReport.DbContext;
using NexgenCosysReport.Dtos.RequestDtos.Common;
using NexgenCosysReport.Inteface.ServiceInterface.Common;
using System.Data;

namespace NexgenCosysReport.Repository.Common
{
    public class LedgerLookupRepository : ILedgerLookupRepository
    {
        private readonly AppDbContext _context;
        private readonly IDateConverterService _dateConverter;

        public LedgerLookupRepository(AppDbContext context, IDateConverterService dateConverter)
        {
            _context = context;
            _dateConverter = dateConverter;
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
            var filter = string.Empty;

            if (!string.IsNullOrEmpty(fromDate) && !string.IsNullOrEmpty(toDate)
                && fromDate != "-1" && toDate != "-1")
            {
                string fromDateAd = await _dateConverter.BsToAdStringAsync(fromDate);
                string toDateAd = await _dateConverter.BsToAdStringAsync(toDate);

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

            return filter;
        }

        // Feeds ddlLedgerName — level 0 in the webform: account-type filter only,
        // returns distinct MainLedger values. sp_6_56_GetVoucherLedgerDetailsMainLedger
        // can return duplicate MainLedger rows (one per SubLedger), so this dedups in C#
        // since I don't have the SP body to confirm whether it already does so.
        public async Task<List<LedgerNameRowDto>> GetLedgerNamesAsync(LedgerNameRequestDto request)
        {
            var sqlFilterExp = await BuildSqlFilterExp(request.FromDate, request.ToDate, request.BranchId, request.AccountTypeId);

            var connectionString = _context.Database.GetConnectionString();
            await using var connection = new SqlConnection(connectionString);

            var parameters = new DynamicParameters();
            parameters.Add("@SqlFilterExp", sqlFilterExp);
            parameters.Add("@SqlFilterExpFilter", string.Empty);
            parameters.Add("@SqlFilterExpOrderBy", string.Empty);
            parameters.Add("@Level", 0);

            var rows = await connection.QueryAsync<LedgerNameRowDto>(
                "sp_6_56_GetVoucherLedgerDetailsMainLedger",
                parameters,
                commandType: CommandType.StoredProcedure,
                commandTimeout: 120
            );

            return rows
                .Where(r => !string.IsNullOrEmpty(r.MainLedger))
                .Select(r => r.MainLedger!)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(m => m)
                .Select(m => new LedgerNameRowDto { MainLedger = m })
                .ToList();
        }

        // Feeds ddlSubLedgerName — level 1 in the webform: account-type filter plus
        // "MainLedger = @MainLedger", returns distinct SubLedger1 values.
        public async Task<List<SubLedgerNameRowDto>> GetSubLedgerNamesAsync(SubLedgerNameRequestDto request)
        {
            var sqlFilterExp = await BuildSqlFilterExp(request.FromDate, request.ToDate, request.BranchId, request.AccountTypeId);

            // Level-1 filter matches the exact pattern in GetVoucherLedgerDetailsMainLedger:
            // " And MainLedger = N'<ledger>'" — parameterized here instead of concatenated.
            const string sqlFilterExpFilter = " And MainLedger = @MainLedger";

            var connectionString = _context.Database.GetConnectionString();
            await using var connection = new SqlConnection(connectionString);

            var parameters = new DynamicParameters();
            parameters.Add("@SqlFilterExp", sqlFilterExp);
            parameters.Add("@SqlFilterExpFilter", sqlFilterExpFilter);
            parameters.Add("@SqlFilterExpOrderBy", string.Empty);
            parameters.Add("@Level", 1);
            parameters.Add("@MainLedger", request.MainLedger);

            var rows = await connection.QueryAsync<SubLedgerNameRowDto>(
                "sp_6_56_GetVoucherLedgerDetailsMainLedger",
                parameters,
                commandType: CommandType.StoredProcedure,
                commandTimeout: 120
            );

            return rows
                .Where(r => !string.IsNullOrEmpty(r.SubLedger1))
                .Select(r => r.SubLedger1!)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(s => s)
                .Select(s => new SubLedgerNameRowDto { SubLedger1 = s })
                .ToList();
        }
    }
}