// Repository/Loan/OtherReports/LoanAppraisalRepository.cs
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
    public class LoanAppraisalRepository : ILoanAppraisalRepository
    {
        private readonly AppDbContext _context;
        private readonly IDateConverterService _dateConverter;
        private readonly ILogger<LoanAppraisalRepository> _logger;

        public LoanAppraisalRepository(
            AppDbContext context,
            IDateConverterService dateConverter,
            ILogger<LoanAppraisalRepository> logger)
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

        public async Task<LoanAppraisalData> GetReportDataAsync(LoanAppraisalRequestDto request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.MemberId))
                {
                    throw new ArgumentException("MemberId is required.");
                }

                var branchIds = SanitizeBranchIds(request.BranchIds);

                var connectionString = _context.Database.GetConnectionString();
                using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();

                var sqlFilterExp = new StringBuilder();
                var sqlFilterExpBranchId = new StringBuilder();
                string? memberName = null;

                // --------------------------------------------------------------
                // Build filter expression matching legacy BLL:
                // 1. Member filter (@SqlFilterExp)
                // 2. Branch filter (@SqlFilterExpbranchId)
                // --------------------------------------------------------------
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

                if (!string.IsNullOrEmpty(branchIds))
                {
                    sqlFilterExpBranchId.Append(" And v.UsmOfficeId in (").Append(branchIds).Append(")");
                }

                var parameters = new DynamicParameters();
                parameters.Add("@SqlFilterExp", sqlFilterExp.ToString(), DbType.String, size: -1);
                parameters.Add("@SqlFilterExpbranchId", sqlFilterExpBranchId.ToString(), DbType.String, size: -1);

                var rows = await connection.QueryAsync<LoanAppraisalRowDto>(
                    "sp_7_16_LoanAppraisalReport",
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

                return new LoanAppraisalData
                {
                    Rows = resultList,
                    TotalRecords = resultList.Count,
                    TotalCollateralAmount = resultList.Sum(r => r.CollateralAmount ?? 0),
                    TotalCollateralSanctionAmount = resultList.Sum(r => r.CollateralSanctionAmount ?? 0),
                    BranchName = branchName,
                    MemberName = memberName,
                    MemberId = request.MemberId.Trim()
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