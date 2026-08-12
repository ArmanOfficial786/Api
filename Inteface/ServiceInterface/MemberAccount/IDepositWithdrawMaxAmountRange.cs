// Inteface/ServiceInterface/AccountOperation/IDepositWithdrawMaxAmountRange.cs
using NexgenCosysReport.Dtos.RequestDtos.MemberAccount;

namespace NexgenCosysReport.Inteface.ServiceInterface.MemberAccount
{
    public interface IDepositWithdrawMaxAmountRange
    {
        Task<DepositWithdrawMaxAmountData> GetDepositWithdrawMaxAmountRange(DepositWithdrawMaxAmountRangeRequest request);
    }
}