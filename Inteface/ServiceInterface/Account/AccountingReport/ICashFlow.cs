using NexgenCosysReport.Dtos.RequestDtos.Account.AccountingReport;

namespace NexgenCosysReport.Inteface.ServiceInterface.Account.AccountingReport
{
    public interface ICashFlow
    {
        Task<CashFlowData> GetCashFlow(CashFlowRequest request);
    }
}
