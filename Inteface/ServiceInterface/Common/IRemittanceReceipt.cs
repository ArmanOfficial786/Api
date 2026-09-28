using NexgenCosysReport.Dtos.RequestDtos.Common;

namespace NexgenCosysReport.Inteface.ServiceInterface.Common
{
    public interface IRemittanceReceipt
    {
        Task<List<RemittanceReceiptResponse>> GetRemittanceReceipts(RemittanceReceiptRequest request);
    }
}
