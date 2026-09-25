//using Microsoft.EntityFrameworkCore;
//using NexgenCosysReport.DbContext;
//using NexgenCosysReport.Dtos.RequestDtos.Common;
//using NexgenCosysReport.Inteface.ServiceInterface.Common;

//namespace NexgenCosysReport.Repository.Common
//{
//    public class CollectionCenterRepository : ICollectionCenter
//    {
//        private readonly AppDbContext _context;

//        public CollectionCenterRepository(AppDbContext context)
//        {
//            _context = context;
//        }

//        // Match interface: parameter type long, not string
//        public async Task<List<CollectionCenterResponseDto>> GetCollectionCenters(long lstOfficeId)
//        {
//            var data = await _context.SycCollectionCenters
//                .Where(x => x.UsmOfficeId == lstOfficeId)
//                .OrderBy(x => Convert.ToInt64(x.CollectionCenterShortCode))
//                .Select(x => new CollectionCenterResponseDto
//                {
//                    CollectionCenterId = x.SycCollectionCenterId,
//                    CollectionCenterShortCode = x.CollectionCenterShortCode,
//                    CollectionCenterName = x.CollectionCenterName
//                })
//                .ToListAsync();

//            return data;
//        }


//    }
//}


using Microsoft.EntityFrameworkCore;
using NexgenCosysReport.DbContext;
using NexgenCosysReport.Dtos.RequestDtos.Common;
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

        public async Task<List<CollectionCenterResponseDto>> GetCollectionCenters(long lstOfficeId)
        {
            var data = await _context.SycCollectionCenters
                .Where(x => x.UsmOfficeId == lstOfficeId)
                .OrderBy(x => Convert.ToInt64(x.CollectionCenterShortCode))
                .Select(x => new CollectionCenterResponseDto
                {
                    CollectionCenterId = x.SycCollectionCenterId,
                    CollectionCenterShortCode = x.CollectionCenterShortCode,
                    CollectionCenterName = x.CollectionCenterName,
                    Address = x.Address,
                    MeetingStartDateOnBs = x.MeetingStartDateOnBs,
                    MeetingTime = x.MeetingTime
                })
                .ToListAsync();

            return data;
        }
    }
}

