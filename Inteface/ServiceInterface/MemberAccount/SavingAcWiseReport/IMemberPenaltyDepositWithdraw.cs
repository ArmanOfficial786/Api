using NexgenCosysReport.Dtos.RequestDtos.MemberAccount.SavingAcWiseReport;

namespace NexgenCosysReport.Inteface.ServiceInterface.MemberAccount.SavingAcWiseReport
{
    public interface IMemberPenaltyDepositWithdraw
    {
        Task<MemberPenaltyDepositWithdrawData> GetMemberPenaltyDepositWithdrawReport(MemberPenaltyDepositWithdrawRequest request);
    }
}
