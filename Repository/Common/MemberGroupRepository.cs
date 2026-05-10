using NexgenCosysReport.Dtos.RequestDtos.Common;
using NexgenCosysReport.Models;
using Microsoft.EntityFrameworkCore;
using NexgenCosysReport.Inteface.ServiceInterface.Common;

namespace NexgenCosysReport.Repository.Common
{
    public class MemberGroupRepository : IMemberGroup
    {
        private readonly AppDbContext _context;

        public MemberGroupRepository(AppDbContext context)
        {
            _context = context;
        }

        // Return type must match interface: Task<List<MemberGroupResponseDto>>
        public async Task<List<MemberGroupResponseDto>> GetMemberGroups(long lstOfficeId, long CollectionCenterId)
        {
            List<MemberGroupResponseDto> result = null;

            try
            {
                IQueryable<SycMemberGroup> query = _context.SycMemberGroups;

                if (CollectionCenterId == -1)
                {
                    if (lstOfficeId <= 0)
                        return new List<MemberGroupResponseDto>();

                    var officeIdList = new List<long> { lstOfficeId };

                    // Handle nullable UsmOfficeId: compare with .Value after null check
                    query = query.Where(p => p.UsmOfficeId != null && officeIdList.Contains(p.UsmOfficeId.Value));
                }
                else
                {
                    query = query.Where(p => p.SycCollectionCenterId == CollectionCenterId);
                }

                var entities = await query.ToListAsync();

                // Map to DTO – convert nullable long? to long (provide default 0 or handle null)
                result = entities.Select(p => new MemberGroupResponseDto
                {
                    MemberGroupId = p.SycMemberGroupId,
                    Name = p.Name,
                    CollectionCenterId = p.SycCollectionCenterId ?? 0,
                    UsmOfficeId = p.UsmOfficeId ?? 0   // Convert long? to long
                }).ToList();
            }
            catch (Exception ex)
            {
                // Log error as needed
                throw;
            }

            return result;
        }
    }
}