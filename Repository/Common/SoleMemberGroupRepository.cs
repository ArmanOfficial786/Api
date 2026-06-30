using Microsoft.EntityFrameworkCore;
using NexgenCosysReport.Dtos.RequestDtos.Common;
using NexgenCosysReport.Inteface.ServiceInterface.Common;

namespace NexgenCosysReport.Repository.Common
{
    public class SoleMemberGroupRepository : ISoleMemberGroup
    {
        private readonly AppDbContext _context;

        public SoleMemberGroupRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<SoleMemberGroupResponseDto>> GetSoleMemberGroups(long lstOfficeId)
        {
            try
            {
                // Query SycMemberGroup directly, filtering by UsmOfficeId
                var result = await _context.SycMemberGroups
                    .Where(mg => mg.UsmOfficeId == lstOfficeId)
                    .Select(mg => new SoleMemberGroupResponseDto
                    {
                        MemberGroupId = mg.SycMemberGroupId,
                        UsmOfficeId = mg.UsmOfficeId.Value,
                        Name = mg.Name
                    })
                    .ToListAsync();

                return result;
            }
            catch (Exception ex)
            {

                throw;
            }
        }
    }
}