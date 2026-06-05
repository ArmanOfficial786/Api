using NexgenCosysReport.Dtos.ReportDtos;
using NexgenCosysReport.Dtos.RequestDtos.Member;

namespace NexgenCosysReport.Inteface.ServiceInterface.Member
{
    public interface IMemberDetail
    {
        Task<List<MemberRegistrationDetail>> GetMemberRegistrationDetail(MemberDetailRequest request);
        //Task<List<CommonHeader>> GetCommonHeaders();
    }
}
