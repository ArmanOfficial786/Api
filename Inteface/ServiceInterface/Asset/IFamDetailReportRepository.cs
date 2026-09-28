using NexgenCosysReport.Dtos.RequestDtos.Asset;

namespace NexgenCosysReport.Inteface.ServiceInterface.Asset
{
    public interface IFamDetailReportRepository
    {
        Task<FamDetailReportData> GetReportDataAsync(FamDetailReportRequestDto request);
    }
}
