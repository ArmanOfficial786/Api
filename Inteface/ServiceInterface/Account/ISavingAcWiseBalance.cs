using NexgenCosysReport.Dtos.RequestDtos.Account;

namespace NexgenCosysReport.Inteface.ServiceInterface.Account
{
    public interface ISavingAcWiseBalance
    {
        Task<List<SavingAcWiseBalanceResponse>> GetSavingAcWiseBalanceAsync(
    SavingAcWiseBalanceRequest request);

    }
}
