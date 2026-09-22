
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
    public class LoanStatementRepository : ILoanStatementRepository
    {
        private readonly AppDbContext _context;
        private readonly IDateConverterService _dateConverter;
        private readonly ILogger<LoanStatementRepository> _logger;

        public LoanStatementRepository(
            AppDbContext context,
            IDateConverterService dateConverter,
            ILogger<LoanStatementRepository> logger)
        {
            _context = context;
            _dateConverter = dateConverter;
            _logger = logger;
        }

        private static string SanitizeBranchIds(string? branchIds)
        {
            if (string.IsNullOrWhiteSpace(branchIds) || branchIds == "-1" || branchIds == "string")
                return string.Empty;

            var validIds = branchIds
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Where(id => long.TryParse(id, out _));

            return string.Join(",", validIds);
        }

        public async Task<LoanStatementData> GetReportDataAsync(LoanStatementRequestDto request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.AccountNo))
                {
                    throw new ArgumentException("Account No is required.");
                }

                var branchIds = SanitizeBranchIds(request.BranchIds);

                var connectionString = _context.Database.GetConnectionString();
                using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();


                var memberInfoSp = request.ReportType == "Nepali"
                    ? "sp_7_16_MemberInfoDetailsByAccountNoReportNepali"
                    : "sp_7_16_MemberInfoDetailsByAccountNoReport";

                var memberInfoParams = new DynamicParameters();
                memberInfoParams.Add("@SqlFilterExp", $" AND ls.LoanAccountNo = '{request.AccountNo.Trim()}'", DbType.String, size: -1);

                var memberInfo = await connection.QueryFirstOrDefaultAsync<LoanStatementMemberInfoDto>(
                    memberInfoSp,
                    memberInfoParams,
                    commandType: CommandType.StoredProcedure,
                    commandTimeout: 120
                );

                if (memberInfo == null)
                {
                    throw new ArgumentException($"No loan account found for Account No '{request.AccountNo}'.");
                }


                var sqlFilterExp = new StringBuilder();
                var sqlFilterExpDate = new StringBuilder();
                var sqlFilterExpTillDate = new StringBuilder();
                var calOpeningBalance = "0";

                sqlFilterExp.Append(" And LS.loanAccountNo = '").Append(request.AccountNo.Trim()).Append("'");

                if (!string.IsNullOrEmpty(request.FromDateBs) && !string.IsNullOrEmpty(request.ToDateBs)
                    && request.FromDateBs != "-1" && request.ToDateBs != "-1")
                {
                    var fromDateAd = await _dateConverter.NepaliToEnglishAsync(request.FromDateBs);
                    var toDateAd = await _dateConverter.NepaliToEnglishAsync(request.ToDateBs);

                    var fromDateStr = fromDateAd.ToString("yyyy-MM-dd");
                    var toDateStr = toDateAd.ToString("yyyy-MM-dd");

                    sqlFilterExpDate.Append(" And Tr.TransactionOn between '").Append(fromDateStr)
                                    .Append("' And '").Append(toDateStr).Append("'");
                    sqlFilterExpTillDate.Append(" And Tr.TransactionOn < '").Append(fromDateStr).Append("'");
                    calOpeningBalance = "1";
                }

                var sqlFilterExpBranchId = new StringBuilder();
                if (!string.IsNullOrEmpty(branchIds))
                {
                    sqlFilterExpBranchId.Append(" And v.UsmOfficeId = ").Append(branchIds);
                }


                var transactionSp = request.ReportType == "Nepali"
                    ? (request.ShowDetailNarration ? "sp_7_16_LoanStatementReportDetailNepali" : "sp_7_16_LoanStatementReportNormalNepali")
                    : (request.ShowDetailNarration ? "sp_7_16_LoanStatementReportDetail" : "sp_7_16_LoanStatementReportNormal");

                var transactionParams = new DynamicParameters();
                transactionParams.Add("@SqlFilterExp", sqlFilterExp.ToString(), DbType.String, size: -1);
                transactionParams.Add("@SqlFilterExpdate", sqlFilterExpDate.ToString(), DbType.String, size: -1);
                transactionParams.Add("@SqlFilterExpTilldate", sqlFilterExpTillDate.ToString(), DbType.String, size: -1);
                transactionParams.Add("@SqlFilterExpbranchId", sqlFilterExpBranchId.ToString(), DbType.String, size: -1);
                transactionParams.Add("@CalOpeningBalance", calOpeningBalance);
                transactionParams.Add("@SqlEnteryByExp", request.ShowEntryBy ? "1" : "0");

                var transactions = (await connection.QueryAsync<LoanStatementTransactionDto>(
                    transactionSp,
                    transactionParams,
                    commandType: CommandType.StoredProcedure,
                    commandTimeout: 120
                )).AsList();


                var guaranteeSp = request.ReportType == "Nepali"
                    ? "sp_7_16_GetLoanStatementGuaranteeDetailNepali"
                    : "sp_7_16_GetLoanStatementGuaranteeDetail";

                var guaranteeParams = new DynamicParameters();
                guaranteeParams.Add("@SqlFilterExp", $" and lli.LoanAccountNo = '{request.AccountNo.Trim()}'", DbType.String, size: -1);

                var guarantees = (await connection.QueryAsync<LoanStatementGuaranteeDto>(
                    guaranteeSp,
                    guaranteeParams,
                    commandType: CommandType.StoredProcedure,
                    commandTimeout: 120
                )).AsList();


                var closingDues = new List<LoanStatementClosingDueDto>();
                if (request.ShowAccountCloseDetails && !string.IsNullOrEmpty(request.ToDateBs))
                {
                    var toDateAd = await _dateConverter.NepaliToEnglishAsync(request.ToDateBs);
                    var sqlTillDate = $"'{toDateAd:yyyy-MM-dd}'";


                    string penaltyPaymentMethod = "PI";


                    var dueParams = new DynamicParameters();
                    dueParams.Add("@SqlFilterExp1", $"'{request.AccountNo.Trim()}'");
                    dueParams.Add("@SqlFilterExp2", "'AOD'");
                    dueParams.Add("@SqlFilterExp3", "'ACP'");
                    dueParams.Add("@SqlFilterExp4", $"'{penaltyPaymentMethod}'");
                    dueParams.Add("@SqlFilterExp5", "'0'");
                    dueParams.Add("@SqlTillDate", sqlTillDate);

                    var ds = await connection.QueryMultipleAsync(
                        "sp_7_16_LoanInstallmentForAccountClosingDueDetails",
                        dueParams,
                        commandType: CommandType.StoredProcedure,
                        commandTimeout: 120
                    );


                    var firstTable = (await ds.ReadAsync<LoanStatementClosingDueDto>()).AsList();
                    if (firstTable.Any())
                    {
                        closingDues = firstTable;
                    }
                    else
                    {
                        var secondTable = (await ds.ReadAsync<LoanStatementClosingDueDto>()).AsList();
                        closingDues = secondTable;
                    }
                }


                string? verifiedTill = null;
                var verification = await connection.QueryFirstOrDefaultAsync<string>(
                    @"SELECT TOP 1 lsv.VerifiedDateOnBs 
                      FROM LmtLoanStatementVerification lsv
                      INNER JOIN LmtLoanIssue li ON li.LmtLoanIssueId = lsv.LmtLoanIssueId
                      WHERE li.LoanAccountNo = @AccountNo
                      ORDER BY lsv.VerifiedDateOn DESC",
                    new { AccountNo = request.AccountNo.Trim() });

                verifiedTill = verification;

                var openingBalance = 0m;
                var closingBalance = 0m;

                if (transactions.Any())
                {

                    if (calOpeningBalance == "1")
                    {

                        var firstRow = transactions.First();
                        openingBalance = (firstRow.Balance ?? 0) - (firstRow.DebitAmount ?? 0) + (firstRow.CreditAmount ?? 0);
                    }
                    closingBalance = transactions.Last().Balance ?? 0;
                }

                var branchName = "All";
                if (!string.IsNullOrEmpty(branchIds))
                {
                    var names = await connection.QueryAsync<string>(
                        $"SELECT OfficeName FROM UsmOffice WHERE UsmOfficeId IN ({branchIds})");
                    var nameList = names.Where(n => !string.IsNullOrWhiteSpace(n)).ToList();
                    branchName = nameList.Count > 0 ? string.Join(", ", nameList) : "All";
                }

                return new LoanStatementData
                {
                    MemberInfo = memberInfo,
                    Transactions = transactions,
                    Guarantees = guarantees,
                    ClosingDues = closingDues,
                    TotalRecords = transactions.Count,
                    TotalDebitAmount = transactions.Sum(t => t.DebitAmount ?? 0),
                    TotalCreditAmount = transactions.Sum(t => t.CreditAmount ?? 0),
                    OpeningBalance = openingBalance,
                    ClosingBalance = closingBalance,
                    FromDateBs = request.FromDateBs,
                    ToDateBs = request.ToDateBs,
                    BranchName = branchName,
                    ReportType = request.ReportType,
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