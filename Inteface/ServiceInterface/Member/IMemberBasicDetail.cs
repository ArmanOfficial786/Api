using NexgenCosysReport.Dtos.RequestDtos.Member;

namespace NexgenCosysReport.Inteface.ServiceInterface.Member
{
    public interface IMemberBasicDetail
    {
        Task<List<MemberBasicDetailsSpDto>> GetMemberBasicDetails(MemberBasicDetailsRequest request);
    }
}
