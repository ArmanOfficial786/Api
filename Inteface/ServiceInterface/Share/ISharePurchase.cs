using NexgenCosysReport.Dtos.RequestDtos.Share;

namespace NexgenCosysReport.Inteface.ServiceInterface.Share
{
    public interface ISharePurchase
    {
        Task<SharePurchaseData> GetReportDataAsync(SharePurchaseRequestDto request);
    }
}
