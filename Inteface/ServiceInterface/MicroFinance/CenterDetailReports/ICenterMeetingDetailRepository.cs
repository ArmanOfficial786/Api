// Inteface/ServiceInterface/Microfinance/CenterDetailReports/ICenterMeetingDetailRepository.cs
using NexgenCosysReport.Dtos.RequestDtos.Microfinance.CenterDetailReports;

namespace NexgenCosysReport.Inteface.ServiceInterface.Microfinance.CenterDetailReports
{
    public interface ICenterMeetingDetailRepository
    {
        Task<CenterMeetingDetailData> GetReportDataAsync(CenterMeetingDetailRequestDto request);
    }
}