using Microsoft.AspNetCore.Mvc;
using NexgenCosysReport.Dtos.RequestDtos.Common;
using NexgenCosysReport.Inteface.ServiceInterface.Common;

namespace NexgenCosysReport.Controllers.Common
{
    [ApiController]
    [Route("api/[controller]")]
    public class FiscalYearController : ControllerBase
    {
        private readonly IFiscalYear _fiscalYearService;
        private readonly ILogger<FiscalYearController> _logger;

        public FiscalYearController(IFiscalYear fiscalYearService, ILogger<FiscalYearController> logger)
        {
            _fiscalYearService = fiscalYearService;
            _logger = logger;
        }

        [HttpGet("GetFiscalYearsBS")]
        public async Task<ActionResult<GeneralResponse<List<FiscalYearResponse>>>> GetAll()
        {
            try
            {
                var response = new GeneralResponse<List<FiscalYearResponse>>();

                var fiscalYears = await _fiscalYearService.GetAll();

                response.isValid = true;
                response.statusCode = 200;
                response.data = fiscalYears;
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching fiscal years");
                return StatusCode(500, new GeneralResponse<List<FiscalYearResponse>>
                {
                    isValid = false,
                    statusCode = 500,
                    message = ex.Message
                });
            }
        }
    }
}