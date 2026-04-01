using JsSampleReport.Dtos.RequestDtos;

namespace JsSampleReport.Inteface.ServiceInterface
{
    public interface IBranch
    {
        Task<List<BranchResponse>> GetAllBranches();
    }
}
