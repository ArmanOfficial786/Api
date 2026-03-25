using JsSampleReport.Dtos.ReportDtos;
using JsSampleReport.Dtos.RequestDtos;

namespace JsSampleReport.Inteface.ServiceInterface
{
    public interface IMemberDetail
    {
        Task<List<MemberRegistrationDetail>> GetMemberRegistrationDetail(MemberDetailRequest request);
        Task<List<CommonHeader>> GetCommonHeaders();
    }
}
