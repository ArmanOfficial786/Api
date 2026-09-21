using NexgenCosysReport.Dtos.RequestDtos.Share;

namespace NexgenCosysReport.Inteface.ServiceInterface.Share
{
    public interface IShareHolding
    {
        Task<ShareHoldingData> GetReportDataAsync(ShareHoldingRequestDto request);
    }
}
