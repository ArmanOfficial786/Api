using NexgenCosysReport.Dtos.RequestDtos.Share;

namespace NexgenCosysReport.Inteface.ServiceInterface.Share
{
    public interface ICopomis
    {
        Task<CopomisData> GetReportDataAsync(CopomisRequestDto request);
    }
}
