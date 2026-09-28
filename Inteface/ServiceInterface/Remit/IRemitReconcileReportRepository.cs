using NexgenCosysReport.Dtos.RequestDtos.Remit;

namespace NexgenCosysReport.Inteface.ServiceInterface.Remit
{
    public interface IRemitReconcileReportRepository
    {
        Task<RemitReconcileReportData> GetReportDataAsync(RemitReconcileReportRequestDto request);
    }
}
