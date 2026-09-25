// Repository/Microfinance/MicrofinanceReport/LoanAgeingArrearCalculationRepository.cs
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using NexgenCosysReport.DbContext;
using NexgenCosysReport.Dtos.RequestDtos.MicroFinance.MicroFinanceReports;
using NexgenCosysReport.Inteface.ServiceInterface.Common;
using NexgenCosysReport.Inteface.ServiceInterface.Microfinance.MicrofinanceReport;
using System.Data;

namespace NexgenCosysReport.Repository.Microfinance.MicrofinanceReport
{
    public class LoanAgeingArrearCalculationRepository : ILoanAgeingArrearCalculationRepository
    {
        private readonly AppDbContext _context;
        private readonly IDateConverterService _dateConverter;
        private readonly ILogger<LoanAgeingArrearCalculationRepository> _logger;

        public LoanAgeingArrearCalculationRepository(
            AppDbContext context,
            IDateConverterService dateConverter,
            ILogger<LoanAgeingArrearCalculationRepository> logger)
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
            return orderBy?.Trim() switch
            {
                "MemberId" => " order by substring(MemberId, 1,(len(MemberId)-charindex('-', MemberId))-1), MemberId ",
                "FullName" => " order by FullName ",
                "LoanAccountNo" => " order by substring(LoanAccountNo, 1,(len(LoanAccountNo)-charindex('-', LoanAccountNo))-1), LoanAccountNo ",
                "LoanIssueAmount" => " order by LoanIssueAmount DESC",
                "BalanceAmount" => " order by BalanceAmount DESC",
                "Noofdays" => " order by Noofdays DESC",
                _ => string.Empty
            };
        }

        private static string GetPenaltyTypeName(string penaltyType) =>
            penaltyType?.Trim().ToUpper() switch
            {
                "R" => "Remaining Principal",
                "A" => "After Maturity",
                "S" => "Schedule Wise",
                _ => "Schedule Wise"
            };

        public async Task<LoanAgeingArrearCalculationData> GetReportDataAsync(LoanAgeingArrearCalculationRequestDto request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.TillDateBs) || request.TillDateBs == "-1")
                    throw new ArgumentException("Till Date is required.");

                var branchIds = SanitizeIdList(request.BranchIds);
                if (branchIds == "-1")
                    throw new ArgumentException("Please select Branch Name.");

                var collectionCenterIds = SanitizeIdList(request.CollectionCenterIds);

                var collectorId = "-1";
                if (!string.IsNullOrEmpty(request.CollectorId) &&
                    request.CollectorId != "-1" &&
                    long.TryParse(request.CollectorId, out var cId))
                {
                    collectorId = cId.ToString();
                }

                long? memberGroupId = null;
                if (!string.IsNullOrEmpty(request.MemberGroupId) &&
                    request.MemberGroupId != "-1" &&
                    long.TryParse(request.MemberGroupId, out var mg))
                {
                    memberGroupId = mg;
                }

                var penaltyType = request.PenaltyType?.Trim().ToUpper();
                if (penaltyType != "S" && penaltyType != "R" && penaltyType != "A")
                    penaltyType = "S";

                var connectionString = _context.Database.GetConnectionString();
                using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();

                var tillDateAd = await _dateConverter.NepaliToEnglishAsync(request.TillDateBs);
                var tillDateStr = $"'{tillDateAd:yyyy-MM-dd}'";

                var sqlFilterExpBranchId = $" And v.UsmOfficeId in ({branchIds})";

                if (memberGroupId.HasValue)
                    sqlFilterExpBranchId += $" AND MR.SycMemberGroupId = {memberGroupId.Value}";

                if (collectorId != "-1")
                    sqlFilterExpBranchId += $" and LS.HurCollectorId = {collectorId}";

                if (collectionCenterIds != "-1")
                    sqlFilterExpBranchId += $" and LS.SycCollectionCenterId in ({collectionCenterIds})";

                var sqlPenaltyType = $"'{penaltyType}'";

                var spName = request.Enable1To30Days
                    ? "sp_7_16_LoanAgingArreal1to30CalculationReport"
                    : "sp_7_16_LoanAgingArrealCalculationReport";

                var parameters = new DynamicParameters();
                parameters.Add("@SqlFilterExp", tillDateStr, DbType.String, size: -1);
                parameters.Add("@SqlFilterExpbranchId", sqlFilterExpBranchId, DbType.String, size: -1);
                parameters.Add("@SqlPenaltyType", sqlPenaltyType, DbType.String, size: -1);

                var rows = (await connection.QueryAsync<LoanAgeingArrearCalculationRowDto>(
                    spName,
                    parameters,
                    commandType: CommandType.StoredProcedure,
                    commandTimeout: 600
                )).AsList();

                string branchName = "All";
                if (branchIds != "-1")
                {
                    var names = await connection.QueryAsync<string>(
                        $"SELECT OfficeName FROM UsmOffice WHERE UsmOfficeId IN ({branchIds})");
                    var list = names.Where(n => !string.IsNullOrWhiteSpace(n)).ToList();
                    branchName = list.Count > 0 ? string.Join(", ", list) : "All";
                }

                string? collectionCenterName = null;
                if (collectionCenterIds != "-1")
                {
                    var ccNames = await connection.QueryAsync<string>(
                        $"SELECT CollectionCenterName FROM SycCollectionCenter WHERE SycCollectionCenterId IN ({collectionCenterIds})");
                    var list = ccNames.Where(n => !string.IsNullOrWhiteSpace(n)).ToList();
                    collectionCenterName = list.Count > 0 ? string.Join(", ", list) : null;
                }

                string? collectorName = null;
                if (collectorId != "-1")
                {
                    collectorName = await connection.QueryFirstOrDefaultAsync<string>(
                        "SELECT CollectorFullName FROM HurCollector WHERE HurCollectorId = @Id",
                        new { Id = long.Parse(collectorId) });
                }

                string? memberGroupName = null;
                if (memberGroupId.HasValue)
                {
                    memberGroupName = await connection.QueryFirstOrDefaultAsync<string>(
                        "SELECT Name FROM SycMemberGroup WHERE SycMemberGroupId = @Id",
                        new { Id = memberGroupId.Value });
                }

                return new LoanAgeingArrearCalculationData
                {
                    Rows = rows,
                    TotalRecords = rows.Count,
                    TotalLoanIssueAmount = rows.Sum(r => r.LoanIssueAmount ?? 0),
                    TotalPrinciplePaidAmount = rows.Sum(r => r.PrinciplePaidAmt ?? 0),
                    TotalLoanBalance = rows.Sum(r => r.LoanBalance ?? 0),
                    TotalGoodLoan = rows.Sum(r => r.Goodloan ?? 0),
                    TotalBetween0to30Days = rows.Sum(r => r.Between0to30Days ?? 0),
                    TotalBetween31to365Days = rows.Sum(r => r.Between31to365Days ?? 0),
                    TotalGraterThan365Days = rows.Sum(r => r.GraterThan365Days ?? 0),
                    TotalOverdue = rows.Sum(r => r.OverDue ?? 0),
                    TillDateBs = request.TillDateBs,
                    TillDateAd = tillDateAd.ToString("yyyy-MM-dd"),
                    BranchName = branchName,
                    CollectionCenterName = collectionCenterName,
                    CollectorName = collectorName,
                    MemberGroupName = memberGroupName,
                    PenaltyType = penaltyType,
                    PenaltyTypeName = GetPenaltyTypeName(penaltyType),
                    OrderBy = request.OrderBy,
                    Enable1To30Days = request.Enable1To30Days,
                    GroupByCollectionCenter = request.GroupByCollectionCenter
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetReportDataAsync (LoanAgeingArrearCalculation)");
                throw;
            }
        }

        public async Task<LoanAgeingArrearCalculationData> GetExcelReportDataAsync(LoanAgeingArrearCalculationRequestDto request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.TillDateBs) || request.TillDateBs == "-1")
                    throw new ArgumentException("Till Date is required.");

                var branchIds = SanitizeIdList(request.BranchIds);
                if (branchIds == "-1")
                    throw new ArgumentException("Please select Branch Name.");

                long? memberGroupId = null;
                if (!string.IsNullOrEmpty(request.MemberGroupId) &&
                    request.MemberGroupId != "-1" &&
                    long.TryParse(request.MemberGroupId, out var mg))
                {
                    memberGroupId = mg;
                }

                var penaltyType = request.PenaltyType?.Trim().ToUpper();
                if (penaltyType != "S" && penaltyType != "R" && penaltyType != "A")
                    penaltyType = "S";

                var connectionString = _context.Database.GetConnectionString();
                using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();

                var tillDateAd = await _dateConverter.NepaliToEnglishAsync(request.TillDateBs);
                var tillDateStr = $"'{tillDateAd:yyyy-MM-dd}'";

                var sqlFilterExpBranchId = $" And v.UsmOfficeId in ({branchIds})";
                if (memberGroupId.HasValue)
                    sqlFilterExpBranchId += $" AND MR.SycMemberGroupId = {memberGroupId.Value}";

                var sqlPenaltyType = $"'{penaltyType}'";

                var parameters = new DynamicParameters();
                parameters.Add("@SqlFilterExp", tillDateStr, DbType.String, size: -1);
                parameters.Add("@SqlFilterExpbranchId", sqlFilterExpBranchId, DbType.String, size: -1);
                parameters.Add("@SqlPenaltyType", sqlPenaltyType, DbType.String, size: -1);

                var rows = (await connection.QueryAsync<LoanAgeingArrearCalculationRowDto>(
                    "sp_7_16_LoanAgeingReportPrintinExcel",
                    parameters,
                    commandType: CommandType.StoredProcedure,
                    commandTimeout: 600
                )).AsList();

                string branchName = "All";
                if (branchIds != "-1")
                {
                    var names = await connection.QueryAsync<string>(
                        $"SELECT OfficeName FROM UsmOffice WHERE UsmOfficeId IN ({branchIds})");
                    var list = names.Where(n => !string.IsNullOrWhiteSpace(n)).ToList();
                    branchName = list.Count > 0 ? string.Join(", ", list) : "All";
                }

                return new LoanAgeingArrearCalculationData
                {
                    Rows = rows,
                    TotalRecords = rows.Count,
                    TotalLoanIssueAmount = rows.Sum(r => r.LoanIssueAmount ?? 0),
                    TotalPrinciplePaidAmount = rows.Sum(r => r.PrinciplePaidAmt ?? 0),
                    TotalLoanBalance = rows.Sum(r => r.LoanBalance ?? 0),
                    TotalGoodLoan = rows.Sum(r => r.Goodloan ?? 0),
                    TotalBetween0to30Days = rows.Sum(r => r.Between0to30Days ?? 0),
                    TotalBetween31to365Days = rows.Sum(r => r.Between31to365Days ?? 0),
                    TotalGraterThan365Days = rows.Sum(r => r.GraterThan365Days ?? 0),
                    TotalOverdue = rows.Sum(r => r.OverDue ?? 0),
                    TillDateBs = request.TillDateBs,
                    TillDateAd = tillDateAd.ToString("yyyy-MM-dd"),
                    BranchName = branchName,
                    MemberGroupName = memberGroupId.HasValue ? memberGroupId.Value.ToString() : null,
                    PenaltyType = penaltyType,
                    PenaltyTypeName = GetPenaltyTypeName(penaltyType),
                    OrderBy = request.OrderBy,
                    Enable1To30Days = false,
                    GroupByCollectionCenter = false
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetExcelReportDataAsync (LoanAgeingArrearCalculation)");
                throw;
            }
        }
    }
}