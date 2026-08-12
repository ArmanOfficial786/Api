using NexgenCosysReport.Dtos.RequestDtos.MemberAccount;

namespace NexgenCosysReport.Inteface.ServiceInterface.MemberAccount
{
    public interface IMemberAccDetailList
    {
        Task<MemberAccountDetailNoData> GetMemberAccountDetailNo(MemberAccountDetailNoRequest request);
    }
}
