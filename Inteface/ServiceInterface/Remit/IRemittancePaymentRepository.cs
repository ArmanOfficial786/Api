using NexgenCosysReport.Dtos.RequestDtos.Remit;

namespace NexgenCosysReport.Inteface.ServiceInterface.Remit
{
    public interface IRemittancePaymentRepository
    {
        Task<RemittancePaymentData> GetReportDataAsync(RemittancePaymentRequestDto request);
    }
}
