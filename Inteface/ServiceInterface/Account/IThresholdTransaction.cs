using NexgenCosysReport.Dtos.RequestDtos.Account;

namespace NexgenCosysReport.Inteface.ServiceInterface.Account
{
    public interface IThresholdTransaction
    {
        Task<ThresholdTransactionData> GetThresholdTransaction(ThresholdTransactionRequest request);
    }
}
