using NexgenCosysReport.Dtos.RequestDtos.Asset;

namespace NexgenCosysReport.Inteface.ServiceInterface.Asset
{
    public interface IFamPurchaseReportRepository
    {
        Task<FamPurchaseReportData> GetReportDataAsync(FamPurchaseReportRequestDto request);
    }
}
