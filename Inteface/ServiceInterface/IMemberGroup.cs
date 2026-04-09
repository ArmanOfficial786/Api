using JsSampleReport.Dtos.RequestDtos;

namespace JsSampleReport.Inteface.ServiceInterface
{
    public interface IMemberGroup
    {
        Task<List<MemberGroupResponseDto>> GetMemberGroups(long lstOfficeId, long sysCollectionCenterId);
    }
}
