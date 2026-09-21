using NexgenCosysReport.Dtos.RequestDtos.Share;

namespace NexgenCosysReport.Inteface.ServiceInterface.Share
{
    public interface IShareDetails
    {
        Task<ShareDetailsData> GetReportDataAsync(ShareDetailsRequestDto request);
    }
}
