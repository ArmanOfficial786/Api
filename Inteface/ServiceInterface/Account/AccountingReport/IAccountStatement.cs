using NexgenCosysReport.Dtos.RequestDtos.Account.AccountingReport;

namespace NexgenCosysReport.Inteface.ServiceInterface.Account.AccountingReport
{
    public interface IAccountStatement
    {
        Task<List<AccountStatementModelResponse>>
            GetAccountStatementTypeAsync(AccountStatementRequest request);

        Task<List<CashBankBalanceModelResponse>>
            GetCashAndBankBalanceOpeningClosingAsync(AccountStatementRequest request);
    }
}