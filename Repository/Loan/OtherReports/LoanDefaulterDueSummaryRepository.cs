// Repository/Loan/OtherReports/LoanDefaulterDueSummaryRepository.cs
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

        public LoanDefaulterDueSummaryRepository(
            AppDbContext context,
            IDateConverterService dateConverter,
            ILogger<LoanDefaulterDueSummaryRepository> logger)
        {
            _context = context;
            _dateConverter = dateConverter;
            _logger = logger;
        }

        // --------------------------------------------------------------
        // @SqlFilterExpOrderby — column names match the SP's final SELECT
        // Preserves the legacy substring-based natural sort for
        // MemberId / LoanAccountNo (strips a trailing "-N" suffix before
        // sorting) exactly as in the BLL.
        // --------------------------------------------------------------
        private static string BuildSqlOrderBy(LoanDefaulterDueSummaryRequestDto request)
        {
            if (string.IsNullOrEmpty(request.OrderBy) || request.OrderBy == "-1")
                return string.Empty;

            return request.OrderBy.Trim() switch
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

        // --------------------------------------------------------------
        // Guards against injection through the comma-separated branch id
        // list, same pattern used across the other reports.
        // --------------------------------------------------------------
        private static string SanitizeBranchIds(string? branchIds)
        {
            if (string.IsNullOrWhiteSpace(branchIds) || branchIds == "-1" || branchIds == "string")
                return string.Empty;

            var validIds = branchIds
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Where(id => long.TryParse(id, out _));

            return string.Join(",", validIds);
        }

        public async Task<LoanDefaulterDueSummaryData> GetReportDataAsync(LoanDefaulterDueSummaryRequestDto request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.TillDate) || request.TillDate == "-1")
                {
                    throw new ArgumentException("TillDate is required.");
                }

                var branchIds = SanitizeBranchIds(request.BranchIds);

                var connectionString = _context.Database.GetConnectionString();
                using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();

                // Convert BS till date to AD
                var tillDateAd = await _dateConverter.NepaliToEnglishAsync(request.TillDate);
                var tillDateStr = tillDateAd.ToString("yyyy-MM-dd");

                var sqlFilterExp = new StringBuilder();

                // --------------------------------------------------------------
                // Build filter expression
                // --------------------------------------------------------------
                if (!string.IsNullOrEmpty(branchIds))
                {
                    sqlFilterExp.Append(" And LS.UsmOfficeId in(").Append(branchIds).Append(")");
                }

                if (!string.IsNullOrEmpty(request.CollectionCenterId) && request.CollectionCenterId != "-1")
                {
                    sqlFilterExp.Append(" And LS.SycCollectionCenterId in(").Append(request.CollectionCenterId).Append(")");
                }

                if (!string.IsNullOrEmpty(request.CollectorId) && request.CollectorId != "-1")
                {
                    sqlFilterExp.Append(" and LS.HurCollectorId = ").Append(request.CollectorId);
                }

                var sqlFilterExpOrderby = new StringBuilder();
                var orderByClause = BuildSqlOrderBy(request);
                if (!string.IsNullOrEmpty(orderByClause))
                {
                    sqlFilterExpOrderby.Append(" order by ").Append(orderByClause);
                }

                // The SP accepts @sqltiidate as a quoted string literal
                var sqltiidate = $"'{tillDateStr}'";

                var parameters = new DynamicParameters();
                parameters.Add("@SqlFilterExp", sqlFilterExp.ToString(), DbType.String, size: -1);
                parameters.Add("@SqlFilterExpOrderby", sqlFilterExpOrderby.ToString(), DbType.String, size: -1);
                parameters.Add("@sqltiidate", sqltiidate, DbType.String, size: -1);

                // Choose the SP based on report type
                var spName = request.ReportType == "LDTPR"
                    ? "sp_7_16_LoanDefaulterDueSummaryTobePaid"
                    : "sp_7_16_LoanDefaulterDueSummary";

                var rows = await connection.QueryAsync<LoanDefaulterDueSummaryRowDto>(
                    spName,
                    parameters,
                    commandType: CommandType.StoredProcedure,
                    commandTimeout: 120
                );

                var resultList = rows.AsList();

                // Get branch names for display
                string branchName = "All";
                if (!string.IsNullOrEmpty(branchIds))
                {
                    var names = await connection.QueryAsync<string>(
                        $"SELECT OfficeName FROM UsmOffice WHERE UsmOfficeId IN ({branchIds})");
                    var nameList = names.Where(n => !string.IsNullOrWhiteSpace(n)).ToList();
                    branchName = nameList.Count > 0 ? string.Join(", ", nameList) : "All";
                }

                // Get collection center name for display
                string? collectionCenterName = null;
                if (!string.IsNullOrEmpty(request.CollectionCenterId) && request.CollectionCenterId != "-1")
                {
                    collectionCenterName = await connection.QueryFirstOrDefaultAsync<string>(
                        $"SELECT STRING_AGG(CollectionCenterName, ', ') FROM SycCollectionCenter WHERE SycCollectionCenterId IN ({request.CollectionCenterId})");
                }

                // Get collector name for display
                string? collectorName = null;
                if (!string.IsNullOrEmpty(request.CollectorId) && request.CollectorId != "-1")
                {
                    collectorName = await connection.QueryFirstOrDefaultAsync<string>(
                        "SELECT CollectorFullName FROM HurCollector WHERE HurCollectorId = @Id",
                        new { Id = request.CollectorId });
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
                    ReportType = request.ReportType,
                    OrderBy = request.OrderBy
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetReportDataAsync");
                throw;
            }
        }
    }
}