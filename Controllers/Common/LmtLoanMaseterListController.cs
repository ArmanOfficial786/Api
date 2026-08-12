using Microsoft.AspNetCore.Mvc;
using NexgenCosysReport.Dtos.RequestDtos.Common;
using NexgenCosysReport.Inteface.ServiceInterface.Common;

namespace NexgenCosysReport.Controllers.Common
{
    [Route("api/[controller]")]
    [ApiController]
    public class LmtLoanMaseterListController : ControllerBase
    {
        private readonly ILmtLoanMasterList _lmtLoanMasterList;

        public LmtLoanMaseterListController(ILmtLoanMasterList lmtLoanMasterList)
        {
            _lmtLoanMasterList = lmtLoanMasterList;
        }

        [HttpGet]
        public async Task<ActionResult<GeneralResponse<List<LmtLoanMaseterListResponse>>>> GetAllLmtLoanMasterList()
        {
            var response = new GeneralResponse<List<LmtLoanMaseterListResponse>>();
            var loanMasters = await _lmtLoanMasterList.GetAllLmtLoanMasterList();
            response.isValid = true;
            response.statusCode = StatusCodes.Status200OK;
            response.data = loanMasters;
            return Ok(response);

        }
    }
}
