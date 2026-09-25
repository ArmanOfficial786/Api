
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
    public class LoanStatementDateWiseRepository : ILoanStatementDateWiseRepository
    {
        private readonly AppDbContext _context;
        private readonly IDateConverterService _dateConverter;
        private readonly ILogger<LoanStatementDateWiseRepository> _logger;

        public LoanStatementDateWiseRepository(
            AppDbContext context,
            IDateConverterService dateConverter,
            ILogger<LoanStatementDateWiseRepository> logger)
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

        public async Task<LoanStatementDateWiseData> GetReportDataAsync(LoanStatementDateWiseRequestDto request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.AccountNo))
                {
                    throw new ArgumentException("Account No is required.");
                }

                if (string.IsNullOrEmpty(request.FromDateBs) || string.IsNullOrEmpty(request.ToDateBs)
                    || request.FromDateBs == "-1" || request.ToDateBs == "-1")
                {
                    throw new ArgumentException("From Date and To Date are required.");
                }

                var branchIds = SanitizeBranchIds(request.BranchIds);

                var connectionString = _context.Database.GetConnectionString();
                using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();

                var memberInfoParams = new DynamicParameters();
                memberInfoParams.Add("@SqlFilterExp", $" AND ls.LoanAccountNo = '{request.AccountNo.Trim()}'", DbType.String, size: -1);

                var memberInfo = await connection.QueryFirstOrDefaultAsync<LoanStatementDateWiseMemberInfoDto>(
                    "sp_7_16_MemberInfoDetailsByAccountNoReport",
                    memberInfoParams,
                    commandType: CommandType.StoredProcedure,
                    commandTimeout: 120
                );

                if (memberInfo == null)
                {
                    throw new ArgumentException($"No loan account found for Account No '{request.AccountNo}'.");
                }

                var sqlFilterExp = new StringBuilder();
                var sqlFilterExpBranchId = new StringBuilder();
                var sqlFilterDate = new StringBuilder();

                sqlFilterExp.Append(" And LS.loanAccountNo = '").Append(request.AccountNo.Trim()).Append("'");

                if (!string.IsNullOrEmpty(branchIds))
                {
                    sqlFilterExpBranchId.Append(" And v.UsmOfficeId = ").Append(branchIds);
                }

                var fromDateAd = await _dateConverter.NepaliToEnglishAsync(request.FromDateBs);
                var toDateAd = await _dateConverter.NepaliToEnglishAsync(request.ToDateBs);

                var fromDateStr = fromDateAd.ToString("yyyy-MM-dd");
                var toDateStr = toDateAd.ToString("yyyy-MM-dd");

                sqlFilterDate.Append(" And Tr.TransactionOn >= '").Append(fromDateStr)
                            .Append("' And Tr.TransactionOn <= '").Append(toDateStr).Append("'");

                var parameters = new DynamicParameters();
                parameters.Add("@SqlFilterExp", sqlFilterExp.ToString(), DbType.String, size: -1);
                parameters.Add("@SqlFilterExpbranchId", sqlFilterExpBranchId.ToString(), DbType.String, size: -1);
                parameters.Add("@SqlFilterDate", sqlFilterDate.ToString(), DbType.String, size: -1);

                var rows = (await connection.QueryAsync<LoanStatementDateWiseRowDto>(
                    "sp_7_16_LoanStatmentDetailsDateWise",
                    parameters,
                    commandType: CommandType.StoredProcedure,
                    commandTimeout: 120
                )).AsList();


                string branchName = "All";
                if (!string.IsNullOrEmpty(branchIds))
                {
                    var names = await connection.QueryAsync<string>(
                        $"SELECT OfficeName FROM UsmOffice WHERE UsmOfficeId IN ({branchIds})");
                    var nameList = names.Where(n => !string.IsNullOrWhiteSpace(n)).ToList();
                    branchName = nameList.Count > 0 ? string.Join(", ", nameList) : "All";
                }


                return new LoanStatementDateWiseData
                {
                    MemberInfo = memberInfo,
                    Rows = rows,
                    TotalRecords = rows.Count,
                    TotalLoanIssueAmount = rows.Sum(r => r.LoanIssueAmount ?? 0),
                    TotalPrincipal = rows.Sum(r => r.Principal ?? 0),
                    TotalInterest = rows.Sum(r => r.Interest ?? 0),
                    TotalFine = rows.Sum(r => r.Fine ?? 0),
                    TotalInstallmentAmt = rows.Sum(r => r.InstallmentAmt ?? 0),
                    TotalBalanceAmount = rows.LastOrDefault()?.BalanceAmount ?? 0,
                    FromDateBs = request.FromDateBs,
                    ToDateBs = request.ToDateBs,
                    BranchName = branchName
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