using NexgenCosysReport.Dtos.RequestDtos.MemberAccount;

namespace NexgenCosysReport.Inteface.ServiceInterface.MemberAccount
{
    public interface ISavingAcWiseBalance
    {
        Task<List<SavingAcWiseBalanceResponse>> GetSavingAcWiseBalanceAsync(
    SavingAcWiseBalanceRequest request);

    }
}
