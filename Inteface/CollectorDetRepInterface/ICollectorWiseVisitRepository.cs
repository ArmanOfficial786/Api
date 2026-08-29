// Inteface/ServiceInterface/MemberAccount/CollectorDetailReport/ICollectorWiseVisitRepository.cs
using NexgenCosysReport.Dtos.RequestDtos.MemberAccount.CollectorDetailReport;

namespace NexgenCosysReport.Inteface.ServiceInterface.MemberAccount.CollectorDetailReport
{
    public interface ICollectorWiseVisitRepository
    {
        Task<CollectorWiseVisitData> GetReportDataAsync(CollectorWiseVisitRequestDto request);
    }
}