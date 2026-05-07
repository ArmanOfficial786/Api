using NexgenCosysReport.Dtos.RequestDtos.Common;

namespace NexgenCosysReport.Inteface.ServiceInterface
{
    public interface IBranch
    {
        Task<List<BranchResponse>> GetByUserId(long usmUserId);
    }
}
