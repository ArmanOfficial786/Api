using NexgenCosysReport.Dtos.RequestDtos.Common;

namespace NexgenCosysReport.Inteface.ServiceInterface.Common
{
    public interface IMemberType
    {
        Task<List<MemberTypeResponse>> GetAllActive();
    }
}
