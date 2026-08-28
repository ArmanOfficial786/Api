using NexgenCosysReport.Dtos.RequestDtos.Common;

namespace NexgenCosysReport.Inteface.ServiceInterface.Common
{
    public interface IBranch
    {
        Task<List<BranchResponse>> GetByUserId(long usmUserId);

        Task<List<BranchResponse>> GetCollectionBranch(long usmUserId);


    }
}
