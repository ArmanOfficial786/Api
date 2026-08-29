// Inteface/ServiceInterface/MemberAccount/CollectorDetailReport/ICollectorWiseCommissionRepository.cs
using NexgenCosysReport.Dtos.RequestDtos.MemberAccount.CollectorDetailReport;

namespace NexgenCosysReport.Inteface.ServiceInterface.MemberAccount.CollectorDetailReport
{
    public interface ICollectorWiseCommissionRepository
    {
        Task<CollectorWiseCommissionData> GetReportDataAsync(CollectorWiseCommissionRequestDto request);
    }
}