using Microsoft.AspNetCore.Mvc;
using NexgenCosysReport.Dtos.RequestDtos.Common;
using NexgenCosysReport.Inteface.ServiceInterface.Common;

namespace NexgenCosysReport.Controllers.Common
{
    [ApiController]
    [Route("api/[controller]")]
    public class MemberTypeController : ControllerBase
    {
        private readonly IMemberType _memberTypeService;
        private readonly ILogger<MemberTypeController> _logger;

        public MemberTypeController(IMemberType memberTypeService, ILogger<MemberTypeController> logger)
        {
            _memberTypeService = memberTypeService;
            _logger = logger;
        }

        [HttpGet("GetAllActive")]
        public async Task<ActionResult<GeneralResponse<List<MemberTypeResponse>>>> GetAllActive()
        {
            try
            {
                var response = new GeneralResponse<List<MemberTypeResponse>>();

                var memberTypes = await _memberTypeService.GetAllActive();

                if (memberTypes == null || !memberTypes.Any())
                {
                    response.isValid = false;
                    response.statusCode = 400;
                    response.message = "No member types found";
                    return BadRequest(response);
                }

                response.isValid = true;
                response.statusCode = 200;
                response.data = memberTypes;
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching member types");
                return StatusCode(500, new GeneralResponse<List<MemberTypeResponse>>
                {
                    isValid = false,
                    statusCode = 500,
                    message = ex.Message
                });
            }
        }
    }
}