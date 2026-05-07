using NexgenCosysReport.Dtos.RequestDtos.Common;

namespace NexgenCosysReport.Inteface.ServiceInterface
{
    public interface IMemberLookUp
    {
        // Grid search with filters + pagination
        Task<PagedResult<MemberLookUpDtos>> GetMemberListAsync(
            MemberLookUpRequest request,
            long userId);

        // Single member when user clicks Sel button
        Task<MemberSelectedDto?> GetSelectedMemberAsync(
            long memMemberRegistrationId,
            long userId);
    }
}
