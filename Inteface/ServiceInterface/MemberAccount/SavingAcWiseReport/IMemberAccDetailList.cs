using NexgenCosysReport.Dtos.RequestDtos.MemberAccount.SavingAcWiseReport;

namespace NexgenCosysReport.Inteface.ServiceInterface.MemberAccount.SavingAcWiseReport
{
    public interface IMemberAccDetailList
    {
        Task<MemberAccountDetailNoData> GetMemberAccountDetailNo(MemberAccountDetailNoRequest request);
    }
}
