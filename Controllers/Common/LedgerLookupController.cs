//using Microsoft.AspNetCore.Mvc;
//using NexgenCosysReport.Dtos.RequestDtos.Common;
//using NexgenCosysReport.Inteface.ServiceInterface.Common;

//namespace NexgenCosysReport.Controllers.Common
//{
//    [ApiController]
//    [Route("api/[controller]")]
//    public class LedgerLookupController : ControllerBase
//    {
//        private readonly ILedgerLookupRepository _repository;

//        public LedgerLookupController(ILedgerLookupRepository repository)
//        {
//            _repository = repository;
//        }

//        [HttpGet("LedgerHead")]
//        public async Task<IActionResult> GetLedgerHeads()
//        {
//            try
//            {
//                var data = await _repository.GetLedgerHeadsAsync();
//                return Ok(new { success = true, data });
//            }
//            catch (Exception ex)
//            {
//                return StatusCode(500, new
//                {
//                    message = ex.Message,
//                    inner = ex.InnerException?.Message,
//                    stack = ex.StackTrace
//                });
//            }
//        }

//        [HttpPost("LedgerName")]
//        public async Task<IActionResult> GetLedgerNames([FromBody] LedgerNameRequestDto request)
//        {
//            try
//            {
//                if (request == null || !ModelState.IsValid)
//                {
//                    return BadRequest(new { success = false, StatusCode = 400, message = "Invalid request" });
//                }

//                var data = await _repository.GetLedgerNamesAsync(request);
//                return Ok(new { success = true, data });
//            }
//            catch (Exception ex)
//            {
//                return StatusCode(500, new
//                {
//                    message = ex.Message,
//                    inner = ex.InnerException?.Message,
//                    stack = ex.StackTrace
//                });
//            }
//        }

//        [HttpPost("SubLedgerName")]
//        public async Task<IActionResult> GetSubLedgerNames([FromBody] SubLedgerNameRequestDto request)
//        {
//            try
//            {
//                if (request == null || !ModelState.IsValid || string.IsNullOrWhiteSpace(request.MainLedger))
//                {
//                    return BadRequest(new { success = false, StatusCode = 400, message = "MainLedger is required" });
//                }

//                var data = await _repository.GetSubLedgerNamesAsync(request);
//                return Ok(new { success = true, data });
//            }
//            catch (Exception ex)
//            {
//                return StatusCode(500, new
//                {
//                    message = ex.Message,
//                    inner = ex.InnerException?.Message,
//                    stack = ex.StackTrace
//                });
//            }
//        }
//    }
//}






using Microsoft.AspNetCore.Mvc;
using NexgenCosysReport.Dtos.RequestDtos.Common;
using NexgenCosysReport.Inteface.ServiceInterface.Common;

namespace NexgenCosysReport.Controllers.Common
{
    [ApiController]
    [Route("api/[controller]")]
    public class LedgerLookupController : ControllerBase
    {
        private readonly ILedgerLookupRepository _repository;

        public LedgerLookupController(ILedgerLookupRepository repository)
        {
            _repository = repository;
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

        [HttpGet("LedgerName")]
        public async Task<IActionResult> GetLedgerNames([FromQuery] LedgerNameRequestDto request)
        {
            try
            {
                if (request == null || !ModelState.IsValid)
                {
                    return BadRequest(new { success = false, StatusCode = 400, message = "Invalid request" });
                }

                var data = await _repository.GetLedgerNamesAsync(request);
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

        [HttpGet("SubLedgerName")]
        public async Task<IActionResult> GetSubLedgerNames([FromQuery] SubLedgerNameRequestDto request)
        {
            try
            {
                if (request == null || !ModelState.IsValid || string.IsNullOrWhiteSpace(request.MainLedger))
                {
                    return BadRequest(new { success = false, StatusCode = 400, message = "MainLedger is required" });
                }

                var data = await _repository.GetSubLedgerNamesAsync(request);
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
        [HttpGet("SecondSubLedgerName")]
        public async Task<IActionResult> GetSecondSubLedgerNames([FromQuery] SecondSubLedgerNameRequestDto request)
        {
            try
            {
                if (request == null || !ModelState.IsValid || string.IsNullOrWhiteSpace(request.SubLedger1))
                    return BadRequest(new { success = false, StatusCode = 400, message = "SubLedger1 is required" });

                var data = await _repository.GetSecondSubLedgerNamesAsync(request);
                return Ok(new { success = true, data });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message, inner = ex.InnerException?.Message, stack = ex.StackTrace });
            }
        }

        [HttpGet("ThirdSubLedgerName")]
        public async Task<IActionResult> GetThirdSubLedgerNames([FromQuery] ThirdSubLedgerNameRequestDto request)
        {
            try
            {
                if (request == null || !ModelState.IsValid || string.IsNullOrWhiteSpace(request.SubLedger2))
                    return BadRequest(new { success = false, StatusCode = 400, message = "SubLedger2 is required" });

                var data = await _repository.GetThirdSubLedgerNamesAsync(request);
                return Ok(new { success = true, data });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message, inner = ex.InnerException?.Message, stack = ex.StackTrace });
            }
        }

        [HttpGet("FourthSubLedgerName")]
        public async Task<IActionResult> GetFourthSubLedgerNames([FromQuery] FourthSubLedgerNameRequestDto request)
        {
            try
            {
                if (request == null || !ModelState.IsValid || string.IsNullOrWhiteSpace(request.SubLedger3))
                    return BadRequest(new { success = false, StatusCode = 400, message = "SubLedger3 is required" });

                var data = await _repository.GetFourthSubLedgerNamesAsync(request);
                return Ok(new { success = true, data });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message, inner = ex.InnerException?.Message, stack = ex.StackTrace });
            }
        }
    }
}