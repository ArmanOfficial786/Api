using JsSampleReport.Dtos.RequestDtos;

namespace JsSampleReport.Inteface.ServiceInterface
{
    public interface IAccountStatement
    {
        Task<List<AccountStatementResponseModel>>
            GetAccountStatementTypeAsync(AccountStatementRequest request);

        Task<List<CashBankBalanceModel>>
            GetCashAndBankBalanceOpeningClosingAsync(AccountStatementRequest request);
    }
}