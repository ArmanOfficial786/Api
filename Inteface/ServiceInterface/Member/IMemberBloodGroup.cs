using NexgenCosysReport.Dtos.RequestDtos.Member;

namespace NexgenCosysReport.Inteface.ServiceInterface.Member
{
    public interface IMemberBloodGroup
    {
        Task<List<MemberBloodGroupSpDto>> GetMemberBloodGroupReport(MemberBloodGroupReportRequest request);
    }
}
