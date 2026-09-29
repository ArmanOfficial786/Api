// Controllers/Common/LoanAccountLookUpController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NexgenCosysReport.Dtos.RequestDtos.Common;
using NexgenCosysReport.Inteface.ServiceInterface.Common;
using System.Security.Claims;

namespace NexgenCosysReport.Controllers.Common
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class LoanAccountLookUpController : ControllerBase
    {
        private readonly ILoanAccountLookUp _repo;
        private readonly ILogger<LoanAccountLookUpController> _logger;
        private readonly IWebHostEnvironment _env;

        public LoanAccountLookUpController(
            ILoanAccountLookUp repo,
            ILogger<LoanAccountLookUpController> logger,
            IWebHostEnvironment env)
        {
            _repo = repo;
            _logger = logger;
            _env = env;
        }

        /// <summary>
        /// Loads the grid, 10 records per page.
        /// GET /api/LoanAccountLookUp/search?Page=1
        /// </summary>
        [HttpGet("search")]
        public async Task<ActionResult<Pagination<LoanAccountLookUpDtos>>> Search(
            [FromQuery] LoanAccountLookUpRequest request)
        {
            try
            {
                long userId = GetUserId();
                var result = await _repo.GetLoanAccountListAsync(request, userId);
                return Ok(result);
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "LoanAccountLookUp Search: unauthorized");
                return Unauthorized(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "LoanAccountLookUp Search failed");
                return StatusCode(
                    StatusCodes.Status500InternalServerError,
                    new { message = ErrorMessage(ex) });
            }
        }

        /// <summary>
        /// Called when user clicks the "Sel" button on a row.
        /// GET /api/LoanAccountLookUp/select/204
        /// </summary>
        [HttpGet("select/{loanIssueId:long}")]
        public async Task<ActionResult<LoanAccountSelectedDto>> Select(long loanIssueId)
        {
            try
            {
                long userId = GetUserId();
                var account = await _repo.GetSelectedLoanAccountAsync(loanIssueId, userId);

                if (account == null)
                    return NotFound(new { message = $"Loan account {loanIssueId} not found." });

                return Ok(account);
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "LoanAccountLookUp Select: unauthorized");
                return Unauthorized(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "LoanAccountLookUp Select failed for id={Id}", loanIssueId);
                return StatusCode(
                    StatusCodes.Status500InternalServerError,
                    new { message = ErrorMessage(ex) });
            }
        }

        // Real message only in Development; generic text in other environments
        private string ErrorMessage(Exception ex) =>
            _env.IsDevelopment() ? ex.Message : "An unexpected error occurred.";

        // -- Extract userId from JWT claims ------------------------------------
        private long GetUserId()
        {
            var userIdClaim =
                User.FindFirst("UserId")?.Value
                ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrWhiteSpace(userIdClaim) || !long.TryParse(userIdClaim, out var userId))
                throw new UnauthorizedAccessException("UserId claim is missing or invalid.");

            return userId;
        }
    }
}