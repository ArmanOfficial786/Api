using NexgenCosysReport.Dtos.RequestDtos.Remit;

namespace NexgenCosysReport.Inteface.ServiceInterface.Remit
{
    public interface IRemitReceiptPrintRepository
    {
        Task<RemitReceiptPrintData> GetReportDataAsync(RemitReceiptPrintRequestDto request);
    }
}
