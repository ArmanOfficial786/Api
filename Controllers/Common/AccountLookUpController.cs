// Controllers/Common/AccountLookUpController.cs
using Microsoft.AspNetCore.Mvc;
using NexgenCosysReport.Dtos.RequestDtos.Common;
using NexgenCosysReport.Inteface.ServiceInterface.Common;
using NexgenCosysReport.Repository.Common;

namespace NexgenCosysReport.Controllers.Common
{
    [Route("api/[controller]")]
    [ApiController]
    public class AccountLookUpController : ControllerBase
    {
        private readonly IAccountLookUp _repo;
        private readonly ILogger<AccountLookUpController> _logger;

        public AccountLookUpController(
            IAccountLookUp repo,
            ILogger<AccountLookUpController> logger)
        {
            _repo = repo;
            _logger = logger;
        }

        /// <summary>
        /// POST /api/AccountLookUp/search
        /// body: { pageNumber, pageSize, params: [{ key, value, option }], sort: [{ field, order }] }
        /// </summary>
        [HttpPost("search")]
        public async Task<ActionResult<GeneralResponse<List<AccountLookUpDtos>>>> Search([FromBody] Filter? filter)
        {
            try
            {
                var userId = GetUserId();
                var pagination = await _repo.GetAccountListAsync(filter ?? new Filter(), userId);

                return Ok(new GeneralResponse<List<AccountLookUpDtos>>
                {
                    isValid = true,
                    statusCode = StatusCodes.Status200OK,
                    message = "",
                    data = pagination.Items,
                    pagination = new Pagination
                    {
                        Items = pagination.Items?.Cast<object>().ToList() ?? [],
                        currentPage = pagination.currentPage,
                        totalPages = pagination.totalPages,
                        pageSize = pagination.pageSize,
                        totalRecord = pagination.totalRecord,
                        hasNextPage = pagination.hasNextPage,
                        hasPreviousPage = pagination.hasPreviousPage
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AccountLookUp Search failed");
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new GeneralResponse<List<AccountLookUpDtos>>
                    {
                        isValid = false,
                        statusCode = StatusCodes.Status500InternalServerError,
                        message = "An unexpected error occurred.",
                        data = null
                    });
            }
        }

        [HttpGet("select/{mamAccountOpeningId:long}")]
        public async Task<ActionResult<GeneralResponse<AccountSelectedDto>>> Select(long mamAccountOpeningId)
        {
            try
            {
                var userId = GetUserId();
                var account = await _repo.GetSelectedAccountAsync(mamAccountOpeningId, userId);

                if (account == null)
                    return NotFound(new GeneralResponse<AccountSelectedDto>
                    {
                        isValid = false,
                        statusCode = StatusCodes.Status404NotFound,
                        message = $"Account {mamAccountOpeningId} not found or not authorized."
                    });

                return Ok(new GeneralResponse<AccountSelectedDto>
                {
                    isValid = true,
                    statusCode = StatusCodes.Status200OK,
                    data = account
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AccountLookUp Select failed for id={Id}", mamAccountOpeningId);
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new GeneralResponse<AccountSelectedDto>
                    {
                        isValid = false,
                        statusCode = StatusCodes.Status500InternalServerError,
                        message = "An unexpected error occurred."
                    });
            }
        }

        [HttpGet("validate/{accountNo}")]
        public async Task<ActionResult<GeneralResponse<AccountSelectedDto>>> Validate(string accountNo)
        {
            try
            {
                var userId = GetUserId();
                var result = await _repo.ValidateAccountNoAsync(accountNo, userId);

                if (!result.IsValid)
                    return BadRequest(new GeneralResponse<AccountSelectedDto>
                    {
                        isValid = false,
                        statusCode = StatusCodes.Status400BadRequest,
                        message = result.Message ?? "Validation failed."
                    });

                return Ok(new GeneralResponse<AccountSelectedDto>
                {
                    isValid = true,
                    statusCode = StatusCodes.Status200OK,
                    data = result.Account
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AccountLookUp Validate failed for accountNo={AccountNo}", accountNo);
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new GeneralResponse<AccountSelectedDto>
                    {
                        isValid = false,
                        statusCode = StatusCodes.Status500InternalServerError,
                        message = "An unexpected error occurred."
                    });
            }
        }

        // ⚠ Replace this with your actual userId from claims/session
        private static long GetUserId() => 0;
    }
}