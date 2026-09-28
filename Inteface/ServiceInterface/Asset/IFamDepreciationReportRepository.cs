using NexgenCosysReport.Dtos.RequestDtos.Asset;

namespace NexgenCosysReport.Inteface.ServiceInterface.Asset
{
    public interface IFamDepreciationReportRepository
    {
        Task<FamDepreciationReportData> GetReportDataAsync(FamDepreciationReportRequestDto request);
    }
}
