using NexgenCosysReport.Dtos.RequestDtos.MemberAccount;

namespace NexgenCosysReport.Inteface.ServiceInterface.MemberAccount
{
    public interface ISavingTypeWiseBalance
    {
        Task<SavingTypeWiseBalanceData> GetSavingTypeWiseBalance(SavingTypeWiseBalanceRequest request);
    }
}
