// Inteface/ServiceInterface/MemberAccount/CollectorDetailReport/ICollectorWiseAccountCloseRepository.cs
using NexgenCosysReport.Dtos.RequestDtos.MemberAccount.CollectorDetailReport;

namespace NexgenCosysReport.Inteface.ServiceInterface.MemberAccount.CollectorDetRepInterface
{
    public interface ICollectorWiseAccountCloseRepository
    {
        Task<CollectorWiseAccountCloseData> GetReportDataAsync(CollectorWiseAccountCloseRequestDto request);
    }
}