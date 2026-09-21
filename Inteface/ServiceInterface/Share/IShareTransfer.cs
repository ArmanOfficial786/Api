using NexgenCosysReport.Dtos.RequestDtos.Share;

namespace NexgenCosysReport.Inteface.ServiceInterface.Share
{
    public interface IShareTransfer
    {
        Task<ShareTransferData> GetReportDataAsync(ShareTransferRequestDto request);
    }
}
