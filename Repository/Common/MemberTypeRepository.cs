using Microsoft.EntityFrameworkCore;
using NexgenCosysReport.DbContext;
using NexgenCosysReport.Dtos.RequestDtos.Common;
using NexgenCosysReport.Inteface.ServiceInterface.Common;

namespace NexgenCosysReport.Repository.Common
{
    public class MemberTypeRepository : IMemberType
    {
        private readonly AppDbContext _context;

        public MemberTypeRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<MemberTypeResponse>> GetAllActive()
        {
            var memberTypes = await _context.SycMemberTypes
                .Where(x => x.IsActive == true)
                .OrderBy(x => x.MemberTypeName)
                .Select(x => new MemberTypeResponse
                {
                    MemberTypeId = x.SycMemberTypeId,
                    MemberTypeName = x.MemberTypeName
                })
                .ToListAsync();

            return memberTypes;
        }
    }
}