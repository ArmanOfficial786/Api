using Microsoft.AspNetCore.Mvc;
using NexgenCosysReport.Dtos.RequestDtos.Common;
using NexgenCosysReport.Inteface.ServiceInterface.Common;

namespace NexgenCosysReport.Controllers.Common
{
    [ApiController]
    [Route("api/[controller]")]
    public class AssetCategoryController : ControllerBase
    {
        private readonly IAssetCategory _assetCategoryService;
        private readonly ILogger<AssetCategoryController> _logger;

        public AssetCategoryController(
            IAssetCategory assetCategoryService,
            ILogger<AssetCategoryController> logger)
        {
            _assetCategoryService = assetCategoryService;
            _logger = logger;
        }

        [HttpGet("GetAllByType")]
        public async Task<ActionResult<GeneralResponse<List<AssetCategoryResponse>>>> GetAllByType(
            [FromQuery] char type)
        {
            try
            {
                var categories = await _assetCategoryService.GetAllByType(type);

                var response = new GeneralResponse<List<AssetCategoryResponse>>
                {
                    isValid = true,
                    statusCode = 200,
                    data = categories
                };

                return Ok(response);
            }
            catch (Exception ex)
            {

                return StatusCode(500, new GeneralResponse<List<AssetCategoryResponse>>
                {
                    isValid = false,
                    statusCode = 500,
                    message = "An error occurred while fetching asset categories"
                });
            }
        }
    }
}