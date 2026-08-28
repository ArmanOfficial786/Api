using Microsoft.AspNetCore.Mvc;
using NexgenCosysReport.Dtos.RequestDtos.Common;
using NexgenCosysReport.Inteface.ServiceInterface.Common;

namespace NexgenCosysReport.Controllers.Common
{
    [ApiController]
    [Route("api/[controller]")]

    public class BranchController : ControllerBase
    {
        private readonly IBranch _branchService;
        private readonly ILogger<BranchController> _logger;

        public BranchController(IBranch branchService, ILogger<BranchController> logger)
        {
            _branchService = branchService;
            _logger = logger;
        }
        [HttpGet("GetAllBranches")]
        public async Task<ActionResult<GeneralResponse<List<BranchResponse>>>> GetAllBranches([FromQuery] long userId)
        {
            try
            {
                var response = new GeneralResponse<List<BranchResponse>>();
                long usmUserId = userId;
                var branches = await _branchService.GetByUserId(usmUserId);

                if (!branches.Any())
                {
                    response.isValid = false;
                    response.statusCode = 400;
                    response.message = "No branches found";
                    return BadRequest(response);
                }


                response.isValid = true;
                response.statusCode = 200;
                response.data = branches;
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching branches");
                return StatusCode(500, new GeneralResponse<string>
                {
                    isValid = false,
                    statusCode = 500,
                    message = ex.Message,

                });
            }
        }

        [HttpGet("GetCollectionBranch")]
        public async Task<ActionResult<GeneralResponse<List<BranchResponse>>>> GetUserBranches([FromQuery] long userId)
        {
            try
            {
                var branches = await _branchService.GetByUserId(userId);

                var response = new GeneralResponse<List<BranchResponse>>
                {
                    isValid = true,
                    statusCode = 200,
                    data = branches,

                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching user branches for userId: {UserId}", userId);
                return StatusCode(500, new GeneralResponse<List<BranchResponse>>
                {
                    isValid = false,
                    statusCode = 500,
                    message = "An error occurred while fetching branches"
                });
            }
        }
    }
}
