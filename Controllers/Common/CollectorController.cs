using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NexgenCosysReport.DbContext;
using NexgenCosysReport.Dtos.RequestDtos.Common;

namespace NexgenCosysReport.Controllers.Common
{
    [Route("api/[controller]")]
    [ApiController]
    public class CollectorController : ControllerBase
    {
        private readonly AppDbContext _context;

        public CollectorController(AppDbContext appDbContext)
        {
            _context = appDbContext;
        }

        [HttpGet("getCollector")]
        public async Task<ActionResult<GeneralResponse<List<CollectorResponse>>>> GetCollector([FromQuery] long userId)
        {
            var response = new GeneralResponse<List<CollectorResponse>>();

            if (userId <= 0)
            {
                response.isValid = false;
                response.statusCode = StatusCodes.Status400BadRequest;
                response.message = "Invalid UserId";
                return BadRequest(response);
            }

            var collectors = await _context.HurCollectors
                .Where(ao => ao.IsActive == true)
                .Join(                                                      // ? method syntax stays IQueryable
                    _context.UsmRelationUserToOffices.Where(re => re.UsmUserId == userId),
                    ao => ao.UsmOfficeId,
                    re => re.UsmOfficeId,
                    (ao, re) => new CollectorResponse
                    {
                        Id = ao.HurCollectorId,
                        CollectorCode = ao.CollectorCode,
                        CollectorName = ao.CollectorCode + " " + ao.CollectorFullName
                    }
                )
                .OrderBy(p => p.CollectorName)
                .ToListAsync();                                             // ? Works without error

            response.isValid = true;
            response.statusCode = StatusCodes.Status200OK;
            response.data = collectors;
            return Ok(response);
        }
    }
}
