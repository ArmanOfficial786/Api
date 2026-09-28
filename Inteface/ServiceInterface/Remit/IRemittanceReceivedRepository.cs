using NexgenCosysReport.Dtos.RequestDtos.Remit;

namespace NexgenCosysReport.Inteface.ServiceInterface.Remit
{
    public interface IRemittanceReceivedRepository
    {
        Task<RemittanceReceivedData> GetReportDataAsync(RemittanceReceivedRequestDto request);
    }
}
