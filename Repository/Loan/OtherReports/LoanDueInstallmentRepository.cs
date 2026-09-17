// Repository/Loan/OtherReports/LoanDueInstallmentRepository.cs
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
    public class LoanDueInstallmentRepository : ILoanDueInstallmentRepository
    {
        private readonly AppDbContext _context;
        private readonly IDateConverterService _dateConverter;
        private readonly ILogger<LoanDueInstallmentRepository> _logger;

        public LoanDueInstallmentRepository(
            AppDbContext context,
            IDateConverterService dateConverter,
            ILogger<LoanDueInstallmentRepository> logger)
        {
            _context = context;
            _dateConverter = dateConverter;
            _logger = logger;
        }

        // --------------------------------------------------------------
        // @sqlFilterExpOrderby — column names match the SP's final SELECT
        // Preserves the legacy substring-based natural sort for
        // MemberId / LoanAccountNo (strips a trailing "-N" suffix before
        // sorting) exactly as in the BLL.
        // --------------------------------------------------------------
        private static string BuildSqlOrderBy(LoanDueInstallmentRequestDto request)
        {
            if (string.IsNullOrEmpty(request.OrderBy) || request.OrderBy == "-1")
                return string.Empty;

            return request.OrderBy.Trim() switch
            {
                "MemberId" => " substring(MemberId, 1,(len(MemberId)-charindex('-', MemberId))-1), MemberId ",
                "FullName" => " FullName",
                "LoanAccountNo" => " substring(LoanAccountNo, 1,(len(LoanAccountNo)-charindex('-', LoanAccountNo))-1), LoanAccountNo ",
                "LoanTypeName" => " LoanTypeName",
                "PrincipleAmount" => "  PrincipleAmount DESC",
                "InterestAmount" => " InterestAmount DESC",
                "InstallmentAmount" => " InstallmentAmount DESC",
                "DateOnBS" => " DateOnBS",
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

        public async Task<LoanDueInstallmentData> GetReportDataAsync(LoanDueInstallmentRequestDto request)
        {
            try
            {
                var branchIds = SanitizeBranchIds(request.BranchIds);

                var connectionString = _context.Database.GetConnectionString();
                using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();

                var sqlFilterExp = new StringBuilder();
                var sqlFilterExpBranchId = new StringBuilder();
                var sqlFilterExpMemberGroup = new StringBuilder();
                string? memberName = null;
                string? memberGroupName = null;
                string? paymentDurationTypeName = null;

                // --------------------------------------------------------------
                // Build filter expressions matching legacy BLL:
                // 1. Branch filter (separate parameter)
                // 2. MemberId + Date range filter (main filter)
                // 3. Member group filter (separate parameter)
                // 4. Payment Duration Type (handled inside SP)
                // --------------------------------------------------------------
                if (!string.IsNullOrEmpty(branchIds))
                {
                    sqlFilterExpBranchId.Append(" And v.UsmOfficeId in (").Append(branchIds).Append(")");
                }

                // Date range is always applied when dates are provided
                string? fromDateStr = null;
                string? toDateStr = null;

                if (!string.IsNullOrEmpty(request.FromDateBs) && !string.IsNullOrEmpty(request.ToDateBs))
                {
                    var fromDateAd = await _dateConverter.NepaliToEnglishAsync(request.FromDateBs);
                    var toDateAd = await _dateConverter.NepaliToEnglishAsync(request.ToDateBs);
                    fromDateStr = fromDateAd.ToString("yyyy-MM-dd");
                    toDateStr = toDateAd.ToString("yyyy-MM-dd");
                }

                if (!string.IsNullOrWhiteSpace(request.MemberId))
                {
                    // Member + Date range
                    sqlFilterExp.Append(" And Mr.MemberId = '").Append(request.MemberId.Trim()).Append("'");
                    if (!string.IsNullOrEmpty(fromDateStr) && !string.IsNullOrEmpty(toDateStr))
                    {
                        sqlFilterExp.Append(" And Tr.ScheduleDateOn between '").Append(fromDateStr)
                                    .Append("' And '").Append(toDateStr).Append("'");
                    }

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
                else
                {
                    // Date range only (All members)
                    if (!string.IsNullOrEmpty(fromDateStr) && !string.IsNullOrEmpty(toDateStr))
                    {
                        sqlFilterExp.Append(" And Tr.ScheduleDateOn between '").Append(fromDateStr)
                                    .Append("' And '").Append(toDateStr).Append("'");
                    }
                }

                if (request.MemberGroupId != -1)
                {
                    sqlFilterExpMemberGroup.Append(" AND MR.SycMemberGroupId = ").Append(request.MemberGroupId);

                    // Get member group name for display
                    memberGroupName = await connection.QueryFirstOrDefaultAsync<string>(
                        "SELECT Name FROM SycMemberGroup WHERE SycMemberGroupId = @Id",
                        new { Id = request.MemberGroupId });
                }

                // Get payment duration type name for display
                if (request.LmtPaymentDurationTypeId != -1)
                {
                    paymentDurationTypeName = await connection.QueryFirstOrDefaultAsync<string>(
                        "SELECT PaymentDurationType FROM LmtPaymentDurationType WHERE LmtPaymentDurationTypeId = @Id",
                        new { Id = request.LmtPaymentDurationTypeId });
                }

                var sqlFilterExpOrderby = BuildSqlOrderBy(request);

                var parameters = new DynamicParameters();
                parameters.Add("@SqlFilterExp", sqlFilterExp.ToString(), DbType.String, size: -1);
                parameters.Add("@LmtPaymentDurationTypeId", request.LmtPaymentDurationTypeId);
                parameters.Add("@SqlFilterExpbranchId", sqlFilterExpBranchId.ToString(), DbType.String, size: -1);
                parameters.Add("@SqlFilterExpMemberGroup", sqlFilterExpMemberGroup.ToString(), DbType.String, size: -1);
                parameters.Add("@sqlFilterExpOrderby", sqlFilterExpOrderby, DbType.String, size: -1);

                var rows = await connection.QueryAsync<LoanDueInstallmentRowDto>(
                    "sp_7_16_LoanDueInstallmentReport",
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

                return new LoanDueInstallmentData
                {
                    Rows = resultList,
                    TotalRecords = resultList.Count,
                    TotalLoanIssueAmount = resultList.Sum(r => r.LoanIssueAmount ?? 0),
                    TotalPrincipleAmount = resultList.Sum(r => r.PrincipleAmount ?? 0),
                    TotalInterestAmount = resultList.Sum(r => r.InterestAmount ?? 0),
                    TotalInstallmentAmount = resultList.Sum(r => r.InstallmentAmount ?? 0),
                    TotalBalanceAmount = resultList.Sum(r => r.BalanceAmount ?? 0),
                    FromDateBs = request.FromDateBs,
                    ToDateBs = request.ToDateBs,
                    BranchNames = branchName,
                    MemberName = memberName,
                    MemberGroupName = memberGroupName,
                    PaymentDurationTypeName = paymentDurationTypeName,
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