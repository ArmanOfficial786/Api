using JsSampleReport.Dtos.RequestDtos;

namespace JsSampleReport.Inteface.ServiceInterface
{
    public interface IAccountStatement
    {
        Task<List<AccountStatementModel>>
            GetAccountStatementTypeAsync(AccountStatementRequest request);

        Task<List<CashBankBalanceModel>>
            GetCashAndBankBalanceOpeningClosingAsync(AccountStatementRequest request);
    }
}