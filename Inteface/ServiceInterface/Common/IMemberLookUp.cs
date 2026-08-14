using NexgenCosysReport.Dtos.RequestDtos.Common;

namespace NexgenCosysReport.Inteface.ServiceInterface.Common
{
    public interface IMemberLookUp
    {
        // Grid search with filters + pagination
        Task<Pagination<MemberLookUpDtos>> GetMemberListAsync(
            MemberLookUpRequest request,
            long userId);

        // Single member when user clicks Sel button
        Task<MemberSelectedDto?> GetSelectedMemberAsync(
            long memMemberRegistrationId,
            long userId);
    }
}
