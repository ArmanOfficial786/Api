// Repository/Loan/OtherReports/LoanRepaymentRepository.cs
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
    public class LoanRepaymentRepository : ILoanRepaymentRepository
    {
        private readonly AppDbContext _context;
        private readonly IDateConverterService _dateConverter;
        private readonly ILogger<LoanRepaymentRepository> _logger;

        public LoanRepaymentRepository(
            AppDbContext context,
            IDateConverterService dateConverter,
            ILogger<LoanRepaymentRepository> logger)
        {
            _context = context;
            _dateConverter = dateConverter;
            _logger = logger;
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

        public async Task<LoanRepaymentData> GetReportDataAsync(LoanRepaymentRequestDto request)
        {
            try
            {
                var branchIds = SanitizeBranchIds(request.BranchIds);

                var connectionString = _context.Database.GetConnectionString();
                using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();

                var sqlFilterExp = new StringBuilder();
                string? memberName = null;

                // --------------------------------------------------------------
                // Build filter expression matching legacy BLL:
                // 1. Member filter
                // 2. Branch filter
                // 3. ORDER BY (always by LoanAccountNo per legacy)
                // --------------------------------------------------------------
                if (!string.IsNullOrWhiteSpace(request.MemberId))
                {
                    sqlFilterExp.Append(" And Mr.MemberId = '").Append(request.MemberId.Trim()).Append("'");

                    // Get member name for display
                    var name = await connection.QueryFirstOrDefaultAsync<string>(
                        @"SELECT FirstName + ' ' +
                                 CASE WHEN MiddleName = '' THEN '' ELSE MiddleName + ' ' END +
                                 LastName
                          FROM MemMemberRegistration 
                          WHERE MemberId = @MemberId AND IsActive = 1",
                        new { MemberId = request.MemberId.Trim() });
                    memberName = name;
                }

                if (!string.IsNullOrEmpty(branchIds))
                {
                    sqlFilterExp.Append(" And v.UsmOfficeId in (").Append(branchIds).Append(")");
                }

                // Legacy always appends " order by LoanAccountNo"
                sqlFilterExp.Append(" order by LoanAccountNo");

                var parameters = new DynamicParameters();
                parameters.Add("@SqlFilterExp", sqlFilterExp.ToString(), DbType.String, size: -1);

                var rows = await connection.QueryAsync<LoanRepaymentRowDto>(
                    "sp_7_16_LoanRepaymentAccountDetails",
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

                return new LoanRepaymentData
                {
                    Rows = resultList,
                    TotalRecords = resultList.Count,
                    TotalLoanIssueAmount = resultList.Sum(r => r.LoanIssueAmount ?? 0),
                    TotalPrincipleBalance = resultList.Sum(r => r.PrincipleBalance ?? 0),
                    BranchName = branchName,
                    MemberName = memberName,
                    MemberId = request.MemberId?.Trim()
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