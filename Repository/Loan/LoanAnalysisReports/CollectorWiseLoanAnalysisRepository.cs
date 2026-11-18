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

        // positive numeric ids only; "-1" means "all"
        private static string SanitizeIdList(string? ids)
        {
            if (string.IsNullOrWhiteSpace(ids) || ids == "-1" || ids == "string")
                return "-1";

            var validIds = ids
                .Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Where(id => long.TryParse(id, out var v) && v > 0)
                .Select(id => long.Parse(id).ToString())
                .Distinct()
                .ToList();

            return validIds.Count == 0 ? "-1" : string.Join(",", validIds);
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
            return penaltyType switch
            {
                "R" => "Remaining Principle",
                "A" => "After Maturity",
                _ => "Schedule Wise"
            };
        }

        // "D" or "0" = Detail; anything else ("S", "1") = Summary
        private static bool IsDetailMode(string? mode)
        {
            var v = (mode ?? "").Trim().ToUpperInvariant();
            return v == "D" || v == "0";
        }

        private static string GetReportModeName(string? reportMode)
            => IsDetailMode(reportMode) ? "Detail" : "Summary";

        private static CollectorWiseLoanAnalysisSummaryRowDto BuildSummaryRow(
            string name, IEnumerable<CollectorWiseLoanAnalysisRowDto> items, bool isHeader)
        {
            var list = items.ToList();
            return new CollectorWiseLoanAnalysisSummaryRowDto
            {
                GroupName = name,
                IsGroupHeader = isHeader,
                OpeningBalance = list.Sum(r => r.OpeningBalance ?? 0),
                OpeningActiveCount = list.Sum(r => r.OpeningActiveLoanCount ?? 0),
                DisburseAmount = list.Sum(r => r.LoanIssueAmount ?? 0),
                PaymentAmount = list.Sum(r => r.PaidAmount ?? 0),
                BalanceAmount = list.Sum(r => r.BalanceAmount ?? 0),
                BalanceActiveCount = list.Sum(r => r.CurrentActiveLoanCount ?? 0),
                ClosingBalance = list.Sum(r => r.ClosingBalance ?? 0),
                ClosingActiveCount = list.Sum(r => r.TotalActiveLoanCount ?? 0),
                GoodAmt = list.Sum(r => r.Goodloan ?? 0),
                DueAmt = list.Sum(r => r.OverDue ?? 0),
                Due1To365 = list.Sum(r => r.Arrearfrm1to365 ?? 0),
                DueGt365 = list.Sum(r => r.Arreargrtthan365 ?? 0)
            };
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

                string collectorId = "-1";
                if (!string.IsNullOrEmpty(request.CollectorId) &&
                    request.CollectorId != "-1" &&
                    long.TryParse(request.CollectorId, out var cId) && cId > 0)
                {
                    collectorId = cId.ToString();
                }

                var connectionString = _context.Database.GetConnectionString();
                if (string.IsNullOrEmpty(connectionString))
                    throw new InvalidOperationException("Database connection string is not configured.");

                using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();

                var fromDateAd = await _dateConverter.NepaliToEnglishAsync(request.FromDateBs);
                var toDateAd = await _dateConverter.NepaliToEnglishAsync(request.ToDateBs);

                var fromDateStr = fromDateAd.ToString("yyyy-MM-dd");
                var toDateStr = toDateAd.ToString("yyyy-MM-dd");

                var sqlFilterExpOrderBy = BuildSqlOrderBy(request.OrderBy);

                var penaltyType = (request.PenaltyType ?? "S").Trim().ToUpperInvariant();
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

                string branchName = "All";
                if (branchIds != "-1")
                {
                    var names = await connection.QueryAsync<string>(
                        $"SELECT OfficeName FROM UsmOffice WHERE UsmOfficeId IN ({branchIds})");
                    var nameList = names.Where(n => !string.IsNullOrWhiteSpace(n)).ToList();
                    branchName = nameList.Count > 0 ? string.Join(", ", nameList) : "All";
                }

                string? collectorName = null;
                if (collectorId != "-1")
                {
                    collectorName = await connection.QueryFirstOrDefaultAsync<string>(
                        "SELECT CollectorFullName FROM HurCollector WHERE HurCollectorId = @Id",
                        new { Id = long.Parse(collectorId) });
                }

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

        public async Task<CollectorWiseLoanAnalysisSummaryData> GetSummaryReportDataAsync(CollectorWiseLoanAnalysisRequestDto request)
        {
            var detail = await GetReportDataAsync(request);

            static string Clean(string? s) => string.IsNullOrWhiteSpace(s) ? "Unassigned" : s.Trim();

            var outputRows = new List<CollectorWiseLoanAnalysisSummaryRowDto>(); // rows that are printed
            var leafRows = new List<CollectorWiseLoanAnalysisSummaryRowDto>();   // used for grand totals (no double counting)

            if (IsDetailMode(request.ReportMode))
            {
                // DETAIL: bold collector row, followed by its loan types
                foreach (var collector in detail.Rows.GroupBy(r => Clean(r.CollectorName)).OrderBy(g => g.Key))
                {
                    outputRows.Add(BuildSummaryRow(collector.Key, collector, isHeader: true));

                    foreach (var loanType in collector.GroupBy(r => Clean(r.LoanType)).OrderBy(g => g.Key))
                    {
                        var row = BuildSummaryRow(loanType.Key, loanType, isHeader: false);
                        outputRows.Add(row);
                        leafRows.Add(row);
                    }
                }
            }
            else
            {
                // SUMMARY: one row per loan type
                foreach (var loanType in detail.Rows.GroupBy(r => Clean(r.LoanType)).OrderBy(g => g.Key))
                {
                    var row = BuildSummaryRow(loanType.Key, loanType, isHeader: false);
                    outputRows.Add(row);
                    leafRows.Add(row);
                }
            }

            return new CollectorWiseLoanAnalysisSummaryData
            {
                Rows = outputRows,
                TotalOpeningBalance = leafRows.Sum(r => r.OpeningBalance),
                TotalOpeningActiveCount = leafRows.Sum(r => r.OpeningActiveCount),
                TotalDisburseAmount = leafRows.Sum(r => r.DisburseAmount),
                TotalPaymentAmount = leafRows.Sum(r => r.PaymentAmount),
                TotalBalanceAmount = leafRows.Sum(r => r.BalanceAmount),
                TotalBalanceActiveCount = leafRows.Sum(r => r.BalanceActiveCount),
                TotalClosingBalance = leafRows.Sum(r => r.ClosingBalance),
                TotalClosingActiveCount = leafRows.Sum(r => r.ClosingActiveCount),
                TotalGoodAmt = leafRows.Sum(r => r.GoodAmt),
                TotalDueAmt = leafRows.Sum(r => r.DueAmt),
                TotalDue1To365 = leafRows.Sum(r => r.Due1To365),
                TotalDueGt365 = leafRows.Sum(r => r.DueGt365),
                FromDateBs = detail.FromDateBs,
                ToDateBs = detail.ToDateBs,
                FromDateAd = detail.FromDateAd,
                ToDateAd = detail.ToDateAd,
                BranchName = detail.BranchName
            };
        }
    }
}