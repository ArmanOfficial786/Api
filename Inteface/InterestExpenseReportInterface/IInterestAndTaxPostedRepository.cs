// Interfaces/ServiceInterface/MemberAccount/InterestExpenseReport/IInterestAndTaxPostedRepository.cs
using NexgenCosysReport.Dtos.RequestDtos.MemberAccount.InterestExpenseReport;

namespace NexgenCosysReport.Interfaces.ServiceInterface.MemberAccount.InterestExpenseReport
{
    public interface IInterestAndTaxPostedRepository
    {
        Task<InterestAndTaxPostedData> GetReportDataAsync(InterestAndTaxPostedRequestDto request);
    }
}