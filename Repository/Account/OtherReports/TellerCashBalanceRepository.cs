// Repositories/Implementations/MemberAccount/TellerCashBalanceRepository.cs
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using NexgenCosysReport.DbContext;
using NexgenCosysReport.Dtos.RequestDtos.MemberAccount.OthersReport;
using NexgenCosysReport.Inteface.ServiceInterface.Common;
using NexgenCosysReport.Inteface.ServiceInterface.MemberAccount.OthersReport;
using System.Data;
using System.Text;

namespace NexgenCosysReport.Repository.MemberAccount.OthersReport
{
    public class TellerCashBalanceRepository : ITellerCashBalanceRepository
    {
        private readonly AppDbContext _context;
        private readonly IDateConverterService _dateConverter;
        private readonly ILogger<TellerCashBalanceRepository> _logger;

        public TellerCashBalanceRepository(
            AppDbContext context,
            IDateConverterService dateConverter,
            ILogger<TellerCashBalanceRepository> logger)
        {
            _context = context;
            _dateConverter = dateConverter;
            _logger = logger;
        }

        // --------------------------------------------------------------
        // Resolves BranchId to a single numeric office id, or null for "All".
        // NOTE: the SP only supports a single "At.UsmOfficeId = <id>" equality
        // filter (no IN (...) list) — if BranchId contains multiple
        // comma-separated ids, only the FIRST valid one is used, and a
        // warning is logged so a frontend accidentally sending a multi-
        // select list doesn't silently filter on the wrong branch set.
        // --------------------------------------------------------------
        private long? ResolveSingleBranchId(string? branchId)
        {
            if (string.IsNullOrWhiteSpace(branchId) || branchId == "-1" || branchId == "string")
                return null;

            var validIds = branchId
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(id => long.TryParse(id, out var parsed) ? (long?)parsed : null)
                .Where(id => id.HasValue)
                .Select(id => id!.Value)
                .ToList();

            if (validIds.Count == 0)
                return null;

            if (validIds.Count > 1)
            {
                _logger.LogWarning(
                    "TellerCashBalance: BranchId '{BranchId}' contained multiple ids; " +
                    "this SP only supports a single branch, using the first: {FirstId}",
                    branchId, validIds[0]);
            }

            return validIds[0];
        }

        // --------------------------------------------------------------
        // @SqlFilterExp
        // Appended inside SP as: WHERE at.IsActive=1 + @SqlFilterExp
        // Uses At.UsmOfficeId and At.TransectionOn (aliased "At"/"at")
        // --------------------------------------------------------------
        private async Task<string> BuildSqlFilterExp(TellerCashBalanceRequestDto request, long? branchId)
        {
            var filter = new StringBuilder();

            if (branchId.HasValue)
            {
                filter.Append(" And At.UsmOfficeId = ").Append(branchId.Value);
            }

            if (!string.IsNullOrEmpty(request.FromDateBs) && !string.IsNullOrEmpty(request.ToDateBs)
                && request.FromDateBs != "-1" && request.ToDateBs != "-1")
            {
                var fromDateAd = await _dateConverter.NepaliToEnglishAsync(request.FromDateBs);
                var toDateAd = await _dateConverter.NepaliToEnglishAsync(request.ToDateBs);

                // ISO format avoids SQL Server regional/language ambiguity for string->datetime literals
                var fromDateStr = fromDateAd.ToString("yyyy-MM-dd");
                var toDateStr = toDateAd.ToString("yyyy-MM-dd");

                filter.Append(" AND At.TransectionOn between '")
                      .Append(fromDateStr).Append("' and '")
                      .Append(toDateStr).Append("'");
            }

            return filter.ToString();
        }

        // --------------------------------------------------------------
        // @SqlFilterExpOrderBy — applied to the SP's final SELECT * FROM #TEMP.
        // Column names match #TEMP's own aliases (TellerName, Date).
        // --------------------------------------------------------------
        private static string BuildSqlOrderBy(TellerCashBalanceRequestDto request)
        {
            // Default now explicitly orders by Date so the view's date-grouping is always
            // contiguous — previously this returned empty, relying on whatever order the
            // SP happened to return rows in.
            if (string.IsNullOrEmpty(request.OrderBy) ||
                request.OrderBy == "-1" ||
                request.OrderBy == "string")
            {
                return " order by Date ";
            }

            return request.OrderBy.Trim().ToLower() switch
            {
                "tellername" => " order by Date, TellerName ",
                "date" => " order by Date ",
                _ => " order by Date "
            };
        }

        public async Task<TellerCashBalanceData> GetReportDataAsync(TellerCashBalanceRequestDto request)
        {
            try
            {
                var branchId = ResolveSingleBranchId(request.BranchId);

                var sqlFilterExp = await BuildSqlFilterExp(request, branchId);
                var sqlFilterExpOrderBy = BuildSqlOrderBy(request);

                var connectionString = _context.Database.GetConnectionString();
                using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();

                var parameters = new DynamicParameters();
                parameters.Add("@SqlFilterExp", sqlFilterExp, DbType.String, size: -1);
                parameters.Add("@SqlFilterExpOrderBy", sqlFilterExpOrderBy, DbType.String, size: -1);

                var rows = await connection.QueryAsync<TellerCashBalanceRowDto>(
                    "sp_5_43_GetTellerCashBalanceReport",
                    parameters,
                    commandType: CommandType.StoredProcedure,
                    commandTimeout: 120
                );

                var resultList = rows.AsList();

                string officeName = "All";
                if (branchId.HasValue)
                {
                    var name = await connection.QueryFirstOrDefaultAsync<string>(
                        "SELECT OfficeName FROM UsmOffice WHERE UsmOfficeId = @BranchId",
                        new { BranchId = branchId.Value });

                    officeName = string.IsNullOrEmpty(name) ? "All" : name;
                }

                return new TellerCashBalanceData
                {
                    Rows = resultList,
                    TotalRecords = resultList.Count,
                    TotalClosingBalance = resultList.Sum(r => r.ClosingBalance ?? 0),
                    FromDateBs = request.FromDateBs,
                    ToDateBs = request.ToDateBs,
                    OfficeName = officeName,
                    OrderBy = request.OrderBy,
                    NepaliReport = request.NepaliReport
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetReportDataAsync");
                throw;
            }
        }

        public Task<TellerCashBalanceData> GetTellerCashBalanceDataAsync(TellerCashBalanceRequestDto request)
        {
            // Delegate to the existing implementation to avoid code duplication
            return GetReportDataAsync(request);
        }
    }
}