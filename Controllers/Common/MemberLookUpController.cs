// Controllers/MemberLookUpController.cs
using JsSampleReport.Dtos.RequestDtos.Common;
using JsSampleReport.Inteface.ServiceInterface;
using Microsoft.AspNetCore.Mvc;

namespace JsSampleReport.Controllers.Common
{
    [Route("api/[controller]")]
    [ApiController]
    public class MemberLookUpController : ControllerBase
    {
        private readonly IMemberLookUp _repo;
        private readonly ILogger<MemberLookUpController> _logger;

        public MemberLookUpController(
            IMemberLookUp repo,
            ILogger<MemberLookUpController> logger)
        {
            _repo = repo;
            _logger = logger;
        }

        /// <summary>
        /// Loads the grid — 10 records per page with optional column filters.
        /// GET /api/MemberLookUp/search?Page=1&MemberName=Ram&Gender=Male
        /// </summary>
        [HttpGet("search")]
        public async Task<ActionResult<PagedResult<MemberLookUpDtos>>> Search(
            [FromQuery] MemberLookUpRequest request)
        {
            try
            {
                long userId = GetUserId();   // 🔧 replace with your actual claim/session
                var result = await _repo.GetMemberListAsync(request, userId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "MemberLookUp Search failed");
                return StatusCode(500, new { message = "An unexpected error occurred." });
            }
        }

        /// <summary>
        /// Called when user clicks the "Sel" button on a row.
        /// GET /api/MemberLookUp/select/204
        /// Returns only that member's data to populate the parent form.
        /// </summary>
        [HttpGet("select/{memMemberRegistrationId:long}")]
        public async Task<ActionResult<MemberSelectedDto>> Select(long memMemberRegistrationId)
        {
            try
            {
                long userId = GetUserId();
                var member = await _repo.GetSelectedMemberAsync(memMemberRegistrationId, userId);

                if (member == null)
                    return NotFound(new { message = $"Member {memMemberRegistrationId} not found." });

                return Ok(member);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "MemberLookUp Select failed for id={Id}", memMemberRegistrationId);
                return StatusCode(500, new { message = "An unexpected error occurred." });
            }
        }

        // 🔧 Replace this with your actual userId from claims/session
        private static long GetUserId() => 0;
    }
}