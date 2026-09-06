// Inteface/ServiceInterface/AccountOperation/OthersReport/ITellerCashDetailRepository.cs
using NexgenCosysReport.Dtos.RequestDtos.AccountOperation.OthersReport;

namespace NexgenCosysReport.Inteface.ServiceInterface.AccountOperation.OthersReport
{
    public interface ITellerCashDetailRepository
    {
        Task<TellerCashDetailData> GetReportDataAsync(TellerCashDetailRequestDto request);
    }
}