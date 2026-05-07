using NexgenCosysReport.Dtos.ReportDtos;
using NexgenCosysReport.Dtos.RequestDtos;

namespace NexgenCosysReport.Inteface.ServiceInterface
{
    public interface IMemberDetail
    {
        Task<List<MemberRegistrationDetail>> GetMemberRegistrationDetail(MemberDetailRequest request);
        Task<List<CommonHeader>> GetCommonHeaders();
    }
}
