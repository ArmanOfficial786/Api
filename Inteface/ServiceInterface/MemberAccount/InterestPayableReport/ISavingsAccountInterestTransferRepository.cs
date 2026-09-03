// Interfaces/ServiceInterface/MemberAccount/SavingsAccountInterestTransfer/ISavingsAccountInterestTransferRepository.cs
using NexgenCosysReport.Dtos.RequestDtos.MemberAccount.InterestPayableReport;

namespace NexgenCosysReport.Inteface.ServiceInterface.MemberAccount.InterestPayableReport
{
    public interface ISavingsAccountInterestTransferRepository
    {
        Task<SavingsAccountInterestTransferData> GetReportDataAsync(SavingsAccountInterestTransferRequestDto request);
    }
}