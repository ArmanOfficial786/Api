using JsSampleReport.Dtos.RequestDtos.Common;
using JsSampleReport.Inteface.ServiceInterface;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace JsSampleReport.Controllers.Common
{
    [Route("api/[controller]")]
    [ApiController]
    public class CollectionCenterController : ControllerBase
    {
        private readonly ICollectionCenter _collectionCenterService;

        public CollectionCenterController(ICollectionCenter collectionCenterService)
        {
            _collectionCenterService = collectionCenterService;
        }
        [HttpPost("collection-centers")]
        public async Task<ActionResult<List<CollectionCenterResponseDto>>> GetCollectionCenters(CollectionCenterRequestDtos request)
        {
            if (request.LstOfficeId <= 0)
                return BadRequest("Invalid office ID.");

            var result = await _collectionCenterService.GetCollectionCenters(request.LstOfficeId);
            return Ok(result);
        }
    }
}
