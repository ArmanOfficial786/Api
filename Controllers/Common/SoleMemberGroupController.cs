using Microsoft.AspNetCore.Mvc;
using NexgenCosysReport.Dtos.RequestDtos.Common;
using NexgenCosysReport.Inteface.ServiceInterface.Common;

namespace NexgenCosysReport.Controllers.Common
{
    [Route("api/[controller]")]
    [ApiController]
    public class SoleMemberGroupController : ControllerBase
    {
        private readonly ISoleMemberGroup _soleMemberGroupService;

        public SoleMemberGroupController(ISoleMemberGroup soleMemberGroupService)
        {
            _soleMemberGroupService = soleMemberGroupService;
        }
        [HttpPost()]
        public async Task<ActionResult<List<SoleMemberGroupResponseDto>>> GetSoleMemberGroup([FromBody] SoleMemberGroupRequestDtos request)
        {
            if (request.lstOfficeId <= 0)
                return BadRequest("Office ID is required");
            var result = await _soleMemberGroupService.GetSoleMemberGroups(request.lstOfficeId);
            return Ok(result);
        }
    }
}
