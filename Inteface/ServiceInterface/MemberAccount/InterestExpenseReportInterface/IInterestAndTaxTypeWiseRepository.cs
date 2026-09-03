// Inteface/ServiceInterface/MemberAccount/InterestExpenseReport/IInterestAndTaxTypeWiseRepository.cs
using NexgenCosysReport.Dtos.RequestDtos.MemberAccount.InterestExpenseReport;

namespace NexgenCosysReport.Inteface.ServiceInterface.MemberAccount.InterestExpenseReportInterface
{
    public interface IInterestAndTaxTypeWiseRepository
    {
        Task<InterestAndTaxTypeWiseData> GetReportDataAsync(InterestAndTaxTypeWiseRequestDto request);
    }
}