using NexgenCosysReport.Dtos.RequestDtos.Common;
using NexgenCosysReport.Inteface.ServiceInterface;
using Microsoft.EntityFrameworkCore;

namespace NexgenCosysReport.Repository
{
    public class DepositeTypeRepository : IDepositeType
    {
        private readonly AppDbContext _context;

        public DepositeTypeRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<DepositTypeResponse>> GetAllDepositeType()
        {
            var depositList = await _context.SycDepositTypes
                .Where(p => p.IsActive == true)
                .OrderBy(p => p.DepositTypeName)
                .Select(p => new DepositTypeResponse
                {
                    // map your fields here based on DepositTypeResponse dto
                    DepositeTypeId = p.SycDepositTypeId,
                    DepositeTypeName = p.DepositTypeName,
                    IsActive = p.IsActive
                })
               .ToListAsync();

            return depositList;
        }
    }
}
