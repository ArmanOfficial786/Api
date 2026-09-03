using NexgenCosysReport.Dtos.RequestDtos.MemberAccount.InterestExpenseReport;

namespace NexgenCosysReport.Inteface.ServiceInterface.MemberAccount.InterestExpenseReportInterface
{
    public interface IInterestAndTaxDetailRepository
    {
        Task<InterestAndTaxDetailData> GetReportDataAsync(InterestAndTaxDetailRequestDto request);
    }
}