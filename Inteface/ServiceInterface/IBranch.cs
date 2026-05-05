using JsSampleReport.Dtos.RequestDtos.Common;

namespace JsSampleReport.Inteface.ServiceInterface
{
    public interface IBranch
    {
        Task<List<BranchResponse>> GetByUserId(long usmUserId);
    }
}
