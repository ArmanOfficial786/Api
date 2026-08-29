// Inteface/ServiceInterface/MemberAccount/InterestExpenseReport/IInterestAndTaxTypeWiseRepository.cs
using NexgenCosysReport.Dtos.RequestDtos.MemberAccount.InterestExpenseReport;

namespace NexgenCosysReport.Inteface.ServiceInterface.MemberAccount.InterestExpenseReport
{
    public interface IInterestAndTaxTypeWiseRepository
    {
        Task<InterestAndTaxTypeWiseData> GetReportDataAsync(InterestAndTaxTypeWiseRequestDto request);
    }
}