using NexgenCosysReport.Dtos.RequestDtos.Common;

namespace NexgenCosysReport.Inteface.ServiceInterface.Common
{
    public interface ISoleMemberGroup
    {
        Task<List<SoleMemberGroupResponseDto>> GetSoleMemberGroups(long lstOfficeId);
    }
}
