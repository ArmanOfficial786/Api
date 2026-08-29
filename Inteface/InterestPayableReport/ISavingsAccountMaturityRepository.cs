// Interfaces/ServiceInterface/MemberAccount/SavingsAccountMaturityReport/ISavingsAccountMaturityRepository.cs
using NexgenCosysReport.Dtos.RequestDtos.MemberAccount.InterestPayableReport;

namespace NexgenCosysReport.Interfaces.ServiceInterface.MemberAccount.SavingsAccountMaturityReport
{
    public interface ISavingsAccountMaturityRepository
    {
        Task<SavingsAccountMaturityData> GetReportDataAsync(SavingsAccountMaturityRequestDto request);
    }
}