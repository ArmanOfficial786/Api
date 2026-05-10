using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NexgenCosysReport.Dtos.RequestDtos.Account;
using NexgenCosysReport.Dtos.RequestDtos.Common;
using NexgenCosysReport.Inteface.ReportInterface;
using NexgenCosysReport.Inteface.ServiceInterface.Account;
using NexgenCosysReport.Inteface.ServiceInterface.Common;

namespace NexgenCosysReport.Controllers.Account
{
    [Route("api/[controller]")]
    [ApiController]
    public class SavingACWiseBalanceReportController : ControllerBase
    {
        private readonly IJsReportService _jsReportService;
        private readonly ICommonHeaderRepository _commonHeaderRepository;
        private readonly ISavingAcWiseBalance _savingAcWiseBalanceRepository;

        public SavingACWiseBalanceReportController(IJsReportService jsReportService, ICommonHeaderRepository commonHeaderRepository, ISavingAcWiseBalance savingAcWiseBalanceRepository)
        {
            _jsReportService = jsReportService;
            _commonHeaderRepository = commonHeaderRepository;
            _savingAcWiseBalanceRepository = savingAcWiseBalanceRepository;
        }
        [HttpPost("SavingACWiseBalanceReport")]
        public async Task<ActionResult> SavingAcWiseReport([FromBody] SavingAcWiseBalanceRequest request, [FromQuery] string format = "VIEW")
        {
            var reportName = "SavingACWiseBalanceReport";
            var upperFormat = format.ToUpper();
            var response = new GeneralResponse<SavingAcWiseBalanceResponse>();
            try
            {
                if (request == null || !ModelState.IsValid)
                {
                    response.IsValid = false;
                    response.StatusCode = StatusCodes.Status400BadRequest;
                    response.Message = "Invalid request data.";
                    return BadRequest(response);
                }


                var commonHeader = await _commonHeaderRepository.GetCommonHeaders();
                var reportData = await _savingAcWiseBalanceRepository.GetSavingAcWiseBalanceAsync(request);


                response.IsValid = true;
                response.StatusCode = StatusCodes.Status200OK;
                response.Message = "Report generated successfully.";
                return Ok(response);


            }
            catch (Exception ex) 
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "An error occurred while processing your request.",
                    error = ex.Message
                });
            }

        
        }
    }
}
