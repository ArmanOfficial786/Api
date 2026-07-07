using NexgenCosysReport.Dtos.RequestDtos.Account;

namespace NexgenCosysReport.Inteface.ServiceInterface.Account
{
    public interface ISummaryTrailBalance
    {
        Task<List<SummaryTrialBalanceRowDto>> GetSummaryTrialBalance(SummaryTrialBalanceRequest request);
    }
}
