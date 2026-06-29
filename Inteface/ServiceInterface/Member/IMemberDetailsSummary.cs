using NexgenCosysReport.Dtos.RequestDtos.Member;

namespace NexgenCosysReport.Inteface.ServiceInterface.Member
{
    public interface IMemberDetailsSummary
    {
        Task<MemberDetailsSummarySpResponse> GetMemberDetailsSummary(MemberDetailsSummaryRequest request);
    }
}
