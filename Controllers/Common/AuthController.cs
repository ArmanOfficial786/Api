using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NexgenCosysReport.Dtos.RequestDtos.Common;
using NexgenCosysReport.Inteface.ServiceInterface.Common;

namespace NexgenCosysReport.Controllers.Common
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuth _authRepository;

        public AuthController(IAuth authRepository)
        {
            _authRepository = authRepository;
        }

        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<ActionResult<LoginResponse>> Login(
            [FromBody] LoginRequest request,
            CancellationToken cancellationToken)
        {
            try
            {
                var response = await _authRepository.LoginAsync(request, cancellationToken);
                return Ok(response);
            }
            catch (Exception ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
        }

        // Add to AuthController
        [HttpPost("logout")]
        [Authorize]
        public async Task<IActionResult> Logout(CancellationToken cancellationToken)
        {
            var userIdClaim = User.FindFirst("UserId")?.Value;
            if (userIdClaim is null || !long.TryParse(userIdClaim, out var userId))
                return Unauthorized();

            await _authRepository.LogoutAsync(userId, cancellationToken);
            return Ok(new { message = "Logged out successfully." });
        }
    }
}