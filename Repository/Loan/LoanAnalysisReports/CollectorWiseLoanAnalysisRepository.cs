// Repository/Loan/LoanAnalysisReport/CollectorWiseLoanAnalysisRepository.cs
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using NexgenCosysReport.DbContext;
using NexgenCosysReport.Dtos.RequestDtos.Loan.LoanAnalysisReport;
using NexgenCosysReport.Inteface.ServiceInterface.Common;
using NexgenCosysReport.Inteface.ServiceInterface.Loan.LoanAnalysisReport;
using System.Data;

namespace NexgenCosysReport.Repository.Loan.LoanAnalysisReport
{
    public class CollectorWiseLoanAnalysisRepository : ICollectorWiseLoanAnalysisRepository
    {
        private readonly AppDbContext _context;
        private readonly IDateConverterService _dateConverter;
        private readonly ILogger<CollectorWiseLoanAnalysisRepository> _logger;

        public CollectorWiseLoanAnalysisRepository(
            AppDbContext context,
            IDateConverterService dateConverter,
            ILogger<CollectorWiseLoanAnalysisRepository> logger)
        {
            _context = context;
            _dateConverter = dateConverter;
            _logger = logger;
        }

        private static string SanitizeIdList(string? ids)
        {
            if (string.IsNullOrWhiteSpace(ids) || ids == "-1" || ids == "string")
                return "-1";

            var validIds = ids
                .Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Where(id => long.TryParse(id, out _));

            var joined = string.Join(",", validIds);
            return string.IsNullOrEmpty(joined) ? "-1" : joined;
        }

        private static string BuildSqlOrderBy(string? orderBy)
        {
            if (string.IsNullOrEmpty(orderBy) || orderBy == "-1")
                return string.Empty;

            return orderBy.Trim() switch
            {
                "LoanType" => " Order By LoanType",
                "LoanIssueAmount desc" => " Order By LoanIssueAmount desc",
                "BalanceAmount desc" => " Order By BalanceAmount desc",
                "Goodloan desc" => " Order By Goodloan desc",
                "Arrear desc" => " Order By Arrear desc",
                "Arrearfrm1to365 desc" => " Order By Arrearfrm1to365 desc",
                "Arreargrtthan365 desc" => " Order By Arreargrtthan365 desc",
                _ => string.Empty
            };
        }

        private static string GetPenaltyTypeName(string penaltyType)
        {
            return penaltyType?.Trim().ToUpper() switch
            {
                "R" => "Remaining Principle",
                "A" => "After Maturity",
                "S" => "Schedule Wise",
                _ => "Schedule Wise"
            };
        }

        private static string GetReportModeName(string reportMode)
        {
            return reportMode == "0" ? "Detail" : "Summary";
        }

        public async Task<CollectorWiseLoanAnalysisData> GetReportDataAsync(CollectorWiseLoanAnalysisRequestDto request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.FromDateBs) || request.FromDateBs == "-1")
                    throw new ArgumentException("From Date is required.");

                if (string.IsNullOrWhiteSpace(request.ToDateBs) || request.ToDateBs == "-1")
                    throw new ArgumentException("To Date is required.");

                var branchIds = SanitizeIdList(request.BranchIds);
                var collectionCenterIds = SanitizeIdList(request.CollectionCenterIds);

                // Validate collector
                string collectorId = "-1";
                if (!string.IsNullOrEmpty(request.CollectorId) &&
                    request.CollectorId != "-1" &&
                    long.TryParse(request.CollectorId, out var cId))
                {
                    collectorId = cId.ToString();
                }

                var connectionString = _context.Database.GetConnectionString();
                using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();

                var fromDateAd = await _dateConverter.NepaliToEnglishAsync(request.FromDateBs);
                var toDateAd = await _dateConverter.NepaliToEnglishAsync(request.ToDateBs);

                var fromDateStr = fromDateAd.ToString("yyyy-MM-dd");
                var toDateStr = toDateAd.ToString("yyyy-MM-dd");

                var sqlFilterExpOrderBy = BuildSqlOrderBy(request.OrderBy);

                // Penalty type is validated against known values (defaults to S)
                var penaltyType = request.PenaltyType?.Trim().ToUpper();
                if (penaltyType != "S" && penaltyType != "R" && penaltyType != "A")
                    penaltyType = "S";

                var parameters = new DynamicParameters();
                parameters.Add("@fromDate", fromDateStr, DbType.String, size: 1000);
                parameters.Add("@toDate", toDateStr, DbType.String, size: 1000);
                parameters.Add("@branchId", branchIds, DbType.String, size: -1);
                parameters.Add("@collectorId", collectorId, DbType.String, size: 1000);
                parameters.Add("@CenterList", collectionCenterIds, DbType.String, size: -1);
                parameters.Add("@SqlFilterExpOrderby", sqlFilterExpOrderBy, DbType.String, size: -1);
                parameters.Add("@SqlPenaltyType", penaltyType, DbType.String, size: -1);

                var rows = (await connection.QueryAsync<CollectorWiseLoanAnalysisRowDto>(
                    "sp_7_16_CollectorWiseLoanAnalysisReport",
                    parameters,
                    commandType: CommandType.StoredProcedure,
                    commandTimeout: 600
                )).AsList();

                // Resolve Branch name
                string branchName = "All";
                if (branchIds != "-1")
                {
                    var names = await connection.QueryAsync<string>(
                        $"SELECT OfficeName FROM UsmOffice WHERE UsmOfficeId IN ({branchIds})");
                    var nameList = names.Where(n => !string.IsNullOrWhiteSpace(n)).ToList();
                    branchName = nameList.Count > 0 ? string.Join(", ", nameList) : "All";
                }

                // Resolve Collector name
                string? collectorName = null;
                if (collectorId != "-1")
                {
                    collectorName = await connection.QueryFirstOrDefaultAsync<string>(
                        "SELECT CollectorFullName FROM HurCollector WHERE HurCollectorId = @Id",
                        new { Id = long.Parse(collectorId) });
                }

                // Resolve Collection Center name
                string? collectionCenterName = null;
                if (collectionCenterIds != "-1")
                {
                    var ccNames = await connection.QueryAsync<string>(
                        $"SELECT CollectionCenterName FROM SycCollectionCenter WHERE SycCollectionCenterId IN ({collectionCenterIds})");
                    var ccList = ccNames.Where(n => !string.IsNullOrWhiteSpace(n)).ToList();
                    collectionCenterName = ccList.Count > 0 ? string.Join(", ", ccList) : null;
                }

                return new CollectorWiseLoanAnalysisData
                {
                    Rows = rows,
                    TotalRecords = rows.Count,
                    TotalLoanIssueAmount = rows.Sum(r => r.LoanIssueAmount ?? 0),
                    TotalPaidAmount = rows.Sum(r => r.PaidAmount ?? 0),
                    TotalBalanceAmount = rows.Sum(r => r.BalanceAmount ?? 0),
                    TotalGoodloan = rows.Sum(r => r.Goodloan ?? 0),
                    TotalArrear = rows.Sum(r => r.Arrear ?? 0),
                    TotalArrearfrm1to365 = rows.Sum(r => r.Arrearfrm1to365 ?? 0),
                    TotalArreargrtthan365 = rows.Sum(r => r.Arreargrtthan365 ?? 0),
                    TotalOverDue = rows.Sum(r => r.OverDue ?? 0),
                    TotalProvision = rows.Sum(r => r.TotalProvision ?? 0),
                    TotalOpeningBalance = rows.Sum(r => r.OpeningBalance ?? 0),
                    TotalClosingBalance = rows.Sum(r => r.ClosingBalance ?? 0),
                    FromDateBs = request.FromDateBs,
                    ToDateBs = request.ToDateBs,
                    FromDateAd = fromDateStr,
                    ToDateAd = toDateStr,
                    BranchName = branchName,
                    CollectorName = collectorName,
                    CollectionCenterName = collectionCenterName,
                    PenaltyType = penaltyType,
                    PenaltyTypeName = GetPenaltyTypeName(penaltyType),
                    OrderBy = request.OrderBy,
                    EnableCollectionCenter = request.EnableCollectionCenter,
                    ReportMode = request.ReportMode,
                    ReportModeName = GetReportModeName(request.ReportMode)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetReportDataAsync (CollectorWiseLoanAnalysis)");
                throw;
            }
        }
    }
}