using NexgenCosysReport.Dtos.RequestDtos;

namespace NexgenCosysReport.Inteface.ServiceInterface
{
    public interface IMemberIdCard
    {
         Task<List<MemberIdCardModel>> GetMemberIdCardData(MemberIdCardRequest request);
    }
}
