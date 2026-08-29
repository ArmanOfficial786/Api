// Inteface/ServiceInterface/MemberAccount/CollectorDetailReport/ICollectorWiseAccountCloseRepository.cs
using NexgenCosysReport.Dtos.RequestDtos.MemberAccount.CollectorDetailReport;

namespace NexgenCosysReport.Inteface.ServiceInterface.MemberAccount.CollectorDetailReport
{
    public interface ICollectorWiseAccountCloseRepository
    {
        Task<CollectorWiseAccountCloseData> GetReportDataAsync(CollectorWiseAccountCloseRequestDto request);
    }
}