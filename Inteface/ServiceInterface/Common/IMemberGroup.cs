using NexgenCosysReport.Dtos.RequestDtos.Common;

namespace NexgenCosysReport.Inteface.ServiceInterface.Common
{
    public interface IMemberGroup
    {
        Task<List<MemberGroupResponseDto>> GetMemberGroups(long lstOfficeId, long sysCollectionCenterId);
    }
}
