using NexgenCosysReport.Dtos.RequestDtos.Remit;

namespace NexgenCosysReport.Inteface.ServiceInterface.Remit
{
    public interface IRemitOnlinePaymentDetailRepository
    {
        Task<RemitOnlinePaymentDetailData> GetReportDataAsync(RemitOnlinePaymentDetailRequestDto request);
    }
}
