using JsSampleProject.Dtos.MemberDtos;
using JsSampleProject.Dtos.ReportDtos;

namespace JsSampleProject.Interface
{
    public interface IMemberDetail
    {
        List<MemberRegistrationDetail> GetMemberRegistrationDetail(MemberDetailRequest request);
        List<CommonHeader> GetCommonHeaders();
    }
}
