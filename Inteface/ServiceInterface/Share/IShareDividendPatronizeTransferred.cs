using NexgenCosysReport.Dtos.RequestDtos.Share;

namespace NexgenCosysReport.Inteface.ServiceInterface.Share
{
    public interface IShareDividendPatronizeTransferred
    {
        Task<ShareDividendPatronizeTransferredData> GetReportDataAsync(ShareDividendPatronizeTransferredRequestDto request);
    }
}
