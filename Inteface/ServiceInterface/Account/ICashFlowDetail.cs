using NexgenCosysReport.Dtos.RequestDtos.Account;

namespace NexgenCosysReport.Inteface.ServiceInterface.Account
{
    public interface ICashFlowDetail
    {
        Task<CashFlowDetailsData> GetCashFlowDetails(CashFlowDetailsRequest request);
    }
}
