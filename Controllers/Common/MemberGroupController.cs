using NexgenCosysReport.Dtos.RequestDtos.Common;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NexgenCosysReport.Inteface.ServiceInterface.Common;

namespace NexgenCosysReport.Controllers.Common
{
    [Route("api/[controller]")]
    [ApiController]
    public class MemberGroupController : ControllerBase
    {
        private readonly IMemberGroup _memberGroupService;

        public MemberGroupController(IMemberGroup memberGroupService)
        {
            _memberGroupService = memberGroupService;
        }
        [HttpPost("member-groups")]
        public async Task<ActionResult<List<MemberGroupResponseDto>>> GetMemberGroups(
          MemberGroupRequestDtos request)
        {
            // Basic validation: if collectionCenterId is not -1, officeId is ignored inside the service.
            // We only need to ensure officeId is provided when collectionCenterId == -1.
            if (request.CollectionCenterId == -1 && request.lstOfficeId <= 0)
                return BadRequest("Office ID is required when collectionCenterId = -1.");

            var result = await _memberGroupService.GetMemberGroups(request.lstOfficeId, request.CollectionCenterId);
            return Ok(result);
        }
    }
}
