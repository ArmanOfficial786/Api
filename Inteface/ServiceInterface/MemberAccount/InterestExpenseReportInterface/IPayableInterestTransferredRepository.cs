// Interfaces/ServiceInterface/MemberAccount/InterestExpenseReport/IPayableInterestTransferredRepository.cs
using NexgenCosysReport.Dtos.RequestDtos.MemberAccount.InterestExpenseReport;

namespace NexgenCosysReport.Inteface.ServiceInterface.MemberAccount.InterestExpenseReportInterface
{
    public interface IPayableInterestTransferredRepository
    {
        Task<PayableInterestTransferredData> GetReportDataAsync(PayableInterestTransferredRequestDto request);
    }
}