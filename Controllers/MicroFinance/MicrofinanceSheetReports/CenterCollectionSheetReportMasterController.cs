// Controllers/Microfinance/MicrofinanceSheetReports/CenterCollectionSheetReportMasterController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NexgenCosysReport.Dtos.RequestDtos.Microfinance.MicrofinanceSheetReports;
using NexgenCosysReport.Inteface.ServiceInterface.Microfinance.MicrofinanceSheetReports;
using System.Security.Claims;

namespace NexgenCosysReport.Controllers.Microfinance.MicrofinanceSheetReports
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class CenterCollectionSheetReportMasterController : ControllerBase
    {
        private readonly ICenterCollectionSheetReportMasterRepository _repository;
        private readonly ILogger<CenterCollectionSheetReportMasterController> _logger;

        public CenterCollectionSheetReportMasterController(
            ICenterCollectionSheetReportMasterRepository repository,
            ILogger<CenterCollectionSheetReportMasterController> logger)
        {
            _repository = repository;
            _logger = logger;
        }

        private bool TryGetUserId(out long userId)
        {
            userId = 0;
            var userIdClaim = User.FindFirst("UserId")?.Value
                              ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return !string.IsNullOrEmpty(userIdClaim) && long.TryParse(userIdClaim, out userId);
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            try
            {
                if (!TryGetUserId(out _))
                    return Unauthorized(new { success = false, StatusCode = 401, message = "Unauthorized" });

                var data = await _repository.GetAllAsync();
                return Ok(new { success = true, StatusCode = 200, data });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetAll (CenterCollectionSheetReportMaster)");
                return StatusCode(500, new { success = false, StatusCode = 500, message = ex.Message });
            }
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            try
            {
                if (!TryGetUserId(out _))
                    return Unauthorized(new { success = false, StatusCode = 401, message = "Unauthorized" });

                var data = await _repository.GetByIdAsync(id);
                if (data == null)
                    return NotFound(new { success = false, StatusCode = 404, message = "Scheme not found" });

                return Ok(new { success = true, StatusCode = 200, data });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { success = false, StatusCode = 400, message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetById (CenterCollectionSheetReportMaster)");
                return StatusCode(500, new { success = false, StatusCode = 500, message = ex.Message });
            }
        }

        [HttpPut]
        public async Task<IActionResult> Update([FromBody] CenterCollectionSheetReportMasterUpdateDto dto)
        {
            try
            {
                if (!TryGetUserId(out _))
                    return Unauthorized(new { success = false, StatusCode = 401, message = "Unauthorized" });

                if (dto == null || !ModelState.IsValid)
                    return BadRequest(new { success = false, StatusCode = 400, message = "Invalid request" });

                if (dto.SycCollectionCenterScemeId <= 0)
                    return BadRequest(new { success = false, StatusCode = 400, message = "Select scheme." });

                var result = await _repository.UpdateAsync(dto);
                if (!result.Success)
                    return BadRequest(new { success = false, StatusCode = 400, message = result.Message });

                return Ok(new { success = true, StatusCode = 200, message = result.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Update (CenterCollectionSheetReportMaster)");
                return StatusCode(500, new { success = false, StatusCode = 500, message = ex.Message });
            }
        }
    }
}