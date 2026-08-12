using Microsoft.EntityFrameworkCore;
using NexgenCosysReport.Dtos.RequestDtos.Common;
using NexgenCosysReport.Inteface.ServiceInterface.Common;
namespace NexgenCosysReport.Repository.Common
{
    public class LmtLoanMasterListRepository : ILmtLoanMasterList
    {
        private readonly AppDbContext _context;

        public LmtLoanMasterListRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<LmtLoanMaseterListResponse>> GetAllLmtLoanMasterList()
        {
            var result = await _context.LmtLoanTypeMasters
                .Select(x => new LmtLoanMaseterListResponse
                {
                    LmtLoanTypeMasterId = x.LmtLoanTypeMasterId,
                    LoanTypeName = x.LoanTypeName
                })
                .ToListAsync();
            return result;
        }
    }
}
