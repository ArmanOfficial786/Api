using NexgenCosysReport.Dtos.RequestDtos.Share;

namespace NexgenCosysReport.Inteface.ServiceInterface.Share
{
    public interface IShareDividend
    {
        Task<ShareDividendData> GetReportDataAsync(ShareDividendRequestDto request);
    }
}
