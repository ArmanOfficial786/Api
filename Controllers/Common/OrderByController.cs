using NexgenCosysReport.Dtos.RequestDtos.Common;
using NexgenCosysReport.Services.CommonService;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
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
                    response.IsValid = false;
                    response.StatusCode = 404;
                    response.Message = "No data found";
                    return NotFound(response);
                }
                response.IsValid = true;
                response.StatusCode = 200;
                response.Message = "Success";
                response.Data = result;

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching OrderBy list");
                return StatusCode(500, new GeneralResponse<AllReportOrderByResponseModel>
                {
                    IsValid = false,
                    StatusCode = 500,
                    Message = ex.Message
                });
            }
        }
    }
}
