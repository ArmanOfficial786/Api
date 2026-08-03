using NexgenCosysReport.Dtos.RequestDtos.MemberAccount;

namespace NexgenCosysReport.Inteface.ServiceInterface.MemberAccount
{
    public interface ISavingTypeWiseIndividualBalance
    {
        Task<SavingTypeWiseIndividualBalanceData> GetSavingTypeWiseIndividualBalance(SavingTypeWiseIndividualBalanceRequest request);
    }
}
