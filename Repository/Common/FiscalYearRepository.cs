using Microsoft.EntityFrameworkCore;
using NexgenCosysReport.DbContext;
using NexgenCosysReport.Dtos.RequestDtos.Common;
using NexgenCosysReport.Inteface.ServiceInterface.Common;

namespace NexgenCosysReport.Repository.Common
{
    public class FiscalYearRepository : IFiscalYear
    {
        private readonly AppDbContext _context;

        public FiscalYearRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<FiscalYearResponse>> GetAll()
        {
            var fiscalYears = await _context.AcoFiscalYears
                .OrderBy(x => x.FiscalYearFromOnBs)
                .Select(x => new FiscalYearResponse
                {
                    FiscalYearId = x.AcoFiscalYearId,
                    FiscalYearToOnBs = x.FiscalYearToOnBs
                })
                .ToListAsync();

            return fiscalYears;
        }
    }
}