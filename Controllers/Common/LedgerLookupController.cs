using Microsoft.AspNetCore.Mvc;
using NexgenCosysReport.Dtos.RequestDtos.Common;
using NexgenCosysReport.Inteface.ServiceInterface.Common;
using System.Diagnostics;

namespace NexgenCosysReport.Controllers.Common
{
    [ApiController]
    [Route("api/[controller]")]
    public class LedgerLookupController : ControllerBase
    {
        private readonly ILedgerLookupRepository _repository;
        private readonly ILogger<LedgerLookupController> _logger;

        public LedgerLookupController(ILedgerLookupRepository repository, ILogger<LedgerLookupController> logger)
        {
            _repository = repository;
            _logger = logger;
        }

        [HttpGet("LedgerHead")]
        public async Task<IActionResult> GetLedgerHeads()
        {
            try
            {
                var data = await _repository.GetLedgerHeadsAsync();
                return Ok(new { success = true, data });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = ex.Message,
                    inner = ex.InnerException?.Message,
                    stack = ex.StackTrace
                });
            }
        }

        [HttpPost("LedgerName")]
        public async Task<IActionResult> GetLedgerNames([FromBody] LedgerNameRequestDto request)
        {
            var stopwatch = Stopwatch.StartNew();
            try
            {
                _logger.LogInformation("GetLedgerNames endpoint called");

                if (request == null || !ModelState.IsValid)
                {
                    return BadRequest(new { success = false, StatusCode = 400, message = "Invalid request" });
                }

                var data = await _repository.GetLedgerNamesAsync(request);
                stopwatch.Stop();
                _logger.LogInformation("GetLedgerNames completed in {TotalMs}ms with {ResultCount} results", 
                    stopwatch.ElapsedMilliseconds, data.Count);

                return Ok(new { success = true, data, elapsed_ms = stopwatch.ElapsedMilliseconds });
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "GetLedgerNames failed after {ElapsedMs}ms", stopwatch.ElapsedMilliseconds);
                return StatusCode(500, new
                {
                    message = ex.Message,
                    inner = ex.InnerException?.Message,
                    stack = ex.StackTrace,
                    elapsed_ms = stopwatch.ElapsedMilliseconds
                });
            }
        }

        [HttpPost("SubLedgerName")]
        public async Task<IActionResult> GetSubLedgerNames([FromBody] SubLedgerNameRequestDto request)
        {
            var stopwatch = Stopwatch.StartNew();
            try
            {
                _logger.LogInformation("GetSubLedgerNames endpoint called");

                if (request == null || !ModelState.IsValid || string.IsNullOrWhiteSpace(request.MainLedger))
                {
                    return BadRequest(new { success = false, StatusCode = 400, message = "MainLedger is required" });
                }

                var data = await _repository.GetSubLedgerNamesAsync(request);
                stopwatch.Stop();
                _logger.LogInformation("GetSubLedgerNames completed in {TotalMs}ms with {ResultCount} results", 
                    stopwatch.ElapsedMilliseconds, data.Count);

                return Ok(new { success = true, data, elapsed_ms = stopwatch.ElapsedMilliseconds });
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "GetSubLedgerNames failed after {ElapsedMs}ms", stopwatch.ElapsedMilliseconds);
                return StatusCode(500, new
                {
                    message = ex.Message,
                    inner = ex.InnerException?.Message,
                    stack = ex.StackTrace,
                    elapsed_ms = stopwatch.ElapsedMilliseconds
                });
            }
        }
    }
}