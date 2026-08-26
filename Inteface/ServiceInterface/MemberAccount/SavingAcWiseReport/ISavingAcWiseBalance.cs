using NexgenCosysReport.Dtos.RequestDtos.MemberAccount.SavingAcWiseReport;

namespace NexgenCosysReport.Inteface.ServiceInterface.MemberAccount.SavingAcWiseReport
{
    public interface ISavingAcWiseBalance
    {
        Task<List<SavingAcWiseBalanceResponse>> GetSavingAcWiseBalanceAsync(
    SavingAcWiseBalanceRequest request);

    }
}
