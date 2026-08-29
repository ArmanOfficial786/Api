// Inteface/ServiceInterface/MemberAccount/InterestExpenseReport/IFixedDepositInterestTransferRepository.cs
using NexgenCosysReport.Dtos.RequestDtos.MemberAccount.InterestExpenseReport;

namespace NexgenCosysReport.Inteface.ServiceInterface.MemberAccount.InterestExpenseReport
{
    public interface IFixedDepositInterestTransferRepository
    {
        Task<FixedDepositInterestTransferData> GetReportDataAsync(FixedDepositInterestTransferRequestDto request);
    }
}