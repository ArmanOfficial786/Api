// Controllers/AccountOperation/DepositStatementVerifyController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NexgenCosysReport.Dtos.RequestDtos.Common;
using NexgenCosysReport.Inteface.ServiceInterface.Common;

namespace NexgenCosysReport.Controllers.Common
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class DepositStatementVerifyController : ControllerBase
    {
        private readonly IDepositStatementVerification _repository;
        private readonly ILogger<DepositStatementVerifyController> _logger;
        private readonly IDateConverterService _dateConverter;

        public DepositStatementVerifyController(
            IDepositStatementVerification repository,
            ILogger<DepositStatementVerifyController> logger,
            IDateConverterService dateConverter)
        {
            _repository = repository;
            _logger = logger;
            _dateConverter = dateConverter;
        }

        /// <summary>
        /// Get verification status for an account
        /// </summary>
        [HttpGet("Status/{mamAccountOpeningId:long}")]
        public async Task<ActionResult<GeneralResponse<VerificationStatusDto>>> GetVerificationStatus(long mamAccountOpeningId)
        {
            try
            {
                var result = await _repository.GetVerificationStatus(mamAccountOpeningId);

                return Ok(new GeneralResponse<VerificationStatusDto>
                {
                    isValid = true,
                    statusCode = StatusCodes.Status200OK,
                    message = "Verification status retrieved successfully",
                    data = result
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting verification status for account: {MamAccountOpeningId}", mamAccountOpeningId);
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new GeneralResponse<VerificationStatusDto>
                    {
                        isValid = false,
                        statusCode = StatusCodes.Status500InternalServerError,
                        message = ex.Message
                    });
            }
        }

        /// <summary>
        /// Get verification history for an account
        /// </summary>
        [HttpGet("History/{mamAccountOpeningId:long}")]
        public async Task<ActionResult<GeneralResponse<List<DepositStatementVerificationDto>>>> GetVerificationHistory(long mamAccountOpeningId)
        {
            try
            {
                var result = await _repository.GetVerificationHistory(mamAccountOpeningId);

                return Ok(new GeneralResponse<List<DepositStatementVerificationDto>>
                {
                    isValid = true,
                    statusCode = StatusCodes.Status200OK,
                    message = "Verification history retrieved successfully",
                    data = result
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting verification history for account: {MamAccountOpeningId}", mamAccountOpeningId);
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new GeneralResponse<List<DepositStatementVerificationDto>>
                    {
                        isValid = false,
                        statusCode = StatusCodes.Status500InternalServerError,
                        message = ex.Message
                    });
            }
        }

        /// <summary>
        /// Verify deposit statement (matches btnStatementVerify_Click in WebForm)
        /// </summary>
        [HttpPost("Verify")]
        public async Task<ActionResult<GeneralResponse<VerificationStatusDto>>> VerifyStatement(
            [FromBody] DepositStatementVerifyRequestDto request)
        {
            try
            {
                // Validate request
                if (request.MamAccountOpeningId <= 0)
                {
                    return BadRequest(new GeneralResponse<VerificationStatusDto>
                    {
                        isValid = false,
                        statusCode = StatusCodes.Status400BadRequest,
                        message = "Account ID is required"
                    });
                }

                if (string.IsNullOrEmpty(request.AccountNo))
                {
                    return BadRequest(new GeneralResponse<VerificationStatusDto>
                    {
                        isValid = false,
                        statusCode = StatusCodes.Status400BadRequest,
                        message = "Account Number is required"
                    });
                }

                if (string.IsNullOrEmpty(request.VerifiedToDateOnBs) || request.VerifiedToDateOnBs == "-1")
                {
                    return BadRequest(new GeneralResponse<VerificationStatusDto>
                    {
                        isValid = false,
                        statusCode = StatusCodes.Status400BadRequest,
                        message = "Verification date is required"
                    });
                }

                // Get user ID from token
                //var userId = GetCurrentUserId();
                var userId = 160;
                if (userId == -1)
                {
                    return Unauthorized(new GeneralResponse<VerificationStatusDto>
                    {
                        isValid = false,
                        statusCode = StatusCodes.Status401Unauthorized,
                        message = "User not authenticated"
                    });
                }

                // Check if account exists
                var accountInfo = await _repository.GetAccountInfo(request.AccountNo);
                if (accountInfo == null)
                {
                    return NotFound(new GeneralResponse<VerificationStatusDto>
                    {
                        isValid = false,
                        statusCode = StatusCodes.Status404NotFound,
                        message = "Account not found"
                    });
                }

                // Check if verification date is in future - matches WebForm
                var verifiedDate = await _dateConverter.NepaliToEnglishAsync(request.VerifiedToDateOnBs);
                if (verifiedDate > DateTime.Now.Date)
                {
                    return BadRequest(new GeneralResponse<VerificationStatusDto>
                    {
                        isValid = false,
                        statusCode = StatusCodes.Status400BadRequest,
                        message = $"Verification date cannot be in the future: {request.VerifiedToDateOnBs}"
                    });
                }

                // Perform verification
                var (success, message) = await _repository.CreateVerification(request, userId);

                if (!success)
                {
                    return BadRequest(new GeneralResponse<VerificationStatusDto>
                    {
                        isValid = false,
                        statusCode = StatusCodes.Status400BadRequest,
                        message = message
                    });
                }

                // Get updated verification status
                var status = await _repository.GetVerificationStatus(request.MamAccountOpeningId);

                return Ok(new GeneralResponse<VerificationStatusDto>
                {
                    isValid = true,
                    statusCode = StatusCodes.Status200OK,
                    message = message,
                    data = new VerificationStatusDto
                    {

                        VerifiedTillBs = status?.VerifiedTillBs,
                        HasVerification = status?.HasVerification ?? false,
                        VerifiedDateBs = status?.VerifiedDateBs,
                        VerifiedBy = status?.VerifiedBy,
                        Message = status?.Message

                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "DepositStatement verification failed");
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new GeneralResponse<VerificationStatusDto>
                    {
                        isValid = false,
                        statusCode = StatusCodes.Status500InternalServerError,
                        message = ex.Message
                    });
            }
        }

        private long GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst("UserId")?.Value;
            if (string.IsNullOrEmpty(userIdClaim))
            {
                var userIdHeader = Request.Headers["X-UserId"].FirstOrDefault();
                if (!string.IsNullOrEmpty(userIdHeader) && long.TryParse(userIdHeader, out var id))
                {
                    return id;
                }
                return -1;
            }
            return long.Parse(userIdClaim);
        }
    }
}