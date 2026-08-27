// Controllers/Common/UserLookupController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NexgenCosysReport.Dtos.RequestDtos.Common;
using NexgenCosysReport.Inteface.ServiceInterface.Common;

namespace NexgenCosysReport.Controllers.Common
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class UserLookupController : ControllerBase
    {
        private readonly IUserLookUp _userLookupService;
        private readonly ITokenService _tokenService;
        private readonly ILogger<UserLookupController> _logger;

        public UserLookupController(
            IUserLookUp userLookupService,
            ITokenService tokenService,
            ILogger<UserLookupController> logger)
        {
            _userLookupService = userLookupService;
            _tokenService = tokenService;
            _logger = logger;
        }

        /// <summary>
        /// Active users the logged-in user can act as Entry By / Edited By on.
        /// Same data source for both dropdowns (legacy odsUser control) — the
        /// frontend calls this once per dropdown and keeps two separate
        /// option lists client-side.
        /// GET /api/UserLookup
        /// </summary>
        [HttpGet()]
        public async Task<ActionResult<List<UserLookupResponse>>> GetUsers()
        {
            try
            {
                var userId = _tokenService.GetUserIdFromPrincipal(User);
                if (userId == null)
                    return Unauthorized();

                var users = await _userLookupService.GetActiveUsersAsync(userId.Value);

                // ── Bare array, matching Teller/TellerExpenseList convention ──
                return Ok(users);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading Entry By / Edited By users");
                return StatusCode(500, "Error loading users");
            }
        }
    }
}