using NexgenCosysReport.Dtos.RequestDtos.Member;

namespace NexgenCosysReport.Inteface.ServiceInterface.Member
{
    public interface IMemberIdCard
    {
         Task<List<MemberIdCardModel>> GetMemberIdCardData(MemberIdCardRequest request);
    }
}
