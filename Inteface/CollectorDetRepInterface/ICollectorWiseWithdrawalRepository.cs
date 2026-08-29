// Inteface/ServiceInterface/MemberAccount/CollectorDetailReport/ICollectorWiseWithdrawalRepository.cs
using NexgenCosysReport.Dtos.RequestDtos.MemberAccount.CollectorDetailReport;

namespace NexgenCosysReport.Inteface.ServiceInterface.MemberAccount.CollectorDetailReport
{
    public interface ICollectorWiseWithdrawalRepository
    {
        Task<CollectorWiseWithdrawalData> GetReportDataAsync(CollectorWiseWithdrawalRequestDto request);
    }
}