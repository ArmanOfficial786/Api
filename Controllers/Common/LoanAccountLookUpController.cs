// Controllers/Common/LoanAccountLookUpController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NexgenCosysReport.Dtos.RequestDtos.Common;
using NexgenCosysReport.Inteface.ServiceInterface.Common;

namespace NexgenCosysReport.Controllers.Common
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class LoanAccountLookUpController : ControllerBase
    {
        private readonly ILoanAccountLookUp _repo;
        private readonly ITokenService _tokenService;
        private readonly ILogger<LoanAccountLookUpController> _logger;
        private readonly IWebHostEnvironment _env;

        public LoanAccountLookUpController(
            ILoanAccountLookUp repo,
            ILogger<LoanAccountLookUpController> logger,
            IWebHostEnvironment env,
            ITokenService tokenService)
        {
            _repo = repo;
            _logger = logger;
            _env = env;
            _tokenService = tokenService;
        }

        /// <summary>
        /// Loads the grid, 10 records per page.
        /// GET /api/LoanAccountLookUp/search?Page=1
        /// </summary>
        [Authorize]
        [HttpGet("search")]
        public async Task<ActionResult<Pagination<LoanAccountLookUpDtos>>> Search(
            [FromQuery] LoanAccountLookUpRequest request)
        {
            try
            {
                if (_tokenService.GetUserIdFromPrincipal(User) is not long userId)
                    return Unauthorized(new { message = "User not authenticated." });
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
        [Authorize]
        [HttpGet("select/{loanIssueId:long}")]
        public async Task<ActionResult<LoanAccountSelectedDto>> Select(long loanIssueId)
        {
            try
            {
                if (_tokenService.GetUserIdFromPrincipal(User) is not long userId)
                    return Unauthorized(new { message = "User not authenticated." });
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


    }
}