// Inteface/ServiceInterface/MemberAccount/CollectorDetailReport/ICollectorWiseCommissionSummaryRepository.cs
using NexgenCosysReport.Dtos.RequestDtos.MemberAccount.CollectorDetailReport;

namespace NexgenCosysReport.Inteface.ServiceInterface.MemberAccount.CollectorDetailReport
{
    public interface ICollectorWiseCommissionSummaryRepository
    {
        Task<CollectorWiseCommissionSummaryData> GetReportDataAsync(CollectorWiseCommissionSummaryRequestDto request);
    }
}