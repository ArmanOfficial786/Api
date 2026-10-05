
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using NexgenCosysReport.DbContext;
using NexgenCosysReport.Dtos.RequestDtos.Loan.LoanAnalysisReport;
using NexgenCosysReport.Inteface.ServiceInterface.Loan.LoanAnalysisReport;
using System.Data;

namespace NexgenCosysReport.Repository.Loan.LoanAnalysisReport
{
    public class LoanPortfolioRepository : ILoanPortfolioRepository
    {
        private readonly AppDbContext _context;
        private readonly ILogger<LoanPortfolioRepository> _logger;

        public LoanPortfolioRepository(
            AppDbContext context,
            ILogger<LoanPortfolioRepository> logger)
        {
            _context = context;
            _logger = logger;
        }

        private static string SanitizeBranchIds(string? branchIds)
        {
            if (string.IsNullOrWhiteSpace(branchIds) || branchIds == "-1" || branchIds == "string")
                return "-1";

            var validIds = branchIds
                .Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Where(id => long.TryParse(id, out _));

            var joined = string.Join(",", validIds);
            return string.IsNullOrEmpty(joined) ? "-1" : joined;
        }

        public async Task<LoanPortfolioData> GetReportDataAsync(LoanPortfolioRequestDto request)
        {
            try
            {
                var branchIds = SanitizeBranchIds(request.BranchIds);

                var connectionString = _context.Database.GetConnectionString();
                using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();

                var sqlFilterExp = string.Empty;
                if (branchIds != "-1")
                {
                    sqlFilterExp = $" And v.UsmOfficeId in ({branchIds})";
                }

                var parameters = new DynamicParameters();
                parameters.Add("@SqlFilterExp", sqlFilterExp, DbType.String, size: -1);

                var loanTypeWise = (await connection.QueryAsync<LoanPortfolioLoanTypeWiseDto>(
                    "sp_7_16_LoanProtfolioLoanTypeWise",
                    parameters,
                    commandType: CommandType.StoredProcedure,
                    commandTimeout: 120
                )).AsList();

                // ── Particulars: SP returns one row of counts; pivot into rows ──
                var particularsRaw = await connection.QueryFirstOrDefaultAsync<LoanPortfolioParticularsDto>(
                    "sp_7_16_LoanProtfolioParticulars",
                    parameters,
                    commandType: CommandType.StoredProcedure,
                    commandTimeout: 120
                );

                var particulars = particularsRaw == null
                    ? new List<LoanPortfolioParticularsDto>()
                    : new List<LoanPortfolioParticularsDto>
                    {
                new() { Particulars = "Total Member Registration", NoofLoan = particularsRaw.TotalMemberRegistration },
                new() { Particulars = "Loan Disburse (Account No Wise)", NoofLoan = particularsRaw.LoanDisburseAccountNoWise },
                new() { Particulars = "Loanee (Member Wise)", NoofLoan = particularsRaw.LoaneeMemberWise },
                new() { Particulars = "Male Loanee", NoofLoan = particularsRaw.MaleLoanee },
                new() { Particulars = "Women Loanee", NoofLoan = particularsRaw.WomenLoanee },
                new() { Particulars = "Others Loanee", NoofLoan = particularsRaw.OthersLoanee },
                    };

                // ── Outstanding: same pivot pattern ──
                var outstandingRaw = await connection.QueryFirstOrDefaultAsync<LoanPortfolioOutstandingDto>(
                    "sp_7_16_LoanProtfolioOutstanding",
                    parameters,
                    commandType: CommandType.StoredProcedure,
                    commandTimeout: 120
                );

                var outstanding = outstandingRaw == null
                    ? new List<LoanPortfolioOutstandingDto>()
                    : new List<LoanPortfolioOutstandingDto>
                    {
                new() { Particulars = "Male Loanee", NoofLoan = outstandingRaw.MaleLoanee },
                new() { Particulars = "Women Loanee", NoofLoan = outstandingRaw.WomenLoanee },
                new() { Particulars = "Others Loanee", NoofLoan = outstandingRaw.OthersLoanee },
                new() { Particulars = "Outstanding", NoofLoan = outstandingRaw.Outstanding },
                    };

                string branchName = "All";
                if (branchIds != "-1")
                {
                    var names = await connection.QueryAsync<string>(
                        $"SELECT OfficeName FROM UsmOffice WHERE UsmOfficeId IN ({branchIds})");
                    var nameList = names.Where(n => !string.IsNullOrWhiteSpace(n)).ToList();
                    branchName = nameList.Count > 0 ? string.Join(", ", nameList) : "All";
                }

                return new LoanPortfolioData
                {
                    Particulars = particulars,
                    Outstanding = outstanding,
                    LoanTypeWise = loanTypeWise,
                    TotalRecords = loanTypeWise.Count,
                    TotalLoans = loanTypeWise.Sum(r => r.NoofLoan ?? 0),
                    TotalLoanIssueAmount = loanTypeWise.Sum(r => r.LoanIssueAmount ?? 0),
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
