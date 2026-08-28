
using NexgenCosysReport.Dtos.RequestDtos.MemberAccount.OthersReport;

namespace NexgenCosysReport.Inteface.ServiceInterface.MemberAccount.OthersReport
{
    public interface ISavingDepositDateWiseRepository
    {
        Task<SavingDepositDateWiseData> GetReportDataAsync(SavingDepositDateWiseRequestDto request);
    }
}