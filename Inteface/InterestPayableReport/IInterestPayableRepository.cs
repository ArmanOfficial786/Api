// Interfaces/ServiceInterface/MemberAccount/InterestPayableReport/IInterestPayableRepository.cs
using NexgenCosysReport.Dtos.RequestDtos.MemberAccount.InterestPayableReport;

namespace NexgenCosysReport.Interfaces.ServiceInterface.MemberAccount.InterestPayableReport
{
    public interface IInterestPayableRepository
    {
        Task<InterestPayableData> GetReportDataAsync(InterestPayableRequestDto request);
    }
}