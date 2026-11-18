
//using Dapper;
//using Microsoft.Data.SqlClient;
//using Microsoft.EntityFrameworkCore;
//using NexgenCosysReport.DbContext;
//using NexgenCosysReport.Dtos.RequestDtos.Loan.LoanAnalysisReport;
//using NexgenCosysReport.Inteface.ServiceInterface.Common;
//using NexgenCosysReport.Inteface.ServiceInterface.Loan.LoanAnalysisReport;
//using System.Data;

//namespace NexgenCosysReport.Repository.Loan.LoanAnalysisReport
//{
//    public class LoanAgingRiskCalculationRepository : ILoanAgingRiskCalculationRepository
//    {
//        private readonly AppDbContext _context;
//        private readonly IDateConverterService _dateConverter;
//        private readonly ILogger<LoanAgingRiskCalculationRepository> _logger;

//        public LoanAgingRiskCalculationRepository(
//            AppDbContext context,
//            IDateConverterService dateConverter,
//            ILogger<LoanAgingRiskCalculationRepository> logger)
//        {
//            _context = context;
//            _dateConverter = dateConverter;
//            _logger = logger;
//        }

//        private static string SanitizeBranchIds(string? branchIds)
//        {
//            if (string.IsNullOrWhiteSpace(branchIds) || branchIds == "-1" || branchIds == "string")
//                return "-1";

//            var validIds = branchIds
//                .Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
//                .Where(id => long.TryParse(id, out _));

//            var joined = string.Join(",", validIds);
//            return string.IsNullOrEmpty(joined) ? "-1" : joined;
//        }

//        private static string BuildSqlOrderBy(string? orderBy)
//        {
//            if (string.IsNullOrEmpty(orderBy) || orderBy == "-1")
//                return string.Empty;

//            return orderBy.Trim() switch
//            {
//                "MemberId" => " order by substring(MemberId, 1,(len(MemberId)-charindex('-', MemberId))-1), MemberId ",
//                "FullName" => " order by FullName ",
//                "LoanAccountNo" => " order by substring(LoanAccountNo, 1,(len(LoanAccountNo)-charindex('-', LoanAccountNo))-1), LoanAccountNo ",
//                "LoanIssueAmount" => " order by LoanIssueAmount DESC",
//                "BalanceAmount" => " order by BalanceAmount DESC",
//                "Noofdays" => " order by Noofdays DESC",
//                _ => string.Empty
//            };
//        }

//        private static string GetPenaltyTypeName(string penaltyType)
//        {
//            return penaltyType?.Trim().ToUpper() switch
//            {
//                "R" => "Remaining Principal",
//                "A" => "After Maturity",
//                "S" => "Schedule Wise",
//                _ => "Schedule Wise"
//            };
//        }

//        public async Task<LoanAgingRiskCalculationData> GetReportDataAsync(LoanAgingRiskCalculationRequestDto request)
//        {
//            try
//            {
//                if (string.IsNullOrWhiteSpace(request.TillDateBs) || request.TillDateBs == "-1")
//                {
//                    throw new ArgumentException("Till Date is required.");
//                }

//                var branchIds = SanitizeBranchIds(request.BranchIds);

//                var connectionString = _context.Database.GetConnectionString();
//                using var connection = new SqlConnection(connectionString);
//                await connection.OpenAsync();

//                var tillDateAd = await _dateConverter.NepaliToEnglishAsync(request.TillDateBs);
//                var tillDateStr = $"'{tillDateAd:yyyy-MM-dd}'";

//                var sqlFilterExp = tillDateStr;

//                var sqlFilterExpBranchId = string.Empty;
//                if (branchIds != "-1")
//                {
//                    sqlFilterExpBranchId += $" And v.UsmOfficeId in ({branchIds})";
//                }

//                if (!string.IsNullOrWhiteSpace(request.MemberGroupId) &&
//                    request.MemberGroupId != "-1" &&
//                    request.MemberGroupId != "string" &&
//                    long.TryParse(request.MemberGroupId, out var memberGroupId))
//                {
//                    sqlFilterExpBranchId += $" AND MR.SycMemberGroupId = {memberGroupId}";
//                }

//                var sqlFilterExpOrderby = BuildSqlOrderBy(request.OrderBy);
//                var sqlPenaltyType = $"'{request.PenaltyType.Trim().ToUpper()}'";

//                var parameters = new DynamicParameters();
//                parameters.Add("@SqlFilterExp", sqlFilterExp, DbType.String, size: -1);
//                parameters.Add("@SqlFilterExpbranchId", sqlFilterExpBranchId, DbType.String, size: -1);
//                parameters.Add("@SqlFilterExpOrderby", sqlFilterExpOrderby, DbType.String, size: -1);
//                parameters.Add("@SqlPenaltyType", sqlPenaltyType, DbType.String, size: -1);

//                var rows = await connection.QueryAsync<LoanAgingRiskCalculationRowDto>(
//                    "sp_7_16_LoanAgingRiskCalculationReport",
//                    parameters,
//                    commandType: CommandType.StoredProcedure,
//                    commandTimeout: 120
//                );

//                var resultList = rows.AsList();

//                string branchName = "All";
//                if (branchIds != "-1")
//                {
//                    var names = await connection.QueryAsync<string>(
//                        $"SELECT OfficeName FROM UsmOffice WHERE UsmOfficeId IN ({branchIds})");
//                    var nameList = names.Where(n => !string.IsNullOrWhiteSpace(n)).ToList();
//                    branchName = nameList.Count > 0 ? string.Join(", ", nameList) : "All";
//                }

//                string? memberGroupName = null;
//                if (!string.IsNullOrWhiteSpace(request.MemberGroupId) &&
//                    request.MemberGroupId != "-1" &&
//                    request.MemberGroupId != "string" &&
//                    long.TryParse(request.MemberGroupId, out var memberGroupIdForName))
//                {
//                    memberGroupName = await connection.QueryFirstOrDefaultAsync<string>(
//                        "SELECT Name FROM SycMemberGroup WHERE SycMemberGroupId = @Id",
//                        new { Id = memberGroupIdForName });
//                }

//                return new LoanAgingRiskCalculationData
//                {
//                    Rows = resultList,
//                    TotalRecords = resultList.Count,
//                    TotalLoanIssueAmount = resultList.Sum(r => r.LoanIssueAmount ?? 0),
//                    TotalBalanceAmount = resultList.Sum(r => r.BalanceAmount ?? 0),
//                    TotalOverDue = resultList.Sum(r => r.OverDue ?? 0),
//                    TotalGoodLoan = resultList.Sum(r => r.Goodloan ?? 0),
//                    TotalArrear = resultList.Sum(r => r.Arrear ?? 0),
//                    TotalFrm1to365 = resultList.Sum(r => r.Frm1to365 ?? 0),
//                    TotalGrtthan365 = resultList.Sum(r => r.Grtrhan365 ?? 0),
//                    TillDateBs = request.TillDateBs,
//                    BranchName = branchName,
//                    MemberGroupName = memberGroupName,
//                    PenaltyType = request.PenaltyType,
//                    PenaltyTypeName = GetPenaltyTypeName(request.PenaltyType),
//                    OrderBy = request.OrderBy
//                };
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, "Error in GetReportDataAsync");
//                throw;
//            }
//        }
//    }
//}


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
    public class LoanAgingRiskCalculationRepository : ILoanAgingRiskCalculationRepository
    {
        private readonly AppDbContext _context;
        private readonly IDateConverterService _dateConverter;
        private readonly ILogger<LoanAgingRiskCalculationRepository> _logger;

        public LoanAgingRiskCalculationRepository(
            AppDbContext context,
            IDateConverterService dateConverter,
            ILogger<LoanAgingRiskCalculationRepository> logger)
        {
            _context = context;
            _dateConverter = dateConverter;
            _logger = logger;
        }

        // "1, 2,3" -> [1,2,3]; ignores "-1", "string" and non-numeric values
        private static List<long> ParseIds(string? csv)
        {
            if (string.IsNullOrWhiteSpace(csv))
                return new List<long>();

            return csv
                .Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(s => long.TryParse(s, out var id) ? id : 0)
                .Where(id => id > 0)
                .Distinct()
                .ToList();
        }

        // Same mapping as the webform's ddl_orderBy switch
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

        // Webform radio values: S = Schedule Wise (default), R = Remaining Principal, A = After Maturity
        private static string NormalizePenaltyType(string? penaltyType) =>
            penaltyType?.Trim().ToUpperInvariant() switch
            {
                "R" => "R",
                "A" => "A",
                _ => "S"
            };

        private static string GetPenaltyTypeName(string penaltyType) =>
            penaltyType switch
            {
                "R" => "Remaining Principal",
                "A" => "After Maturity",
                _ => "Schedule Wise"
            };

        public async Task<LoanAgingRiskCalculationData> GetReportDataAsync(
            LoanAgingRiskCalculationRequestDto request, long userId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.TillDateBs) || request.TillDateBs == "-1")
                    throw new ArgumentException("Till Date is required.");

                var connectionString = _context.Database.GetConnectionString();
                using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();

                // 1. Offices: webform checkbox list = CUsmOffice.GetByUsmUserId(userId)
                var allowedOfficeIds = (await connection.QueryAsync<long>(
                    "SELECT DISTINCT UsmOfficeId FROM UsmRelationUserToOffice WHERE UsmUserId = @UserId",
                    new { UserId = userId })).ToList();

                if (allowedOfficeIds.Count == 0)
                    throw new UnauthorizedAccessException("User has no office access.");

                var requestedOfficeIds = ParseIds(request.BranchIds);
                if (requestedOfficeIds.Except(allowedOfficeIds).Any())
                    throw new UnauthorizedAccessException("You do not have access to one or more selected branches.");

                // Nothing sent = webform "Select All" = all of the user's offices
                var officeIds = requestedOfficeIds.Count > 0 ? requestedOfficeIds : allowedOfficeIds;

                // 2. Till date -> SP @SqlFilterExp (quoted date, as in the BLL)
                var tillDateAd = await _dateConverter.NepaliToEnglishAsync(request.TillDateBs);
                var sqlFilterExp = $"'{tillDateAd:yyyy-MM-dd}'";

                // 3. Office filter always applied; member group when selected
                var sqlFilterExpBranchId = $" And v.UsmOfficeId in ({string.Join(",", officeIds)})";

                string? memberGroupName = null;
                if (long.TryParse(request.MemberGroupId, out var memberGroupId) && memberGroupId > 0)
                {
                    sqlFilterExpBranchId += $" AND MR.SycMemberGroupId = {memberGroupId}";

                    memberGroupName = await connection.QueryFirstOrDefaultAsync<string>(
                        "SELECT Name FROM SycMemberGroup WHERE SycMemberGroupId = @Id",
                        new { Id = memberGroupId });
                }

                // 4. Order by and penalty type
                var sqlFilterExpOrderby = BuildSqlOrderBy(request.OrderBy);
                var penaltyType = NormalizePenaltyType(request.PenaltyType);

                _logger.LogInformation(
                    "SP sp_7_16_LoanAgingRiskCalculationReport | exp={Exp} | branch={Branch} | order={Order} | penalty={Penalty}",
                    sqlFilterExp, sqlFilterExpBranchId, sqlFilterExpOrderby, penaltyType);

                var parameters = new DynamicParameters();
                parameters.Add("@SqlFilterExp", sqlFilterExp, DbType.String, size: -1);
                parameters.Add("@SqlFilterExpbranchId", sqlFilterExpBranchId, DbType.String, size: -1);
                parameters.Add("@SqlFilterExpOrderby", sqlFilterExpOrderby, DbType.String, size: -1);
                parameters.Add("@SqlPenaltyType", $"'{penaltyType}'", DbType.String, size: -1);

                var rows = (await connection.QueryAsync<LoanAgingRiskCalculationRowDto>(
                    "sp_7_16_LoanAgingRiskCalculationReport",
                    parameters,
                    commandType: CommandType.StoredProcedure,
                    commandTimeout: 120
                )).AsList();

                // 5. Header branch names
                var names = await connection.QueryAsync<string>(
                    "SELECT OfficeName FROM UsmOffice WHERE UsmOfficeId IN @Ids",
                    new { Ids = officeIds });
                var nameList = names.Where(n => !string.IsNullOrWhiteSpace(n)).ToList();
                var branchName = nameList.Count > 0 ? string.Join(", ", nameList) : "All";

                return new LoanAgingRiskCalculationData
                {
                    Rows = rows,
                    TotalRecords = rows.Count,
                    TotalLoanIssueAmount = rows.Sum(r => r.LoanIssueAmount ?? 0),
                    TotalBalanceAmount = rows.Sum(r => r.BalanceAmount ?? 0),
                    TotalOverDue = rows.Sum(r => r.OverDue ?? 0),
                    TotalGoodLoan = rows.Sum(r => r.Goodloan ?? 0),
                    TotalArrear = rows.Sum(r => r.Arrear ?? 0),
                    TotalFrm1to365 = rows.Sum(r => r.Frm1to365 ?? 0),
                    TotalGrtthan365 = rows.Sum(r => r.Grtrhan365 ?? 0),
                    TillDateBs = request.TillDateBs,
                    BranchName = branchName,
                    MemberGroupName = memberGroupName,
                    PenaltyType = penaltyType,
                    PenaltyTypeName = GetPenaltyTypeName(penaltyType),
                    OrderBy = request.OrderBy
                };
            }
            catch (Exception ex) when (ex is UnauthorizedAccessException || ex is ArgumentException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetReportDataAsync");
                throw;
            }
        }
    }
}