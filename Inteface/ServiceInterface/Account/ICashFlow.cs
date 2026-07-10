using NexgenCosysReport.Dtos.RequestDtos.Account;

namespace NexgenCosysReport.Inteface.ServiceInterface.Account
{
    public interface ICashFlow
    {
        Task<CashFlowData> GetCashFlow(CashFlowRequest request);
    }
}
