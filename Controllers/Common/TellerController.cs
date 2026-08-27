// Controllers/Common/TellerController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NexgenCosysReport.Dtos.RequestDtos.Common;
using NexgenCosysReport.Inteface.ServiceInterface.Common;
using System.Security.Claims;

namespace NexgenCosysAPI.Controllers.Common
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class TellerController : ControllerBase
    {
        private readonly ITeller _tellerService;
        private readonly IDateConverterService _dateConverter;
        private readonly ILogger<TellerController> _logger;

        public TellerController(ITeller tellerService, IDateConverterService dateConverter, ILogger<TellerController> logger)
        {
            _tellerService = tellerService;
            _dateConverter = dateConverter;
            _logger = logger;
        }

        /// <summary>
        /// Get list of tellers for the given date range (replaces btnLoadTeller_Click)
        /// GET /api/Teller/Tellers?fromDateBs=2081/01/01&toDateBs=2081/12/30
        /// </summary>
        [HttpGet("Tellers")]
        public async Task<ActionResult<List<TellerLookupResponse>>> GetTellers(
    [FromQuery] string fromDateBs,
    [FromQuery] string toDateBs)
        {
            try
            {
                var userId = GetUserIdFromToken();
                if (userId == null)
                    return Unauthorized(new GeneralResponse<List<TellerLookupResponse>>
                    {
                        isValid = false,
                        statusCode = 401,
                        message = "User not authenticated"
                    });

                var fromDate = await _dateConverter.NepaliToEnglishAsync(fromDateBs);
                var toDate = await _dateConverter.NepaliToEnglishAsync(toDateBs);

                var tellers = await _tellerService.GetTellersAsync(fromDate, toDate, userId.Value);

                return Ok(new GeneralResponse<List<TellerLookupResponse>>
                {
                    isValid = true,
                    statusCode = 200,
                    data = tellers
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading tellers");
                return StatusCode(500, new GeneralResponse<List<TellerLookupResponse>>
                {
                    isValid = false,
                    statusCode = 500,
                    message = "Error loading tellers"
                });
            }
        }

        private long? GetUserIdFromToken()
        {
            var userIdClaim = User.FindFirst("UserId")?.Value;
            if (string.IsNullOrEmpty(userIdClaim))
                userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (long.TryParse(userIdClaim, out var id))
                return id;
            return null;
        }
    }
}