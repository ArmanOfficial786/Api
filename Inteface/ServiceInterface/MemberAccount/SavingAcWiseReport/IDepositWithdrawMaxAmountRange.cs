// Inteface/ServiceInterface/AccountOperation/IDepositWithdrawMaxAmountRange.cs
using NexgenCosysReport.Dtos.RequestDtos.MemberAccount.SavingAcWiseReport;

namespace NexgenCosysReport.Inteface.ServiceInterface.MemberAccount.SavingAcWiseReport
{
    public interface IDepositWithdrawMaxAmountRange
    {
        Task<DepositWithdrawMaxAmountData> GetDepositWithdrawMaxAmountRange(DepositWithdrawMaxAmountRangeRequest request);
    }
}