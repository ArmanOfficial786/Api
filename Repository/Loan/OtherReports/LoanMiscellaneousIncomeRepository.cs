// Repository/Loan/OtherReports/MiscellaneousIncomeRepository.cs
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
    public class LoanMiscellaneousIncomeRepository : ILoanMiscellaneousIncomeRepository
    {
        private readonly AppDbContext _context;
        private readonly IDateConverterService _dateConverter;
        private readonly ILogger<LoanMiscellaneousIncomeRepository> _logger;

        public LoanMiscellaneousIncomeRepository(
            AppDbContext context,
            IDateConverterService dateConverter,
            ILogger<LoanMiscellaneousIncomeRepository> logger)
        {
            _context = context;
            _dateConverter = dateConverter;
            _logger = logger;
        }

        // --------------------------------------------------------------
        // BuildSqlOrderBy — note the legacy BLL appends ORDER BY to the
        // branchId parameter (@SqlFilterExpbranchId), not a separate one.
        // Preserves the substring-based natural sort for MemberId and
        // LoanAccountNo exactly as in the legacy code.
        // --------------------------------------------------------------
        private static string BuildSqlOrderBy(string? orderBy)
        {
            if (string.IsNullOrEmpty(orderBy))
                return string.Empty;

            return orderBy.Trim() switch
            {
                "AccountNo" => " ORDER BY SUBSTRING(LoanAccountNo, 1, LEN(LoanAccountNo) - CHARINDEX('-', LoanAccountNo) - 1), LoanAccountNo",
                "A/CNo" => " ORDER BY SUBSTRING(LoanAccountNo, 1, LEN(LoanAccountNo) - CHARINDEX('-', LoanAccountNo) - 1), LoanAccountNo",
                "MemberId" => " ORDER BY SUBSTRING(MemberId, 1, LEN(MemberId) - CHARINDEX('-', MemberId) - 1), MemberId",
                "FullName" => " order by MemberName",
                "Amount" => " order by Amount desc",
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

        public async Task<LoanMiscellaneousIncomeData> GetReportDataAsync(LoanMiscellaneousIncomeRequestDto request)
        {
            try
            {
                var branchIds = SanitizeBranchIds(request.BranchIds);

                var connectionString = _context.Database.GetConnectionString();
                using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();

                var sqlFilterExp = new StringBuilder();
                var sqlFilterExpBranchId = new StringBuilder();
                string? memberName = null;
                string? memberGroupName = null;

                // --------------------------------------------------------------
                // Build filter expressions matching legacy BLL:
                // 1. MemberId filter (main @SqlFilterExp)
                // 2. Branch + MemberGroup + ORDER BY (@SqlFilterExpbranchId)
                // --------------------------------------------------------------
                if (!string.IsNullOrWhiteSpace(request.MemberId))
                {
                    sqlFilterExp.Append(" And m.MemberId = '").Append(request.MemberId.Trim()).Append("'");

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
                    sqlFilterExpBranchId.Append(" And v.UsmOfficeId in (").Append(branchIds).Append(")");
                }

                if (!string.IsNullOrEmpty(request.MemberGroupId) && request.MemberGroupId != "-1")
                {
                    sqlFilterExpBranchId.Append(" AND m.SycMemberGroupId = ").Append(request.MemberGroupId);

                    // Get member group name for display
                    memberGroupName = await connection.QueryFirstOrDefaultAsync<string>(
                        "SELECT Name FROM SycMemberGroup WHERE SycMemberGroupId = @Id",
                        new { Id = request.MemberGroupId });
                }

                // Append ORDER BY clause to the branch filter (legacy behavior)
                var orderByClause = BuildSqlOrderBy(request.OrderBy);
                sqlFilterExpBranchId.Append(orderByClause);

                var parameters = new DynamicParameters();
                parameters.Add("@SqlFilterExp", sqlFilterExp.ToString(), DbType.String, size: -1);
                parameters.Add("@SqlFilterExpbranchId", sqlFilterExpBranchId.ToString(), DbType.String, size: -1);

                var rows = await connection.QueryAsync<LoanMiscellaneousIncomeRowDto>(
                    "sp_7_16_MiscellaneousIncomeReport",
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

                return new LoanMiscellaneousIncomeData
                {
                    Rows = resultList,
                    TotalRecords = resultList.Count,
                    TotalAmount = resultList.Sum(r => r.Amount ?? 0),
                    BranchName = branchName,
                    MemberName = memberName,
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