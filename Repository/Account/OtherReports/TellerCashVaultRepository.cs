// Repositories/Implementations/AccountOperation/TellerCashVaultRepository.cs
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using NexgenCosysReport.DbContext;
using NexgenCosysReport.Dtos.RequestDtos.Account.OtherReports;
using NexgenCosysReport.Inteface.ServiceInterface.Account.OtherReports;
using NexgenCosysReport.Inteface.ServiceInterface.Common;
using System.Data;
using System.Text;

namespace NexgenCosysReport.Repository.Account.OtherReports
{
    public class TellerCashVaultRepository : ITellerCashVault
    {
        private readonly AppDbContext _context;
        private readonly IDateConverterService _dateConverter;
        private readonly ILogger<TellerCashVaultRepository> _logger;

        public TellerCashVaultRepository(
            AppDbContext context,
            IDateConverterService dateConverter,
            ILogger<TellerCashVaultRepository> logger)
        {
            _context = context;
            _dateConverter = dateConverter;
            _logger = logger;
        }

        // --------------------------------------------------------------
        // request.Type is a bool on the actual DTO (matching the controller's
        // own "request.Type ? ToVault : FromVault" logic) — NOT a string, so
        // the previous string-keyed ProcedureMap/ResolveProcedureName(string)
        // could never have compiled against this DTO. Resolve directly off
        // the bool instead, and report back the same "ToVault"/"FromVault"
        // label the controller computes locally, so data.ReportType matches
        // what the controller expects to hand to the view.
        // --------------------------------------------------------------
        private static string ResolveProcedureName(bool type) =>
            type ? "sp_6_56_GetTellerCashToVault" : "sp_6_56_GetTellerCashFromVault";

        private static string ResolveReportTypeLabel(bool type) =>
            type ? "ToVault" : "FromVault";

        // --------------------------------------------------------------
        // @SqlFilterExp
        // Appended inside SP as: WHERE t.TellerCashReturned = 1 AND t.IsActive=1 + @SqlFilterExp
        // Uses t.TransectionOn and t.UsmOfficeId; also carries ORDER BY
        // since both SPs take a single filter/orderby-combined parameter.
        // --------------------------------------------------------------
        private async Task<string> BuildSqlFilterExp(TellerCashVaultRequestDto request)
        {
            var filter = new StringBuilder();

            if (!string.IsNullOrEmpty(request.FromDateBs) && !string.IsNullOrEmpty(request.ToDateBs)
                && request.FromDateBs != "-1" && request.ToDateBs != "-1")
            {
                var fromDateAd = await _dateConverter.NepaliToEnglishAsync(request.FromDateBs);
                var toDateAd = await _dateConverter.NepaliToEnglishAsync(request.ToDateBs);

                // ISO format avoids SQL Server regional/language ambiguity for string->datetime literals
                var fromDateStr = fromDateAd.ToString("yyyy-MM-dd");
                var toDateStr = toDateAd.ToString("yyyy-MM-dd");

                filter.Append(" And t.TransectionOn between '")
                      .Append(fromDateStr).Append("' And '")
                      .Append(toDateStr).Append("' ");
            }

            // "Same Company Name" checked => branch filter is dropped entirely (branchId = -1),
            // matching the legacy WebForm: chkSameCompanyName.Checked == true -> branchId = -1
            if (!request.SameCompanyName)
            {
                var branchIds = SanitizeBranchIds(request.BranchId);
                if (branchIds != "-1")
                {
                    filter.Append(" And t.UsmOfficeId in (").Append(branchIds).Append(") ");
                }
            }

            filter.Append(BuildSqlOrderBy(request));

            return filter.ToString();
        }

        // --------------------------------------------------------------
        // ORDER BY — column names match SP's final SELECT aliases.
        // Grouped by TellerName first so the report renders cleanly
        // teller-by-teller regardless of the secondary sort chosen.
        // --------------------------------------------------------------
        private static string BuildSqlOrderBy(TellerCashVaultRequestDto request)
        {
            if (string.IsNullOrEmpty(request.OrderBy) ||
                request.OrderBy == "-1" ||
                request.OrderBy == "string")
            {
                return " order by TellerName, Date";
            }

            return request.OrderBy.Trim().ToLower() switch
            {
                "teller name" => " order by TellerName",
                "date" => " order by TellerName, Date",
                "amount" => " order by TellerName, Amount",
                "issued by" => " order by TellerName, FromVaultBy",
                "returned by" => " order by TellerName, FromVaultBy",
                _ => " order by TellerName, Date"
            };
        }

        // Guards against injection through the comma-separated branch id list
        private static string SanitizeBranchIds(string? branchIds)
        {
            if (string.IsNullOrWhiteSpace(branchIds) || branchIds == "-1" || branchIds == "string")
                return "-1";

            var validIds = branchIds
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Where(id => long.TryParse(id, out _));

            var joined = string.Join(",", validIds);
            return string.IsNullOrEmpty(joined) ? "-1" : joined;
        }

        public async Task<TellerCashVaultData> GetReportDataAsync(TellerCashVaultRequestDto request)
        {
            try
            {
                var procedureName = ResolveProcedureName(request.Type);
                var resolvedType = ResolveReportTypeLabel(request.Type);

                var sqlFilterExp = await BuildSqlFilterExp(request);

                var connectionString = _context.Database.GetConnectionString();
                using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();

                var parameters = new DynamicParameters();
                parameters.Add("@SqlFilterExp", sqlFilterExp, DbType.String, size: -1);

                var rows = await connection.QueryAsync<TellerCashVaultRowDto>(
                    procedureName,
                    parameters,
                    commandType: CommandType.StoredProcedure,
                    commandTimeout: 120
                );

                var resultList = rows.AsList();

                var branchNames = resultList
                    .Select(r => r.OfficeName)
                    .Where(n => !string.IsNullOrWhiteSpace(n))
                    .Distinct()
                    .ToList();

                return new TellerCashVaultData
                {
                    Rows = resultList,
                    TotalRecords = resultList.Count,
                    TotalAmount = resultList.Sum(r => r.Amount ?? 0),
                    FromDateBs = request.FromDateBs,
                    ToDateBs = request.ToDateBs,
                    BranchNames = request.SameCompanyName
                        ? "All"
                        : (branchNames.Count > 0 ? string.Join(", ", branchNames) : "All Branches"),
                    OrderBy = request.OrderBy,
                    ReportType = resolvedType // matches the controller's own "ToVault"/"FromVault" label
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetReportDataAsync for Type={Type}", request.Type);
                throw;
            }
        }
    }
}