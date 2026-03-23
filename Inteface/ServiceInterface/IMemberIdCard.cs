using JsSampleReport.Dtos.RequestDtos;

namespace JsSampleReport.Inteface.ServiceInterface
{
    public interface IMemberIdCard
    {
        public List<MemberIdCardResponseModel> GetMemberIdCardData(MemberIdCardRequest request);
    }
}
