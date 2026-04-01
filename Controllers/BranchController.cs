using JsSampleReport.Inteface.ServiceInterface;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace JsSampleReport.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
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
        public async Task<IActionResult> GetAllBranches()
        {
            try
            {
                var branches = await _branchService.GetAllBranches();

                if (!branches.Any())
                    return NotFound(new { success = false, message = "No branches found" });

                return Ok(new { success = true, data = branches });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching branches");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }
    }
}
