using JsSampleReport.Dtos.ReportDtos;
using JsSampleReport.Dtos.RequestDtos;

namespace JsSampleReport.Inteface.ServiceInterface
{
    public interface IMemberDetail
    {
        List<MemberRegistrationDetail> GetMemberRegistrationDetail(MemberDetailRequest request);
        List<CommonHeader> GetCommonHeaders();
    }
}
