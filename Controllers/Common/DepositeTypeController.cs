using Microsoft.AspNetCore.Mvc;
using NexgenCosysReport.Dtos.RequestDtos.Common;
using NexgenCosysReport.Inteface.ServiceInterface.Common;

namespace NexgenCosysReport.Controllers.Common
{
    [Route("api/[controller]")]
    [ApiController]
    public class DepositeTypeController : ControllerBase
    {
        private readonly ILogger<DepositeTypeController> _logger;
        private readonly IDepositeType _depositeTypeService;

        public DepositeTypeController(ILogger<DepositeTypeController> logger, IDepositeType depositeTypeService)
        {
            _logger = logger;
            _depositeTypeService = depositeTypeService;
        }

        [HttpGet("getDepositeType")]
        public async Task<ActionResult> GetAllDepositeType()
        {
            var response = new GeneralResponse<List<DepositTypeResponse>>();
            var depositeType = await _depositeTypeService.GetAllDepositeType();
            if (depositeType == null)
            {
                response.isValid = false;
                response.statusCode = StatusCodes.Status404NotFound;
                response.message = "No Deposite Type Found";
                return NotFound(response);
            }

            response.isValid = true;
            response.statusCode = StatusCodes.Status200OK;
            response.message = "Success";
            response.data = depositeType;
            return Ok(response);
        }
    }
}
