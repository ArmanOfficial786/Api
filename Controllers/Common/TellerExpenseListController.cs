// Controllers/Common/TellerExpenseController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NexgenCosysReport.Dtos.RequestDtos.Common;
using NexgenCosysReport.Inteface.ServiceInterface.Common;

namespace NexgenCosysReport.Controllers.Common
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class TellerExpenseListController : ControllerBase
    {
        private readonly ITellerExpenseDropdown _tellerExpenseService;
        private readonly ITokenService _tokenService;
        private readonly IDateConverterService _dateConverter;
        private readonly ILogger<TellerExpenseListController> _logger;

        public TellerExpenseListController(ITellerExpenseDropdown tellerExpenseService, IDateConverterService dateConverter, ILogger<TellerExpenseListController> logger, ITokenService tokenService)
        {
            _tellerExpenseService = tellerExpenseService;
            _dateConverter = dateConverter;
            _logger = logger;
            _tokenService = tokenService;
        }

        /// <summary>
        /// Get list of tellers for expense transactions within the given date range.
        /// Returns a bare TellerLookupResponse[] to match the generated api.ts contract
        /// (tellerExpenseListList) and the frontend's tellerExpenseService, which read
        /// res.data directly as the array — same shape as GET /api/Teller.
        /// GET /api/TellerExpenseList?fromDateBs=2081/01/01&toDateBs=2081/12/30
        /// </summary>
        [HttpGet()]
        public async Task<ActionResult<List<TellerLookupResponse>>> GetTellers(
            [FromQuery] string fromDateBs,
            [FromQuery] string toDateBs)
        {
            try
            {
                var userId = _tokenService.GetUserIdFromPrincipal(User);
                if (userId == null)
                    return Unauthorized();

                var fromDate = await _dateConverter.NepaliToEnglishAsync(fromDateBs);
                var toDate = await _dateConverter.NepaliToEnglishAsync(toDateBs);

                var tellers = await _tellerExpenseService.GetTellersAsync(fromDate, toDate, userId.Value);

                // ── Bare array, no GeneralResponse wrapper ─────────────────
                return Ok(tellers);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading expense tellers");
                return StatusCode(500, "Error loading expense tellers");
            }
        }
    }
}