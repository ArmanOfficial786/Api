using NexgenCosysReport.Dtos.RequestDtos.Remit;

namespace NexgenCosysReport.Inteface.ServiceInterface.Remit
{
    public interface IRemittancePaymentDetailRepository
    {
        Task<RemittancePaymentDetailData> GetReportDataAsync(RemittancePaymentDetailRequestDto request);
    }
}
