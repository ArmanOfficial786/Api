using NexgenCosysReport.Dtos.RequestDtos.Share;

namespace NexgenCosysReport.Inteface.ServiceInterface.Share
{
    public interface IShareStatement
    {
        Task<ShareStatementData> GetReportDataAsync(ShareStatementRequestDto request);
    }
}
