using NexgenCosysReport.Dtos.RequestDtos.MemberAccount;

namespace NexgenCosysReport.Inteface.ServiceInterface.MemberAccount
{
    public interface IDepositUnverified
    {
        Task<DepositUnverifiedData> GetDepositUnverified(DepositUnverifiedRequest request);
    }
}
