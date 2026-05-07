using NexgenCosysReport.Dtos.RequestDtos;

namespace NexgenCosysReport.Inteface.ServiceInterface
{
    public interface IAccountStatement
    {
        Task<List<AccountStatementModelResponse>>
            GetAccountStatementTypeAsync(AccountStatementRequest request);

        Task<List<CashBankBalanceModelResponse>>
            GetCashAndBankBalanceOpeningClosingAsync(AccountStatementRequest request);
    }
}