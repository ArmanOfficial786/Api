
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
    public class LoanAgingArrealCalculationRepository : ILoanAgingArrealCalculationRepository
    {
        private readonly AppDbContext _context;
        private readonly IDateConverterService _dateConverter;
        private readonly ILogger<LoanAgingArrealCalculationRepository> _logger;

        public LoanAgingArrealCalculationRepository(
            AppDbContext context,
            IDateConverterService dateConverter,
            ILogger<LoanAgingArrealCalculationRepository> logger)
        {
            _context = context;
            _dateConverter = dateConverter;
            _logger = logger;
        }

        private static string SanitizeBranchIds(string? branchIds)
        {
            if (string.IsNullOrWhiteSpace(branchIds) || branchIds == "-1" || branchIds == "string")
                return "-1";

            var validIds = branchIds
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
                "MemberId" => " order by substring(MemberId, 1,(len(MemberId)-charindex('-', MemberId))-1), MemberId ",
                "FullName" => " order by FullName ",
                "LoanAccountNo" => " order by substring(LoanAccountNo, 1,(len(LoanAccountNo)-charindex('-', LoanAccountNo))-1), LoanAccountNo ",
                "LoanIssueAmount" => " order by LoanIssueAmount DESC",
                "BalanceAmount" => " order by BalanceAmount DESC",
                "Noofdays" => " order by Noofdays DESC",
                _ => string.Empty
            };
        }

        private static string GetPenaltyTypeName(string penaltyType)
        {
            return penaltyType?.Trim().ToUpper() switch
            {
                "R" => "Remaining Principal",
                "A" => "After Maturity",
                "S" => "Schedule Wise",
                _ => "Schedule Wise"
            };
        }

        public async Task<LoanAgingArrealCalculationData> GetReportDataAsync(LoanAgingArrealCalculationRequestDto request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.TillDateBs) || request.TillDateBs == "-1")
                {
                    throw new ArgumentException("Till Date is required.");
                }

                var branchIds = SanitizeBranchIds(request.BranchIds);

                var connectionString = _context.Database.GetConnectionString();
                using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();

                var tillDateAd = await _dateConverter.NepaliToEnglishAsync(request.TillDateBs);
                var tillDateStr = $"'{tillDateAd:yyyy-MM-dd}'";

                var sqlFilterExp = tillDateStr;

                var sqlFilterExpBranchId = string.Empty;
                if (branchIds != "-1")
                {
                    sqlFilterExpBranchId += $" And LS.UsmOfficeId in ({branchIds})";
                }

                if (!string.IsNullOrWhiteSpace(request.MemberGroupId) &&
                    request.MemberGroupId != "-1" &&
                    request.MemberGroupId != "string" &&
                    long.TryParse(request.MemberGroupId, out var memberGroupId))
                {
                    sqlFilterExpBranchId += $" AND MR.SycMemberGroupId = {memberGroupId}";
                }

                if (!string.IsNullOrEmpty(request.CollectorId) && request.CollectorId != "-1")
                {
                    sqlFilterExpBranchId += $" and LS.HurCollectorId = {request.CollectorId}";
                }

                if (!string.IsNullOrEmpty(request.CollectionCenterId) && request.CollectionCenterId != "-1")
                {
                    sqlFilterExpBranchId += $" And LS.SycCollectionCenterId in({request.CollectionCenterId})";
                }

                var sqlFilterExpOrderby = BuildSqlOrderBy(request.OrderBy);
                var sqlPenaltyType = $"'{request.PenaltyType.Trim().ToUpper()}'";

                var spName = request.Enable1To30Days
                    ? "sp_7_16_LoanAgingArreal1To30CalculationReport"
                    : "sp_7_16_LoanAgingArrealCalculationReport";

                var parameters = new DynamicParameters();
                parameters.Add("@SqlFilterExp", sqlFilterExp, DbType.String, size: -1);
                parameters.Add("@SqlFilterExpbranchId", sqlFilterExpBranchId, DbType.String, size: -1);
                parameters.Add("@SqlFilterExpOrderby", sqlFilterExpOrderby, DbType.String, size: -1);
                parameters.Add("@SqlPenaltyType", sqlPenaltyType, DbType.String, size: -1);

                var rows = await connection.QueryAsync<LoanAgingArrealCalculationRowDto>(
                    spName,
                    parameters,
                    commandType: CommandType.StoredProcedure,
                    commandTimeout: 120
                );

                var resultList = rows.AsList();

                string branchName = "All";
                if (branchIds != "-1")
                {
                    var names = await connection.QueryAsync<string>(
                        $"SELECT OfficeName FROM UsmOffice WHERE UsmOfficeId IN ({branchIds})");
                    var nameList = names.Where(n => !string.IsNullOrWhiteSpace(n)).ToList();
                    branchName = nameList.Count > 0 ? string.Join(", ", nameList) : "All";
                }

                string? memberGroupName = null;
                if (!string.IsNullOrWhiteSpace(request.MemberGroupId) &&
                    request.MemberGroupId != "-1" &&
                    request.MemberGroupId != "string" &&
                    long.TryParse(request.MemberGroupId, out var memberGroupIdForName))
                {
                    memberGroupName = await connection.QueryFirstOrDefaultAsync<string>(
                        "SELECT Name FROM SycMemberGroup WHERE SycMemberGroupId = @Id",
                        new { Id = memberGroupIdForName });
                }

                string? collectionCenterName = null;
                if (!string.IsNullOrEmpty(request.CollectionCenterId) && request.CollectionCenterId != "-1")
                {
                    collectionCenterName = await connection.QueryFirstOrDefaultAsync<string>(
                        $"SELECT STRING_AGG(CollectionCenterName, ', ') FROM SycCollectionCenter WHERE SycCollectionCenterId IN ({request.CollectionCenterId})");
                }

                string? collectorName = null;
                if (!string.IsNullOrEmpty(request.CollectorId) && request.CollectorId != "-1")
                {
                    collectorName = await connection.QueryFirstOrDefaultAsync<string>(
                        "SELECT CollectorFullName FROM HurCollector WHERE HurCollectorId = @Id",
                        new { Id = request.CollectorId });
                }

                return new LoanAgingArrealCalculationData
                {
                    Rows = resultList,
                    TotalRecords = resultList.Count,
                    TotalLoanIssueAmount = resultList.Sum(r => r.LoanIssueAmount ?? 0),
                    TotalBalanceAmount = resultList.Sum(r => r.BalanceAmount ?? 0),
                    TotalOverDue = resultList.Sum(r => r.OverDue ?? 0),
                    TotalGoodLoan = resultList.Sum(r => r.Goodloan ?? 0),
                    TotalArrear = resultList.Sum(r => r.Arrear ?? 0),
                    TotalFrm1to365 = resultList.Sum(r => r.Frm1to365 ?? 0),
                    TotalGrtthan365 = resultList.Sum(r => r.Grtrhan365 ?? 0),
                    TillDateBs = request.TillDateBs,
                    BranchName = branchName,
                    MemberGroupName = memberGroupName,
                    PenaltyType = request.PenaltyType,
                    PenaltyTypeName = GetPenaltyTypeName(request.PenaltyType),
                    CollectionCenterName = collectionCenterName,
                    CollectorName = collectorName,
                    Enable1To30Days = request.Enable1To30Days,
                    OrderBy = request.OrderBy
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetReportDataAsync");
                throw;
            }
        }

        public async Task<List<LoanAgingArrealExcelExportDto>> GetExcelExportDataAsync(LoanAgingArrealCalculationRequestDto request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.TillDateBs) || request.TillDateBs == "-1")
                {
                    throw new ArgumentException("Till Date is required.");
                }

                var branchIds = SanitizeBranchIds(request.BranchIds);

                var connectionString = _context.Database.GetConnectionString();
                using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();

                var tillDateAd = await _dateConverter.NepaliToEnglishAsync(request.TillDateBs);
                var tillDateStr = $"'{tillDateAd:yyyy-MM-dd}'";

                var sqlFilterExp = tillDateStr;

                var sqlFilterExpBranchId = string.Empty;
                if (branchIds != "-1")
                {
                    sqlFilterExpBranchId += $" And v.UsmOfficeId in ({branchIds})";
                }

                if (!string.IsNullOrWhiteSpace(request.MemberGroupId) &&
                    request.MemberGroupId != "-1" &&
                    request.MemberGroupId != "string" &&
                    long.TryParse(request.MemberGroupId, out var memberGroupId))
                {
                    sqlFilterExpBranchId += $" AND MR.SycMemberGroupId = {memberGroupId}";
                }

                var sqlPenaltyType = $"'{request.PenaltyType.Trim().ToUpper()}'";

                var parameters = new DynamicParameters();
                parameters.Add("@SqlFilterExp", sqlFilterExp, DbType.String, size: -1);
                parameters.Add("@SqlFilterExpbranchId", sqlFilterExpBranchId, DbType.String, size: -1);
                parameters.Add("@SqlPenaltyType", sqlPenaltyType, DbType.String, size: -1);

                var rows = await connection.QueryAsync<LoanAgingArrealExcelExportDto>(
                    "sp_7_16_LoanAgeingReportPrintinExcel",
                    parameters,
                    commandType: CommandType.StoredProcedure,
                    commandTimeout: 120
                );

                return rows.AsList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetExcelExportDataAsync");
                throw;
            }
        }
    }
}