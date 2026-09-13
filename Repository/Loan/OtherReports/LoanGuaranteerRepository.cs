// Repository/Loan/OtherReports/LoanGuaranteerRepository.cs
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
    public class LoanGuaranteerRepository : ILoanGuaranteerRepository
    {
        private readonly AppDbContext _context;
        private readonly IDateConverterService _dateConverter;
        private readonly ILogger<LoanGuaranteerRepository> _logger;

        public LoanGuaranteerRepository(
            AppDbContext context,
            IDateConverterService dateConverter,
            ILogger<LoanGuaranteerRepository> logger)
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
        private static string BuildSqlOrderBy(LoanGuaranteerRequestDto request)
        {
            if (string.IsNullOrEmpty(request.OrderBy) || request.OrderBy == "-1")
                return string.Empty;

            return request.OrderBy.Trim() switch
            {
                "MrL.MemberId" => " order by substring(MrL.MemberId, 1,(len(MrL.MemberId)-charindex('-', MrL.MemberId))-1), MrL.MemberId ",
                "LoneeFullName" => " order by LoneeFullName ",
                "LoanAccountNo" => " order by substring(LoanAccountNo, 1,(len(LoanAccountNo)-charindex('-', LoanAccountNo))-1), LoanAccountNo ",
                "AccountNo" => " order by AccountNo",
                "GuaranteeAmount" => " order by GuaranteeAmount DESC",
                "GuaranteeShareAmount" => " order by GuaranteeShareAmount DESC",
                "GuaranteeDateOnBs" => " order by GuaranteeDateOnBs",
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

        public async Task<LoanGuaranteerData> GetReportDataAsync(LoanGuaranteerRequestDto request)
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
                // 3. ORDER BY (@SqlFilterExpOrderBy)
                // --------------------------------------------------------------
                sqlFilterExp.Append(" And MR.MemberId = '").Append(request.MemberId.Trim()).Append("'");

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

                var sqlFilterExpOrderBy = BuildSqlOrderBy(request);

                var parameters = new DynamicParameters();
                parameters.Add("@SqlFilterExp", sqlFilterExp.ToString(), DbType.String, size: -1);
                parameters.Add("@SqlFilterExpbranchId", sqlFilterExpBranchId.ToString(), DbType.String, size: -1);
                parameters.Add("@SqlFilterExpOrderBy", sqlFilterExpOrderBy, DbType.String, size: -1);

                var rows = await connection.QueryAsync<LoanGuaranteerRowDto>(
                    "sp_7_16_LoanGuaranteerReport",
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

                return new LoanGuaranteerData
                {
                    Rows = resultList,
                    TotalRecords = resultList.Count,
                    TotalGuaranteeAmount = resultList.Sum(r => r.GuaranteeAmount ?? 0),
                    TotalGuaranteeShareAmount = resultList.Sum(r => r.GuaranteeShareAmount ?? 0),
                    BranchName = branchName,
                    MemberName = memberName,
                    MemberId = request.MemberId.Trim(),
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