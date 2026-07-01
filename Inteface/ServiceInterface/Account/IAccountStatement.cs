using NexgenCosysReport.Dtos.RequestDtos.Account;

namespace NexgenCosysReport.Inteface.ServiceInterface.Account
{
    public interface IAccountStatement
    {
        Task<List<AccountStatementModelResponse>>
            GetAccountStatementTypeAsync(AccountStatementRequest request);

        Task<List<CashBankBalanceModelResponse>>
            GetCashAndBankBalanceOpeningClosingAsync(AccountStatementRequest request);
    }
}