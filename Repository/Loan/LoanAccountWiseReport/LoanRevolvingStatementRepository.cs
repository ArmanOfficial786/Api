// Repository/Loan/OtherReports/LoanRevolvingStatementRepository.cs
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
    public class LoanRevolvingStatementRepository : ILoanRevolvingStatementRepository
    {
        private readonly AppDbContext _context;
        private readonly IDateConverterService _dateConverter;
        private readonly ILogger<LoanRevolvingStatementRepository> _logger;

        public LoanRevolvingStatementRepository(
            AppDbContext context,
            IDateConverterService dateConverter,
            ILogger<LoanRevolvingStatementRepository> logger)
        {
            _context = context;
            _dateConverter = dateConverter;
            _logger = logger;
        }

        public async Task<LoanRevolvingStatementData> GetReportDataAsync(LoanRevolvingStatementRequestDto request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.AccountNo))
                {
                    throw new ArgumentException("Account No is required.");
                }

                var connectionString = _context.Database.GetConnectionString();
                using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();

                var sqlFilterExp = new StringBuilder();
                var sqlFilterExpDate = new StringBuilder();

                string? memberName = null;
                string? memberId = null;
                string? verifiedTill = null;

                // --------------------------------------------------------------
                // Build filter expression matching legacy BLL:
                // 1. Account No filter
                // 2. Date range filter
                // --------------------------------------------------------------
                sqlFilterExp.Append(" And LS.loanAccountNo = '").Append(request.AccountNo.Trim()).Append("'");

                if (!string.IsNullOrEmpty(request.FromDateBs) && !string.IsNullOrEmpty(request.ToDateBs)
                    && request.FromDateBs != "-1" && request.ToDateBs != "-1")
                {
                    var fromDateAd = await _dateConverter.NepaliToEnglishAsync(request.FromDateBs);
                    var toDateAd = await _dateConverter.NepaliToEnglishAsync(request.ToDateBs);

                    var fromDateStr = fromDateAd.ToString("yyyy-MM-dd");
                    var toDateStr = toDateAd.ToString("yyyy-MM-dd");

                    sqlFilterExpDate.Append(" And Tr.DateOn between '").Append(fromDateStr)
                                    .Append("' And '").Append(toDateStr).Append("'");
                }

                // --------------------------------------------------------------
                // Get member info for display
                // --------------------------------------------------------------
                var memberInfo = await connection.QueryFirstOrDefaultAsync<dynamic>(
                    @"SELECT TOP 1 
                            MR.MemberId, 
                            MR.FirstName + ' ' + ISNULL(MR.MiddleName, '') + ' ' + MR.LastName AS MemberName
                      FROM LmtLoanIssue LS
                      INNER JOIN MemMemberRegistration MR ON MR.MemMemberRegistrationId = LS.MemMemberRegistrationId
                      WHERE LS.LoanAccountNo = @AccountNo",
                    new { AccountNo = request.AccountNo.Trim() });

                if (memberInfo != null)
                {
                    memberId = memberInfo.MemberId;
                    memberName = memberInfo.MemberName;
                }

                var parameters = new DynamicParameters();
                parameters.Add("@SqlFilterExp", sqlFilterExp.ToString(), DbType.String, size: -1);
                parameters.Add("@SqlFilterExpdate", sqlFilterExpDate.ToString(), DbType.String, size: -1);

                var rows = await connection.QueryAsync<LoanRevolvingStatementRowDto>(
                    "sp_7_16_LoanStatementOverDraftReport",
                    parameters,
                    commandType: CommandType.StoredProcedure,
                    commandTimeout: 120
                );

                var resultList = rows.AsList();

                // Get verification info if available
                var verification = await connection.QueryFirstOrDefaultAsync<dynamic>(
                    @"SELECT TOP 1 VerifiedDateOnBs 
                      FROM LmtLoanStatementVerification 
                      WHERE LmtLoanIssueId = (SELECT TOP 1 LmtLoanIssueId FROM LmtLoanIssue WHERE LoanAccountNo = @AccountNo)
                      ORDER BY LmtLoanStatementVerificationId DESC",
                    new { AccountNo = request.AccountNo.Trim() });

                if (verification != null)
                {
                    verifiedTill = verification.VerifiedDateOnBs;
                }

                // Calculate opening balance (first row's balance before first transaction)
                var openingBalance = resultList.FirstOrDefault()?.Balance ?? 0;

                // Calculate closing balance (last row's balance)
                var closingBalance = resultList.LastOrDefault()?.Balance ?? 0;

                return new LoanRevolvingStatementData
                {
                    Rows = resultList,
                    TotalRecords = resultList.Count,
                    TotalDebitAmount = resultList.Sum(r => r.DebitAmount ?? 0),
                    TotalCreditAmount = resultList.Sum(r => r.CreditAmount ?? 0),
                    OpeningBalance = openingBalance,
                    ClosingBalance = closingBalance,
                    FromDateBs = request.FromDateBs,
                    ToDateBs = request.ToDateBs,
                    AccountNo = request.AccountNo.Trim(),
                    MemberName = memberName,
                    MemberId = memberId,
                    VerifiedTill = verifiedTill
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