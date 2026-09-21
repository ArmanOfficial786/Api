using NexgenCosysReport.Dtos.RequestDtos.Share;

namespace NexgenCosysReport.Inteface.ServiceInterface.Share
{
    public interface IShareReturn
    {
        Task<ShareReturnData> GetReportDataAsync(ShareReturnRequestDto request);
    }
}
