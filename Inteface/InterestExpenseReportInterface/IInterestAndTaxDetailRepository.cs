// Interfaces/ServiceInterface/MemberAccount/InterestExpenseReport/IInterestAndTaxDetailRepository.cs
using NexgenCosysReport.Dtos.RequestDtos.MemberAccount.InterestExpenseReport;

namespace NexgenCosysReport.Interfaces.ServiceInterface.MemberAccount.InterestExpenseReport
{
    public interface IInterestAndTaxDetailRepository
    {
        Task<InterestAndTaxDetailData> GetReportDataAsync(InterestAndTaxDetailRequestDto request);
    }
}