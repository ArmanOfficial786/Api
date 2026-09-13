// Repository/Loan/OtherReports/LoanPenaltyDiscountRepository.cs
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
    public class LoanPenaltyDiscountRepository : ILoanPenaltyDiscountRepository
    {
        private readonly AppDbContext _context;
        private readonly IDateConverterService _dateConverter;
        private readonly ILogger<LoanPenaltyDiscountRepository> _logger;

        public LoanPenaltyDiscountRepository(
            AppDbContext context,
            IDateConverterService dateConverter,
            ILogger<LoanPenaltyDiscountRepository> logger)
        {
            _context = context;
            _dateConverter = dateConverter;
            _logger = logger;
        }

        // --------------------------------------------------------------
        // @SqlFilterExp — column names match the SP's final SELECT
        // aliases. Preserves the legacy substring-based natural sort for
        // MemberId / LoanAccountNo (strips a trailing "-N" suffix before
        // sorting) exactly as in the BLL.
        // --------------------------------------------------------------
        private static string BuildSqlOrderBy(LoanPenaltyDiscountRequestDto request)
        {
            return request.OrderBy?.Trim() switch
            {
                "Date" => " ORDER BY TransactionOnBs",
                "MemberId" => " ORDER BY SUBSTRING(MemberId, 1, LEN(MemberId) - CHARINDEX('-', MemberId) - 1), MemberId",
                "FullName" => " order by FullName",
                "LoanType" => " order by LoanTypeName",
                "LoanAcAmount" => " ORDER BY SUBSTRING(LoanAccountNo, 1, LEN(LoanAccountNo) - CHARINDEX('-', LoanAccountNo) - 1), LoanAccountNo",
                "Amount" => " ORDER BY CashReceived",
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

        public async Task<LoanPenaltyDiscountData> GetReportDataAsync(LoanPenaltyDiscountRequestDto request)
        {
            try
            {
                var branchIds = SanitizeBranchIds(request.BranchIds);

                var connectionString = _context.Database.GetConnectionString();
                using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();

                var sqlFilterExp = new StringBuilder();
                string? memberName = null;
                string? loanTypeName = null;

                // --------------------------------------------------------------
                // Build filter expression - matching legacy BLL order:
                // 1. MemberId filter
                // 2. Branch filter
                // 3. LoanType filter
                // 4. Date range filter
                // 5. ORDER BY
                // --------------------------------------------------------------
                if (!string.IsNullOrWhiteSpace(request.MemberId))
                {
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
                }

                if (!string.IsNullOrEmpty(branchIds))
                {
                    sqlFilterExp.Append(" And LIs.UsmOfficeId in (").Append(branchIds).Append(")");
                }

                if (request.LoanTypeId != -1)
                {
                    sqlFilterExp.Append(" And LTMr.LmtLoanTypeMasterId = ").Append(request.LoanTypeId);

                    // Get loan type name for display
                    loanTypeName = await connection.QueryFirstOrDefaultAsync<string>(
                        "SELECT LoanTypeName FROM LmtLoanTypeMaster WHERE LmtLoanTypeMasterId = @Id",
                        new { Id = request.LoanTypeId });
                }

                if (!string.IsNullOrEmpty(request.FromDateBs) && !string.IsNullOrEmpty(request.ToDateBs))
                {
                    var fromDateAd = await _dateConverter.NepaliToEnglishAsync(request.FromDateBs);
                    var toDateAd = await _dateConverter.NepaliToEnglishAsync(request.ToDateBs);

                    // ISO format avoids SQL Server regional/language ambiguity for string->date literals
                    var fromDateStr = fromDateAd.ToString("yyyy-MM-dd");
                    var toDateStr = toDateAd.ToString("yyyy-MM-dd");

                    sqlFilterExp.Append(" And Ac.TransactionOn >= '").Append(fromDateStr).Append("'");
                    sqlFilterExp.Append(" And Ac.TransactionOn <= '").Append(toDateStr).Append("'");
                }

                // Append ORDER BY clause
                var sqlOrderBy = BuildSqlOrderBy(request);
                sqlFilterExp.Append(sqlOrderBy);

                var parameters = new DynamicParameters();
                parameters.Add("@SqlFilterExp", sqlFilterExp.ToString(), DbType.String, size: -1);

                var rows = await connection.QueryAsync<LoanPenaltyDiscountRowDto>(
                    "sp_7_16_LoanPenaltyDiscountReport",
                    parameters,
                    commandType: CommandType.StoredProcedure,
                    commandTimeout: 120
                );

                var resultList = rows.AsList();

                string branchName = "All";
                if (!string.IsNullOrEmpty(branchIds))
                {
                    var names = await connection.QueryAsync<string>(
                        $"SELECT OfficeName FROM UsmOffice WHERE UsmOfficeId IN ({branchIds})");
                    var nameList = names.Where(n => !string.IsNullOrWhiteSpace(n)).ToList();
                    branchName = nameList.Count > 0 ? string.Join(", ", nameList) : "All";
                }

                return new LoanPenaltyDiscountData
                {
                    Rows = resultList,
                    TotalRecords = resultList.Count,
                    TotalAmount = resultList.Sum(r => r.CashReceived ?? 0),
                    FromDateBs = request.FromDateBs,
                    ToDateBs = request.ToDateBs,
                    BranchName = branchName,
                    MemberName = memberName,
                    LoanTypeName = loanTypeName,
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