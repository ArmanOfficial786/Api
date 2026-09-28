using Microsoft.AspNetCore.Mvc;
using NexgenCosysReport.Dtos.RequestDtos.Common;
using NexgenCosysReport.Inteface.ServiceInterface.Common;

namespace NexgenCosysReport.Controllers.Common
{
    [ApiController]
    [Route("api/[controller]")]
    public class RemittanceTypeController : ControllerBase
    {
        private readonly IRemittanceType _remittanceTypeService;
        private readonly ILogger<RemittanceTypeController> _logger;

        public RemittanceTypeController(IRemittanceType remittanceTypeService, ILogger<RemittanceTypeController> logger)
        {
            _remittanceTypeService = remittanceTypeService;
            _logger = logger;
        }

        [HttpGet("GetAllRemittanceTypes")]
        public async Task<ActionResult<GeneralResponse<List<RemittanceTypeResponse>>>> GetAllRemittanceTypes()
        {
            try
            {
                var response = new GeneralResponse<List<RemittanceTypeResponse>>();
                var remittanceTypes = await _remittanceTypeService.GetAllActive();

                if (remittanceTypes == null || !remittanceTypes.Any())
                {
                    response.isValid = false;
                    response.statusCode = 400;
                    response.message = "No remittance types found";
                    return BadRequest(response);
                }

                response.isValid = true;
                response.statusCode = 200;
                response.data = remittanceTypes;
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching remittance types");
                return StatusCode(500, new GeneralResponse<string>
                {
                    isValid = false,
                    statusCode = 500,
                    message = ex.Message
                });
            }
        }
    }
}