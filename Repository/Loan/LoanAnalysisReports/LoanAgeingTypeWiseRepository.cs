// Repository/Loan/LoanAnalysisReport/LoanAgeingTypeWiseRepository.cs
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
    public class LoanAgeingTypeWiseRepository : ILoanAgeingTypeWiseRepository
    {
        private readonly AppDbContext _context;
        private readonly IDateConverterService _dateConverter;
        private readonly ILogger<LoanAgeingTypeWiseRepository> _logger;

        public LoanAgeingTypeWiseRepository(
            AppDbContext context,
            IDateConverterService dateConverter,
            ILogger<LoanAgeingTypeWiseRepository> logger)
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
                "LoanTypeName" => " order by LoanTypeName",
                "LoanIssueAmount" => " order by LoanIssueAmount DESC",
                "Repaid" => " order by Repaid DESC",
                "overdue" => " order by overdue DESC",
                _ => " order by LoanTypeName"
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

        public async Task<LoanAgeingTypeWiseData> GetReportDataAsync(LoanAgeingTypeWiseRequestDto request)
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

                var sqlFilterExpBranchId = string.Empty;
                if (branchIds != "-1")
                {
                    sqlFilterExpBranchId += $" And v.UsmOfficeId in ({branchIds})";
                }

                if (!string.IsNullOrEmpty(request.MemberGroupId) &&
                    request.MemberGroupId != "-1" &&
                    request.MemberGroupId != "string" &&
                    long.TryParse(request.MemberGroupId, out var memberGroupId))
                {
                    sqlFilterExpBranchId += $" AND MR.SycMemberGroupId = {memberGroupId}";
                }

                var sqlFilterExpOrderby = BuildSqlOrderBy(request.OrderBy);
                var sqlPenaltyType = $"'{request.PenaltyType.Trim().ToUpper()}'";

                var spName = request.WithMemberCount
                    ? "sp_7_16_LoanAgeingTypeWiseWithMemberCountReport"
                    : "sp_7_16_LoanAgeingTypeWiseReport";

                var parameters = new DynamicParameters();
                parameters.Add("@SqlFilterExp", tillDateStr, DbType.String, size: -1);
                parameters.Add("@SqlFilterExpbranchId", sqlFilterExpBranchId, DbType.String, size: -1);
                parameters.Add("@SqlFilterExpOrderby", sqlFilterExpOrderby, DbType.String, size: -1);
                parameters.Add("@SqlPenaltyType", sqlPenaltyType, DbType.String, size: -1);

                var rows = (await connection.QueryAsync<LoanAgeingTypeWiseRowDto>(
                    spName,
                    parameters,
                    commandType: CommandType.StoredProcedure,
                    commandTimeout: 300
                )).AsList();

                string branchName = "All";
                if (branchIds != "-1")
                {
                    var names = await connection.QueryAsync<string>(
                        $"SELECT OfficeName FROM UsmOffice WHERE UsmOfficeId IN ({branchIds})");
                    var nameList = names.Where(n => !string.IsNullOrWhiteSpace(n)).ToList();
                    branchName = nameList.Count > 0 ? string.Join(", ", nameList) : "All";
                }

                string? memberGroupName = null;
                if (!string.IsNullOrEmpty(request.MemberGroupId) &&
                    request.MemberGroupId != "-1" &&
                    request.MemberGroupId != "string" &&
                    long.TryParse(request.MemberGroupId, out var memberGroupIdForName))
                {
                    memberGroupName = await connection.QueryFirstOrDefaultAsync<string>(
                        "SELECT Name FROM SycMemberGroup WHERE SycMemberGroupId = @Id",
                        new { Id = memberGroupIdForName });
                }

                return new LoanAgeingTypeWiseData
                {
                    Rows = rows,
                    TotalRecords = rows.Count,
                    TotalLoans = rows.Sum(r => r.NoofLoan ?? 0),
                    TotalMembers = rows.Sum(r => r.MemberCount ?? 0),
                    TotalLoanIssueAmount = rows.Sum(r => r.LoanIssueAmount ?? 0),
                    TotalRepaid = rows.Sum(r => r.Repaid ?? 0),
                    TotalOverdue = rows.Sum(r => r.Overdue ?? 0),
                    TotalGoodLoan = rows.Sum(r => r.GoodLoan ?? 0),
                    TillDateBs = request.TillDateBs,
                    BranchName = branchName,
                    MemberGroupName = memberGroupName,
                    PenaltyType = request.PenaltyType,
                    PenaltyTypeName = GetPenaltyTypeName(request.PenaltyType),
                    OrderBy = request.OrderBy,
                    WithMemberCount = request.WithMemberCount
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