// Repository/Microfinance/CenterDetailReports/LoanEvaluationRepository.cs
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using NexgenCosysReport.DbContext;
using NexgenCosysReport.Dtos.RequestDtos.Microfinance.CenterDetailReports;
using NexgenCosysReport.Inteface.ServiceInterface.Microfinance.CenterDetailReports;
using System.Data;

namespace NexgenCosysReport.Repository.Microfinance.CenterDetailReports
{
    public class LoanEvaluationRepository : ILoanEvaluationRepository
    {
        private readonly AppDbContext _context;
        private readonly ILogger<LoanEvaluationRepository> _logger;

        public LoanEvaluationRepository(
            AppDbContext context,
            ILogger<LoanEvaluationRepository> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<LoanEvaluationData> GetReportDataAsync(LoanEvaluationReqResponse request)
        {
            try
            {
                if (!request.MemberRegistrationId.HasValue || request.MemberRegistrationId.Value <= 0)
                    throw new ArgumentException("Member is required.");

                var connectionString = _context.Database.GetConnectionString();
                using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();

                var parameters = new DynamicParameters();
                parameters.Add("@MemberRegistrationId", request.MemberRegistrationId.Value, DbType.Int32);

                using var multi = await connection.QueryMultipleAsync(
                    "sp_4_113_GetLoanEvaluationReport",
                    parameters,
                    commandType: CommandType.StoredProcedure,
                    commandTimeout: 600);

                var member = await multi.ReadFirstOrDefaultAsync<LoanEvaluationMemberDto>();

                var loanDetails = (await multi.ReadAsync<LoanEvaluationLoanDetailDto>()).AsList();

                var assetLedger = (await multi.ReadAsync<LoanEvaluationAssetLedgerDto>()).AsList();

                var meeting = await multi.ReadFirstOrDefaultAsync<LoanEvaluationMeetingDto>();

                var incomes = (await multi.ReadAsync<LoanEvaluationIncomeDto>()).AsList();

                var expenses = (await multi.ReadAsync<LoanEvaluationExpenseDto>()).AsList();

                var loanIssues = (await multi.ReadAsync<LoanEvaluationLoanIssueDto>()).AsList();

                return new LoanEvaluationData
                {
                    Member = member,
                    LoanDetails = loanDetails,
                    AssetLedger = assetLedger,
                    Meeting = meeting,
                    Incomes = incomes,
                    Expenses = expenses,
                    LoanIssues = loanIssues,

                    TotalSaving = loanDetails.Sum(x => x.Saving ?? 0),
                    TotalShare = loanDetails.Sum(x => x.Share ?? 0),
                    TotalLoanIssue = loanDetails.Sum(x => x.LoanIssue ?? 0),
                    TotalLoanRemaining = loanDetails.Sum(x => x.LoanRemaining ?? 0),
                    TotalLoanDue = loanDetails.Sum(x => x.LoanDue ?? 0),
                    GrandTotal = loanDetails.Sum(x => x.Total ?? 0),
                    TotalAssetCost = assetLedger.Sum(x => x.Cost ?? 0),
                    TotalIncome = incomes.Sum(x => x.Amount ?? 0),
                    TotalExpense = expenses.Sum(x => x.Amount ?? 0)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetReportDataAsync (LoanEvaluation)");
                throw;
            }
        }
    }
}