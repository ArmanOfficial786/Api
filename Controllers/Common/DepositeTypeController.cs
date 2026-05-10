using NexgenCosysReport.Dtos.RequestDtos;
using NexgenCosysReport.Dtos.RequestDtos.Common;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
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
                response.IsValid = false;
                response.StatusCode = StatusCodes.Status404NotFound;
                response.Message = "No Deposite Type Found";
                return NotFound(response);
            }

            response.IsValid = true;
            response.StatusCode = StatusCodes.Status200OK;
            response.Message = "Success";
            response.Data = depositeType;
            return Ok(response);
        }
    }
}
