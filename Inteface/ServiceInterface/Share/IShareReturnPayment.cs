using NexgenCosysReport.Dtos.RequestDtos.Share;

namespace NexgenCosysReport.Inteface.ServiceInterface.Share
{
    public interface IShareReturnPayment
    {
        Task<ShareReturnPaymentData> GetReportDataAsync(ShareReturnPaymentRequestDto request);
    }
}
