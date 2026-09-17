// Repository/Loan/OtherReports/LoanPaymentRepository.cs
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
    public class LoanPaymentRepository : ILoanPaymentRepository
    {
        private readonly AppDbContext _context;
        private readonly IDateConverterService _dateConverter;
        private readonly ILogger<LoanPaymentRepository> _logger;

        public LoanPaymentRepository(
            AppDbContext context,
            IDateConverterService dateConverter,
            ILogger<LoanPaymentRepository> logger)
        {
            _context = context;
            _dateConverter = dateConverter;
            _logger = logger;
        }

        // --------------------------------------------------------------
        // @SqlFilterOrderBy — column names match the SP's final SELECT
        // Preserves the substring-based natural sort for MemberId and
        // LoanAccountNo exactly as in the legacy BLL.
        // --------------------------------------------------------------
        private static string BuildSqlOrderBy(LoanPaymentRequestDto request)
        {
            if (string.IsNullOrEmpty(request.OrderBy) || request.OrderBy == "-1")
                return string.Empty;

            return request.OrderBy.Trim() switch
            {
                "MemberId" => " order by substring(MemberId, 1,(len(MemberId)-CHARINDEX('-', MemberId))-1), MemberId ",
                "FullName" => " order by FullName ",
                "LoanAccountNo" => " order by substring(LoanAccountNo, 1,(len(LoanAccountNo)-CHARINDEX('-', LoanAccountNo))-1), LoanAccountNo ",
                "LoanTypeName" => " order by LoanTypeName",
                "LoanIssueAmount" => " order by LoanIssueAmount DESC",
                "LoanIssueDate" => " order by LoanIssueDate",
                "Period" => " order by Period",
                "InterestRate" => " order by InterestRate desc",
                "Amount" => " order by Amount",
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

        public async Task<LoanPaymentData> GetReportDataAsync(LoanPaymentRequestDto request)
        {
            try
            {
                var branchIds = SanitizeBranchIds(request.BranchIds);

                var connectionString = _context.Database.GetConnectionString();
                using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();

                var sqlFilterExp = new StringBuilder();
                var sqlFilterExpBranchId = new StringBuilder();
                string? memberGroupName = null;

                // --------------------------------------------------------------
                // Build filter expression matching legacy BLL:
                // 1. Branch + MemberGroup filter (@SqlFilterExpbranchId)
                // 2. Date range + PaymentBy filter (@SqlFilterExp)
                // 3. ORDER BY (@SqlFilterOrderBy)
                // --------------------------------------------------------------
                if (!string.IsNullOrEmpty(branchIds))
                {
                    sqlFilterExpBranchId.Append(" And v.UsmOfficeId in (").Append(branchIds).Append(")");
                }

                if (request.MemberGroupId != -1)
                {
                    sqlFilterExpBranchId.Append(" AND MR.SycMemberGroupId = ").Append(request.MemberGroupId);

                    // Get member group name for display
                    memberGroupName = await connection.QueryFirstOrDefaultAsync<string>(
                        "SELECT Name FROM SycMemberGroup WHERE SycMemberGroupId = @Id",
                        new { Id = request.MemberGroupId });
                }

                // Date range + PaymentBy filter
                if (!string.IsNullOrEmpty(request.FromDateBs) && !string.IsNullOrEmpty(request.ToDateBs))
                {
                    var fromDateAd = await _dateConverter.NepaliToEnglishAsync(request.FromDateBs);
                    var toDateAd = await _dateConverter.NepaliToEnglishAsync(request.ToDateBs);

                    var fromDateStr = fromDateAd.ToString("yyyy-MM-dd");
                    var toDateStr = toDateAd.ToString("yyyy-MM-dd");

                    sqlFilterExp.Append(" And tm.IssueDateAD between '").Append(fromDateStr)
                                .Append("' And '").Append(toDateStr).Append("'");

                    // Add paymentby filter only when supplied (legacy behavior)
                    if (!string.IsNullOrEmpty(request.PaymentBy))
                    {
                        sqlFilterExp.Append(" and tm.Paymentby = '").Append(request.PaymentBy).Append("'");
                    }
                }

                var sqlFilterOrderBy = BuildSqlOrderBy(request);

                var parameters = new DynamicParameters();
                parameters.Add("@SqlFilterExp", sqlFilterExp.ToString(), DbType.String, size: -1);
                parameters.Add("@SqlFilterExpbranchId", sqlFilterExpBranchId.ToString(), DbType.String, size: -1);
                parameters.Add("@SqlFilterOrderBy", sqlFilterOrderBy, DbType.String, size: -1);

                var rows = await connection.QueryAsync<LoanPaymentRowDto>(
                    "sp_7_16_LoanPaymentReport",
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

                return new LoanPaymentData
                {
                    Rows = resultList,
                    TotalRecords = resultList.Count,
                    TotalAmount = resultList.Sum(r => r.Amount ?? 0),
                    TotalLoanIssueAmount = resultList.Sum(r => r.LoanIssueAmount ?? 0),
                    FromDateBs = request.FromDateBs,
                    ToDateBs = request.ToDateBs,
                    BranchName = branchName,
                    MemberGroupName = memberGroupName,
                    PaymentBy = request.PaymentBy,
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