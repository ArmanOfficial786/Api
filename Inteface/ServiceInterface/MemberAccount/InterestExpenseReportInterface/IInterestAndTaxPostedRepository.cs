// Interfaces/ServiceInterface/MemberAccount/InterestExpenseReport/IInterestAndTaxPostedRepository.cs
using NexgenCosysReport.Dtos.RequestDtos.MemberAccount.InterestExpenseReport;

namespace NexgenCosysReport.Inteface.ServiceInterface.MemberAccount.InterestExpenseReportInterface
{
    public interface IInterestAndTaxPostedRepository
    {
        Task<InterestAndTaxPostedData> GetReportDataAsync(InterestAndTaxPostedRequestDto request);
    }
}