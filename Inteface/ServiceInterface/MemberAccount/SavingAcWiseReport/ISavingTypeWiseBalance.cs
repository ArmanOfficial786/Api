using NexgenCosysReport.Dtos.RequestDtos.MemberAccount.SavingAcWiseReport;

namespace NexgenCosysReport.Inteface.ServiceInterface.MemberAccount.SavingAcWiseReport
{
    public interface ISavingTypeWiseBalance
    {
        Task<SavingTypeWiseBalanceData> GetSavingTypeWiseBalance(SavingTypeWiseBalanceRequest request);
    }
}
