// Repository/Loan/OtherReports/LoanInterestReceivableMonthlyRepository.cs
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
    public class LoanInterestReceivableMonthlyRepository : ILoanInterestReceivableMonthlyRepository
    {
        private readonly AppDbContext _context;
        private readonly IDateConverterService _dateConverter;
        private readonly ILogger<LoanInterestReceivableMonthlyRepository> _logger;

        public LoanInterestReceivableMonthlyRepository(
            AppDbContext context,
            IDateConverterService dateConverter,
            ILogger<LoanInterestReceivableMonthlyRepository> logger)
        {
            _context = context;
            _dateConverter = dateConverter;
            _logger = logger;
        }

        // --------------------------------------------------------------
        // @SqlFilterExpOrderBy — column names match the SP's final SELECT
        // Preserves the substring-based natural sort for MemberId and
        // LoanAccountNo exactly as in the legacy BLL.
        // --------------------------------------------------------------
        private static string BuildSqlOrderBy(LoanInterestReceivableMonthlyRequestDto request)
        {
            if (string.IsNullOrEmpty(request.OrderBy) || request.OrderBy == "-1")
                return string.Empty;

            return request.OrderBy.Trim() switch
            {
                "MemberId" => " order by substring(MemberId, 1,(len(MemberId)-charindex('-', MemberId))-1), MemberId ",
                "FullName" => " order by FullName",
                "LoanAccountNo" => " order by substring(LoanAccountNo, 1,(len(LoanAccountNo)-charindex('-', LoanAccountNo))-1), LoanAccountNo ",
                "InterestAmount" => " order by InterestAmount DESC",
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

        public async Task<LoanInterestReceivableMonthlyData> GetReportDataAsync(LoanInterestReceivableMonthlyRequestDto request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.TillDateBs) || request.TillDateBs == "-1")
                {
                    throw new ArgumentException("TillDateBs is required.");
                }

                var branchIds = SanitizeBranchIds(request.BranchIds);

                var connectionString = _context.Database.GetConnectionString();
                using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();

                var sqlFilterExpBranchId = new StringBuilder();
                string? memberGroupName = null;

                // --------------------------------------------------------------
                // Build filter expression matching legacy BLL:
                // 1. Member Group filter (@SqlFilterExpbranchId)
                // 2. Branch filter (@SqlFilterExpbranchId)
                // 3. @SqlFilterExp = quoted AD date string
                // 4. ORDER BY (@SqlFilterExpOrderBy)
                // --------------------------------------------------------------
                if (request.MemberGroupId != -1)
                {
                    sqlFilterExpBranchId.Append(" AND MR.SycMemberGroupId = ").Append(request.MemberGroupId);

                    // Get member group name for display
                    memberGroupName = await connection.QueryFirstOrDefaultAsync<string>(
                        "SELECT Name FROM SycMemberGroup WHERE SycMemberGroupId = @Id",
                        new { Id = request.MemberGroupId });
                }

                if (!string.IsNullOrEmpty(branchIds))
                {
                    sqlFilterExpBranchId.Append(" And v.UsmOfficeId in (").Append(branchIds).Append(")");
                }

                // Convert BS till date to AD
                var tillDateAd = await _dateConverter.NepaliToEnglishAsync(request.TillDateBs);
                var tillDateStr = tillDateAd.ToString("yyyy-MM-dd");

                // @SqlFilterExp is a quoted date string (SP concatenates it inside SQL)
                var sqlFilterExp = $"'{tillDateStr}'";
                var sqlFilterExpOrderBy = BuildSqlOrderBy(request);

                var parameters = new DynamicParameters();
                parameters.Add("@SqlFilterExp", sqlFilterExp, DbType.String, size: -1);
                parameters.Add("@SqlFilterExpbranchId", sqlFilterExpBranchId.ToString(), DbType.String, size: -1);
                parameters.Add("@SqlFilterExpOrderBy", sqlFilterExpOrderBy, DbType.String, size: -1);

                var rows = await connection.QueryAsync<LoanInterestReceivableMonthlyRowDto>(
                    "sp_7_16_LoanInterestReceivableMonthlyReport",
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

                return new LoanInterestReceivableMonthlyData
                {
                    Rows = resultList,
                    TotalRecords = resultList.Count,
                    TotalLoanIssueAmount = resultList.Sum(r => r.LoanIssueAmount ?? 0),
                    TotalDepositAmount = resultList.Sum(r => r.DepositAmount ?? 0),
                    TotalBalance = resultList.Sum(r => r.Balance ?? 0),
                    TotalInterestAmount = resultList.Sum(r => r.InterestAmount ?? 0),
                    TillDateBs = request.TillDateBs,
                    BranchName = branchName,
                    MemberGroupName = memberGroupName,
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