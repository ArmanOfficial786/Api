using NexgenCosysReport.Dtos.RequestDtos.MemberAccount;

namespace NexgenCosysReport.Inteface.ServiceInterface.MemberAccount
{
    public interface IMemberPenaltyDepositWithdraw
    {
        Task<MemberPenaltyDepositWithdrawData> GetMemberPenaltyDepositWithdrawReport(MemberPenaltyDepositWithdrawRequest request);
    }
}
