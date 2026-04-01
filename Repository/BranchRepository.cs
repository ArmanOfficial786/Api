
using JsSampleReport.Dtos.RequestDtos;
using JsSampleReport.Inteface.ServiceInterface;
using Microsoft.EntityFrameworkCore;

namespace JsSampleReport.Repository
{
    public class BranchRepository : IBranch
    {
        private readonly AppDbContext _context;

        public BranchRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<BranchResponse>> GetAllBranches()
        {
            var branchList = await _context.UsmOffices
                .Where(x => x.IsActive)
                .OrderBy(x => x.OfficeName)
                .Select(x => new BranchResponse
                {
                    BranchId = x.UsmOfficeId,
                    BranchName = x.OfficeName
                })
                .ToListAsync();

            // ✅ Prepend "All" as default first option
            branchList.Insert(0, new BranchResponse
            {
                BranchId = -1,
                BranchName = "All"
            });

            return branchList;
        }
    }
}