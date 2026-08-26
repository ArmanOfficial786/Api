using NexgenCosysReport.Dtos.RequestDtos.Account.AccountingReport;

namespace NexgenCosysReport.Inteface.ServiceInterface.Account.AccountingReport
{
    public interface IThresholdTransaction
    {
        Task<ThresholdTransactionData> GetThresholdTransaction(ThresholdTransactionRequest request);
    }
}
