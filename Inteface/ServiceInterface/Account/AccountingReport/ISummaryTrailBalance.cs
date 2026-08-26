using NexgenCosysReport.Dtos.RequestDtos.Account.AccountingReport;

namespace NexgenCosysReport.Inteface.ServiceInterface.Account.AccountingReport
{
    public interface ISummaryTrailBalance
    {
        Task<List<SummaryTrialBalanceRowDto>> GetSummaryTrialBalance(SummaryTrialBalanceRequest request);
    }
}
