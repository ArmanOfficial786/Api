using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NexgenCosysReport.Dtos.RequestDtos.Common;
using NexgenCosysReport.Inteface.ServiceInterface.Common;

namespace NexgenCosysReport.Controllers.Common
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class FamClearanceStatusLookupController : ControllerBase
    {
        private readonly IFamClearanceStatusLookup _repository;
        private readonly ILogger<FamClearanceStatusLookupController> _logger;

        public FamClearanceStatusLookupController(
            IFamClearanceStatusLookup repository,
            ILogger<FamClearanceStatusLookupController> logger)
        {
            _repository = repository;
            _logger = logger;
        }

        [HttpGet]
        public async Task<ActionResult<GeneralResponse<List<FamClearanceStatusLookupDto>>>> GetAll()
        {
            try
            {
                var data = await _repository.GetAllAsync();


                return Ok(new GeneralResponse<List<FamClearanceStatusLookupDto>>
                {
                    isValid = true,
                    statusCode = StatusCodes.Status200OK,
                    data = data
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching FamClearanceStatus lookup");

                return StatusCode(500, new GeneralResponse<List<FamClearanceStatusLookupDto>>
                {
                    isValid = false,
                    statusCode = StatusCodes.Status500InternalServerError,
                    message = "An unexpected error occurred.",
                    data = null
                });
            }
        }
    }
}