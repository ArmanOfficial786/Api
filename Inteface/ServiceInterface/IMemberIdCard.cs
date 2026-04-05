using JsSampleReport.Dtos.RequestDtos;

namespace JsSampleReport.Inteface.ServiceInterface
{
    public interface IMemberIdCard
    {
         Task<List<MemberIdCardModel>> GetMemberIdCardData(MemberIdCardRequest request);
    }
}
