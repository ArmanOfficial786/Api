using NexgenCosysReport.Dtos.RequestDtos.Common;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

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

        [HttpPost("getCollector")]
        public async Task<ActionResult> GetCollector([FromQuery] long userId)
        {
            var response = new GeneralResponse<List<CollectorResponse>>(); // ? List<> added

            if (userId <= 0)
            {
                response.IsValid = false;
                response.StatusCode = StatusCodes.Status400BadRequest;
                response.Message = "Invalid UserId";
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

            response.IsValid = true;
            response.StatusCode = StatusCodes.Status200OK;
            response.Message = "Success";
            response.Data = collectors;
            return Ok(response);
        }
    }
}