using NexgenCosysReport.Dtos.RequestDtos.MemberAccount.SavingAcWiseReport;

namespace NexgenCosysReport.Inteface.ServiceInterface.MemberAccount.SavingAcWiseReport
{
    public interface IDepositUnverified
    {
        Task<DepositUnverifiedData> GetDepositUnverified(DepositUnverifiedRequest request);
    }
}
