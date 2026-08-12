using Microsoft.EntityFrameworkCore;
using NexgenCosysReport.Dtos.RequestDtos.Common;
using NexgenCosysReport.Inteface.ServiceInterface.Common;

namespace NexgenCosysReport.Repository.Common
{
    public class ShareTypeRepository : IShareType
    {
        private readonly AppDbContext _context;

        public ShareTypeRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<ShareTypeResponse>> GetAllLmtLoanMasterList()
        {
            return await _context.ShmShareTypes
                .Where(x => x.IsActive)
                .Select(x => new ShareTypeResponse
                {
                    ShmShareTypeId = x.ShmShareTypeId,
                    ShareTypeName = x.ShareTypeName
                })
                .ToListAsync();
        }
    }
}