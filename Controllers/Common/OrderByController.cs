using Microsoft.AspNetCore.Mvc;
using NexgenCosysReport.Dtos.RequestDtos.Common;
using NexgenCosysReport.Inteface.ServiceInterface.Common;

namespace NexgenCosysReport.Controllers.Common
{
    [Route("api/[controller]")]
    [ApiController]
    public class OrderByController : ControllerBase
    {

        private readonly IOrderBy _orderByService;
        private readonly ILogger<OrderByController> _logger;

        public OrderByController(IOrderBy orderByService, ILogger<OrderByController> logger)
        {
            _orderByService = orderByService;
            _logger = logger;
        }
        // ? Single endpoint — returns ALL report enums at once
        [HttpGet("GetAllOrderBy")]
        public async Task<ActionResult<GeneralResponse<AllReportOrderByResponseModel>>> GetAllOrderBy()
        {
            try
            {
                var response = new GeneralResponse<AllReportOrderByResponseModel>();
                var result = _orderByService.GetAllReportOrderBy();
                if (result == null)
                {
                    response.isValid = false;
                    response.statusCode = 404;
                    response.message = "No data found";
                    return NotFound(response);
                }
                response.isValid = true;
                response.statusCode = 200;
                response.data = result;

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching OrderBy list");
                return StatusCode(500, new GeneralResponse<AllReportOrderByResponseModel>
                {
                    isValid = false,
                    statusCode = 500,
                    message = ex.Message
                });
            }
        }
    }
}
