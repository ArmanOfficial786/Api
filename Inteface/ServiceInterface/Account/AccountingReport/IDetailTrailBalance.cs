using NexgenCosysReport.Dtos.RequestDtos.Account.AccountingReport;

namespace NexgenCosysReport.Inteface.ServiceInterface.Account.AccountingReport
{
    public interface IDetailTrailBalance
    {
        Task<DetailTrialBalanceData> GetDetailTrialBalance(DetailTrialBalanceRequest request);
    }
}
