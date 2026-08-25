
//using NexgenCosysReport.Dtos.RequestDtos;
using NexgenCosysReport.DbContext;
//using NexgenCosysReport.Inteface.ServiceInterface;
//using Microsoft.EntityFrameworkCore;

//namespace NexgenCosysReport.Repository
//{
//    public class BranchRepository : IBranch
//    {
//        private readonly AppDbContext _context;

//        public BranchRepository(AppDbContext context)
//        {
//            _context = context;
//        }

//        public async Task<List<BranchResponse>> GetAllBranches()
//        {
//            var branchList = await _context.UsmOffices
//                .Where(x => x.IsActive)
//                .OrderBy(x => x.OfficeName)
//                .Select(x => new BranchResponse
//                {
//                    BranchId = x.UsmOfficeId,
//                    BranchName = x.OfficeName
//                })
//                .ToListAsync();

//          

//            return branchList;
//        }
//    }
//}






using NexgenCosysReport.Dtos.RequestDtos.Common;
using Microsoft.EntityFrameworkCore;
using NexgenCosysReport.Inteface.ServiceInterface.Common;

namespace NexgenCosysReport.Repository.Common
{
    public class BranchRepository : IBranch
    {
        private readonly AppDbContext _context;

        public BranchRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<BranchResponse>> GetByUserId(long usmUserId)
        {
            //Query according to role

            //var branchList = await (
            //    from relation in _context.UsmRelationUserToOffices
            //    join office in _context.UsmOffices
            //        on relation.UsmOfficeId equals office.UsmOfficeId
            //    where relation.UsmUserId == usmUserId
            //       && office.IsActive == true
            //    orderby office.OfficeName
            //    select new BranchResponse
            //    {
            //        BranchId = office.UsmOfficeId,
            //        BranchName = office.OfficeName
            //    }
            //).ToListAsync();

            //All branch fetch
            var branchList = await _context.UsmOffices
                .Where(x => x.IsActive)
                .OrderBy(x => x.OfficeName)
                .Select(x => new BranchResponse
                {
                    BranchId = x.UsmOfficeId,
                    BranchName = x.OfficeName
                })
                .ToListAsync();



            return branchList;



            return branchList;
        }
    }
}
