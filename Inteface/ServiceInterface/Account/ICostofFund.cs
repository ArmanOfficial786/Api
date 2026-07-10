using NexgenCosysReport.Dtos.RequestDtos.Account;

namespace NexgenCosysReport.Inteface.ServiceInterface.Account
{
    public interface ICostofFund
    {
        Task<CostOfFundData> GetCostOfFund(CostOfFundRequest request);
    }
}
