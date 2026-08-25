using NexgenCosysReport.Dtos.RequestDtos.Common;
using NexgenCosysReport.DbContext;
using Microsoft.EntityFrameworkCore;
using NexgenCosysReport.Inteface.ServiceInterface.Common;

namespace NexgenCosysReport.Repository.Common
{
    public class CollectionCenterRepository : ICollectionCenter
    {
        private readonly AppDbContext _context;

        public CollectionCenterRepository(AppDbContext context)
        {
            _context = context;
        }

        // Match interface: parameter type long, not string
        public async Task<List<CollectionCenterResponseDto>> GetCollectionCenters(long lstOfficeId)
        {
            var data = await _context.SycCollectionCenters
                .Where(x => x.UsmOfficeId == lstOfficeId)   // direct equality, not Contains
                .OrderBy(x => Convert.ToInt64(x.CollectionCenterShortCode))
                .Select(x => new CollectionCenterResponseDto
                {
                    CollectionCenterId = x.SycCollectionCenterId,
                    CollectionCenterShortCode = x.CollectionCenterShortCode,
                    CollectionCenterName = x.CollectionCenterName
                })
                .ToListAsync();

            return data;
        }
    }
}
