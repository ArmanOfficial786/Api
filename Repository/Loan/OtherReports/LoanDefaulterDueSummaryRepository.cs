using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using NexgenCosysReport.DbContext;
using NexgenCosysReport.Dtos.RequestDtos.Loan.OtherReports;
using NexgenCosysReport.Inteface.ServiceInterface.Common;
using NexgenCosysReport.Inteface.ServiceInterface.Loan.OtherReports;
using System.Data;
using System.Text;

namespace NexgenCosysReport.Repository.Loan.OtherReports
{
    public class LoanDefaulterDueSummaryRepository : ILoanDefaulterDueSummaryRepository
    {
        private readonly AppDbContext _context;
        private readonly IDateConverterService _dateConverter;
        private readonly ILogger<LoanDefaulterDueSummaryRepository> _logger;

        // The SPs run dynamic SQL (and LDTPR calls scalar UDFs per row), so they can
        // exceed ADO.NET's 30s default on large branches.
        private const int ReportCommandTimeoutSeconds = 180;

        public LoanDefaulterDueSummaryRepository(
            AppDbContext context,
            IDateConverterService dateConverter,
            ILogger<LoanDefaulterDueSummaryRepository> logger)
        {
            _context = context;
            _dateConverter = dateConverter;
            _logger = logger;
        }

        public async Task<LoanDefaulterDueSummaryData> GetReportDataAsync(LoanDefaulterDueSummaryRequestDto request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.TillDate) || request.TillDate == "-1")
                    throw new ArgumentException("TillDate is required.");

                // WebForms: "Please select Branch Name" when nothing is selected
                if (string.IsNullOrWhiteSpace(request.BranchIds))
                    throw new ArgumentException("Please select Branch Name");

                var branchIds = SanitizeIdList(request.BranchIds);
                var collectionCenterIds = SanitizeIdList(request.CollectionCenterId);
                var collectorId = ParseSingleId(request.CollectorId);
                var isTobePaid = IsTobePaid(request.ReportType);

                var connectionString = _context.Database.GetConnectionString();
                if (string.IsNullOrEmpty(connectionString))
                    throw new InvalidOperationException("Database connection string is not configured.");

                using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();

                // No silent fallback to today's date: a bad date must fail, as in WebForms.
                var tillDateAd = await _dateConverter.NepaliToEnglishAsync(request.TillDate);
                var tillDateStr = tillDateAd.ToString("yyyy-MM-dd");

                // ---- filters, same order as CLmtLoanIssueReports ----
                var sqlFilterExp = new StringBuilder();

                // LDR (Schedulewise): till date is part of @SqlFilterExp.
                // LDTPR (Till Date): the date goes only in @sqltiidate.
                if (!isTobePaid)
                    sqlFilterExp.Append(" and Tr.ScheduleDateOn <= '").Append(tillDateStr).Append("'");

                if (!string.IsNullOrEmpty(branchIds))
                    sqlFilterExp.Append(" And LS.UsmOfficeId in(").Append(branchIds).Append(")");

                if (!string.IsNullOrEmpty(collectionCenterIds))
                    sqlFilterExp.Append(" And LS.SycCollectionCenterId in(").Append(collectionCenterIds).Append(")");

                if (collectorId.HasValue)
                    sqlFilterExp.Append(" and LS.HurCollectorId = ").Append(collectorId.Value);

                // ---- order by (same as the WebForms switch) ----
                var orderByClause = MapOrderBy(request.OrderBy);
                var sqlFilterExpOrderby = string.IsNullOrEmpty(orderByClause)
                    ? string.Empty
                    : " order by " + orderByClause;

                // LDR SP takes 2 parameters; LDTPR takes the extra @sqltiidate.
                var spName = isTobePaid
                    ? "sp_7_16_LoanDefaulterDueSummaryTobePaid"
                    : "sp_7_16_LoanDefaulterDueSummary";

                var parameters = new DynamicParameters();
                parameters.Add("@SqlFilterExp", sqlFilterExp.ToString(), DbType.String, size: -1);
                parameters.Add("@SqlFilterExpOrderby", sqlFilterExpOrderby, DbType.String, size: -1);

                if (isTobePaid)
                    parameters.Add("@sqltiidate", $"'{tillDateStr}'", DbType.String, size: -1);

                _logger.LogInformation(
                    "LoanDefaulterDueSummary -> SP: {SpName}, Filter: {Filter}, OrderBy: {OrderBy}",
                    spName, sqlFilterExp.ToString(), sqlFilterExpOrderby);

                List<LoanDefaulterDueSummaryRowDto> resultList;
                try
                {
                    var rows = await connection.QueryAsync<LoanDefaulterDueSummaryRowDto>(
                        spName,
                        parameters,
                        commandType: CommandType.StoredProcedure,
                        commandTimeout: ReportCommandTimeoutSeconds);

                    resultList = rows.AsList();
                }
                catch (SqlException ex) when (ex.Number == -2 || ex.Message.Contains("Timeout"))
                {
                    _logger.LogError(ex, "LoanDefaulterDueSummary SP timed out after {Timeout}s", ReportCommandTimeoutSeconds);
                    throw new TimeoutException(
                        $"The Loan Defaulter Due Summary report timed out after {ReportCommandTimeoutSeconds}s. " +
                        "Try narrowing the branch/date filters.", ex);
                }

                _logger.LogInformation("LoanDefaulterDueSummary returned {Count} rows", resultList.Count);

                // ---- header names ----
                string branchName = "All";
                var branchIdList = ToIdList(branchIds);
                if (branchIdList.Count > 0)
                {
                    var names = await connection.QueryAsync<string>(
                        "SELECT OfficeName FROM UsmOffice WHERE UsmOfficeId IN @Ids",
                        new { Ids = branchIdList });
                    var nameList = names.Where(n => !string.IsNullOrWhiteSpace(n)).ToList();
                    branchName = nameList.Count > 0 ? string.Join(", ", nameList) : "All";
                }

                string? collectionCenterName = null;
                var ccIdList = ToIdList(collectionCenterIds);
                if (ccIdList.Count > 0)
                {
                    var ccNames = await connection.QueryAsync<string>(
                        "SELECT CollectionCenterName FROM SycCollectionCenter WHERE SycCollectionCenterId IN @Ids",
                        new { Ids = ccIdList });
                    var ccNameList = ccNames.Where(n => !string.IsNullOrWhiteSpace(n)).ToList();
                    collectionCenterName = ccNameList.Count > 0 ? string.Join(", ", ccNameList) : null;
                }

                string? collectorName = null;
                if (collectorId.HasValue)
                {
                    collectorName = await connection.QueryFirstOrDefaultAsync<string>(
                        "SELECT CollectorFullName FROM HurCollector WHERE HurCollectorId = @Id",
                        new { Id = collectorId.Value });
                }

                return new LoanDefaulterDueSummaryData
                {
                    Rows = resultList,
                    TotalRecords = resultList.Count,
                    TotalLoanIssueAmount = resultList.Sum(r => r.LoanIssueAmount ?? 0),
                    TotalInterest = resultList.Sum(r => r.Interest ?? 0),
                    TotalPrincipleAmount = resultList.Sum(r => r.PrincipleAmount ?? 0),
                    TotalInstallmentAmount = resultList.Sum(r => r.InstallamentAmount ?? 0),
                    TillDate = request.TillDate,
                    BranchName = branchName,
                    CollectionCenterName = collectionCenterName,
                    CollectorName = collectorName,
                    ReportType = isTobePaid ? "LDTPR" : "LDR",
                    OrderBy = request.OrderBy
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetReportDataAsync (LoanDefaulterDueSummary)");
                throw;
            }
        }

        // WebForms radio values: "LDR" (Schedulewise) and "LDT<hidden char>PR" (Till Date).
        // Strip everything but letters so the hidden character cannot break the match.
        private static bool IsTobePaid(string? reportType)
        {
            if (string.IsNullOrWhiteSpace(reportType))
                return false;

            var v = new string(reportType.Where(char.IsLetter).ToArray()).ToUpperInvariant();

            if (v == "LDR")
                return false;

            return v.StartsWith("LDT") || v.Contains("TPR") || v.Contains("TOBEPAID");
        }

        // Same values and SQL text as the ddlorderBy switch in the WebForm.
        private static string MapOrderBy(string? orderBy)
        {
            if (string.IsNullOrWhiteSpace(orderBy) || orderBy == "-1")
                return string.Empty;

            return orderBy.Trim() switch
            {
                "MemberId" => " substring(MemberId, 1,(len(MemberId)-charindex('-', MemberId))-1), MemberId ",
                "FullName" => "FullName",
                "LoanAccountNo" => " substring(LoanAccountNo, 1,(len(LoanAccountNo)-charindex('-', LoanAccountNo))-1), LoanAccountNo ",
                "LoanTypeName" => " LoanTypeName",
                "PrincipleAmount" => "  PrincipleAmount DESC",
                "InterestAmount" => " InterestAmount",
                "InstallmentAmount" => " InstallmentAmount",
                "DateOnBS" => " DateOnBS",
                _ => string.Empty
            };
        }

        // Positive numeric ids only; "" means "no filter".
        private static string SanitizeIdList(string? ids)
        {
            if (string.IsNullOrWhiteSpace(ids) || ids == "-1" || ids == "string")
                return string.Empty;

            var valid = ids
                .Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Where(id => long.TryParse(id, out var v) && v > 0)
                .Select(id => long.Parse(id).ToString())
                .Distinct();

            return string.Join(",", valid);
        }

        private static long? ParseSingleId(string? id)
        {
            if (string.IsNullOrWhiteSpace(id) || id == "-1" || id == "string")
                return null;

            return long.TryParse(id.Trim(), out var v) && v > 0 ? v : null;
        }

        private static List<long> ToIdList(string ids)
        {
            if (string.IsNullOrEmpty(ids))
                return new List<long>();

            return ids
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(long.Parse)
                .ToList();
        }
    }
}

