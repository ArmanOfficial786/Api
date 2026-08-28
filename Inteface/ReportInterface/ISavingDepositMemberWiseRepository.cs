
using NexgenCosysReport.Dtos.RequestDtos.MemberAccount.OthersReport;

namespace NexgenCosysReport.Inteface.ServiceInterface.MemberAccount.OthersReport
{
    public interface ISavingDepositMemberWiseRepository
    {
        Task<SavingDepositMemberWiseData> GetReportDataAsync(SavingDepositMemberWiseRequestDto request);
    }
}