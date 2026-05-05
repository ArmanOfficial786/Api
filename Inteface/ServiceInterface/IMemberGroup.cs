using JsSampleReport.Dtos.RequestDtos.Common;

namespace JsSampleReport.Inteface.ServiceInterface
{
    public interface IMemberGroup
    {
        Task<List<MemberGroupResponseDto>> GetMemberGroups(long lstOfficeId, long sysCollectionCenterId);
    }
}
