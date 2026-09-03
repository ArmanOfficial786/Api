// Interfaces/ServiceInterface/MemberAccount/SavingsAccountMaturityReport/ISavingsAccountMaturityRepository.cs
using NexgenCosysReport.Dtos.RequestDtos.MemberAccount.InterestPayableReport;

namespace NexgenCosysReport.Inteface.ServiceInterface.MemberAccount.InterestPayableReport
{
    public interface ISavingsAccountMaturityRepository
    {
        Task<SavingsAccountMaturityData> GetReportDataAsync(SavingsAccountMaturityRequestDto request);
    }
}