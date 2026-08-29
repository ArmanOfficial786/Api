// Interfaces/ServiceInterface/MemberAccount/SavingsAccountInterestTransfer/ISavingsAccountInterestTransferRepository.cs
using NexgenCosysReport.Dtos.RequestDtos.MemberAccount.InterestPayableReport;

namespace NexgenCosysReport.Interfaces.ServiceInterface.MemberAccount.SavingsAccountInterestTransfer
{
    public interface ISavingsAccountInterestTransferRepository
    {
        Task<SavingsAccountInterestTransferData> GetReportDataAsync(SavingsAccountInterestTransferRequestDto request);
    }
}