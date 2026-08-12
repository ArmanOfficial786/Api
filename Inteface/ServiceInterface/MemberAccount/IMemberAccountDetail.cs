using NexgenCosysReport.Dtos.RequestDtos.MemberAccount;

namespace NexgenCosysReport.Inteface.ServiceInterface.MemberAccount
{
    public interface IMemberAccountDetail
    {
        Task<MemberAccountDetailData> GetMemberAccountDetailReport(
            MemberAccountDetailRequest request,
            CancellationToken ct = default);
    }
}
