using NexgenCosysReport.Dtos.RequestDtos.Member;

namespace NexgenCosysReport.Inteface.ServiceInterface.Member
{
    public interface IMemberAllDetails
    {
        Task<List<MemberAllDetailSpResponse>> GetMemberAllDetailsAsync(MemberAllDetailRequst request);
    }
}
