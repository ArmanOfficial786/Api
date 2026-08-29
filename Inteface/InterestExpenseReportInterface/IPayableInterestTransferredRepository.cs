// Interfaces/ServiceInterface/MemberAccount/InterestExpenseReport/IPayableInterestTransferredRepository.cs
using NexgenCosysReport.Dtos.RequestDtos.MemberAccount.InterestExpenseReport;

namespace NexgenCosysReport.Interfaces.ServiceInterface.MemberAccount.InterestExpenseReport
{
    public interface IPayableInterestTransferredRepository
    {
        Task<PayableInterestTransferredData> GetReportDataAsync(PayableInterestTransferredRequestDto request);
    }
}