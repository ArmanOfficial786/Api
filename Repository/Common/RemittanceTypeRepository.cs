using Microsoft.EntityFrameworkCore;
using NexgenCosysReport.DbContext;
using NexgenCosysReport.Dtos.RequestDtos.Common;
using NexgenCosysReport.Inteface.ServiceInterface.Common;

namespace NexgenCosysReport.Repository.Common
{
    public class RemittanceTypeRepository : IRemittanceType
    {
        private readonly AppDbContext _context;

        public RemittanceTypeRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<RemittanceTypeResponse>> GetAllActive()
        {
            return await _context.RemRemittanceTypes
                .AsNoTracking()
                .Where(x => x.IsActive == true)
                .OrderBy(x => x.RemittanceTypeName)
                .Select(x => new RemittanceTypeResponse
                {
                    RemittanceTypeId = x.RemRemittanceTypeId,
                    RemittanceTypeName = x.RemittanceTypeName
                })
                .ToListAsync();
        }
    }
}