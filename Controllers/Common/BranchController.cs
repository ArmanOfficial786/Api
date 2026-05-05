using JsSampleReport.Dtos.RequestDtos.Common;
using JsSampleReport.Inteface.ServiceInterface;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace JsSampleReport.Controllers.Common
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
                    response.IsValid = false;
                    response.StatusCode = 400;
                    response.Message = "No branches found";
                    response.Data = null;
                    return BadRequest(response);
                }

                //return Ok(new { success = true, data = branches });
                response.IsValid = true;
                response.StatusCode = 200;
                response.Message = "Success";
                response.Data = branches;
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching branches");
                return StatusCode(500, new GeneralResponse<string>
                {
                    IsValid = false,
                    StatusCode = 500,
                    Message = ex.Message,
                    Data = null
                });
            }
        }
    }
}
