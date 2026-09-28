using Microsoft.AspNetCore.Mvc;
using NexgenCosysReport.Dtos.RequestDtos.Common;
using NexgenCosysReport.Inteface.ServiceInterface.Common;

namespace NexgenCosysReport.Controllers.Common
{
    [ApiController]
    [Route("api/[controller]")]
    //[Authorize]
    public class FamAssetLookupController : ControllerBase
    {
        private readonly IFamAssetNameLookup _repository;
        private readonly ILogger<FamAssetLookupController> _logger;

        public FamAssetLookupController(
            IFamAssetNameLookup repository,
            ILogger<FamAssetLookupController> logger)
        {
            _repository = repository;
            _logger = logger;
        }

        [HttpGet("GetById")]
        public async Task<ActionResult<GeneralResponse<FamAssetNameLookupDto>>> GetById(
            [FromQuery] long famFixedAssetsDetailId)
        {
            try
            {
                var data = await _repository.GetByIdAsync(
                    famFixedAssetsDetailId);

                if (data == null)
                {
                    return NotFound(new GeneralResponse<FamAssetNameLookupDto>
                    {
                        isValid = false,
                        statusCode = StatusCodes.Status404NotFound,
                        message = "Asset not found.",
                        data = null
                    });
                }

                return Ok(new GeneralResponse<FamAssetNameLookupDto>
                {
                    isValid = true,
                    statusCode = StatusCodes.Status200OK,
                    data = data
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error fetching FamAsset detail for ID: {FamFixedAssetsDetailId}",
                    famFixedAssetsDetailId);

                return StatusCode(
                    StatusCodes.Status500InternalServerError,
                    new GeneralResponse<FamAssetNameLookupDto>
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