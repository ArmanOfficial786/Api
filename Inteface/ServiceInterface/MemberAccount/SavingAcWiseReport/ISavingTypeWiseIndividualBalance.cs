using NexgenCosysReport.Dtos.RequestDtos.MemberAccount.SavingAcWiseReport;

namespace NexgenCosysReport.Inteface.ServiceInterface.MemberAccount.SavingAcWiseReport
{
    public interface ISavingTypeWiseIndividualBalance
    {
        Task<SavingTypeWiseIndividualBalanceData> GetSavingTypeWiseIndividualBalance(SavingTypeWiseIndividualBalanceRequest request);
    }
}
