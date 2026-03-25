using JsSampleReport.Dtos.RequestDtos;

namespace JsSampleReport.Inteface.ServiceInterface
{
    public interface IMemberIdCard
    {
         Task<List<MemberIdCardResponseModel>> GetMemberIdCardData(MemberIdCardRequest request);
    }
}
