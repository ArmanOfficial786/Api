using JsSampleReport.Inteface.ServiceInterface;
using JsSampleReport.Services.CommonService;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace JsSampleReport.Controllers
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
        // ✅ Single endpoint — returns ALL report enums at once
        [HttpGet("GetAllOrderBy")]
        public IActionResult GetAllOrderBy()
        {
            try
            {
                var result = _orderByService.GetAllReportOrderBy();
                return Ok(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching OrderBy list");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }
    }
}
