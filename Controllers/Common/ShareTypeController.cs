using Microsoft.AspNetCore.Mvc;
using NexgenCosysReport.Dtos.RequestDtos.Common;
using NexgenCosysReport.Inteface.ServiceInterface.Common;

namespace NexgenCosysReport.Controllers.Common
{
    [Route("api/[controller]")]
    [ApiController]
    public class ShareTypeController : ControllerBase
    {
        private readonly IShareType _shareType;

        public ShareTypeController(IShareType shareType)
        {
            _shareType = shareType;
        }
        [HttpGet]
        public async Task<ActionResult<GeneralResponse<List<ShareTypeResponse>>>> GetAllShareType()
        {
            var response = new GeneralResponse<List<ShareTypeResponse>>();
            var shareTypes = await _shareType.GetAllLmtLoanMasterList();
            response.isValid = true;
            response.statusCode = StatusCodes.Status200OK;
            response.data = shareTypes;
            return Ok(response);
        }
    }
}
