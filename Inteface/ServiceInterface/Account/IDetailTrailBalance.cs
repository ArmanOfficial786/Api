using NexgenCosysReport.Dtos.RequestDtos.Account;

namespace NexgenCosysReport.Inteface.ServiceInterface.Account
{
    public interface IDetailTrailBalance
    {
        Task<DetailTrialBalanceData> GetDetailTrialBalance(DetailTrialBalanceRequest request);
    }
}
