using JsSampleReport.Dtos.RequestDtos;

namespace JsSampleReport.Inteface.ServiceInterface
{
    public interface IAccountStatement
    {
        Task<List<AccountStatementModelResponse>>
            GetAccountStatementTypeAsync(AccountStatementRequest request);

        Task<List<CashBankBalanceModelResponse>>
            GetCashAndBankBalanceOpeningClosingAsync(AccountStatementRequest request);
    }
}