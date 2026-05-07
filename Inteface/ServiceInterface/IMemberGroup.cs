using NexgenCosysReport.Dtos.RequestDtos.Common;

namespace NexgenCosysReport.Inteface.ServiceInterface
{
    public interface IMemberGroup
    {
        Task<List<MemberGroupResponseDto>> GetMemberGroups(long lstOfficeId, long sysCollectionCenterId);
    }
}
