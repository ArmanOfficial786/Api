// Controllers/Common/VoucherController.cs
using Microsoft.AspNetCore.Mvc;
using NexgenCosysReport.Dtos.RequestDtos.Common;
using NexgenCosysReport.Inteface.ServiceInterface.Common;

namespace NexgenCosysReport.Controllers.Common
{
    [ApiController]
    [Route("api/[controller]")]
    public class VoucherController : ControllerBase
    {
        private readonly IVoucher _voucherRepository;
        private readonly ILogger<VoucherController> _logger;

        public VoucherController(IVoucher voucherRepository, ILogger<VoucherController> logger)
        {
            _voucherRepository = voucherRepository;
            _logger = logger;
        }

        // GET api/Voucher/list?fromDate=2079/01/01&toDate=2083/01/01&branchIds=2&branchIds=5
        [HttpPost("list")]
        public async Task<ActionResult<GeneralResponse<List<VoucherOptionResponse>>>> GetVoucherList(
            VoucherListRequest request,
            CancellationToken cancellationToken)
        {

            try
            {
                var result = await _voucherRepository.GetVoucherListAsync(request, cancellationToken);
                return Ok(result);
            }

            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in GetVoucherList");
                return StatusCode(500, new GeneralResponse<List<VoucherOptionResponse>>
                {
                    isValid = false,
                    statusCode = 500,
                    message = "An unexpected error occurred while fetching vouchers."
                });
            }
        }

        // GET api/Voucher/by-number?voucherNo=CR/2/2082/83
        [HttpGet("by-number")]
        public async Task<ActionResult<VoucherOptionResponse>> GetByVoucherNo(
            [FromQuery] string voucherNo,
            CancellationToken cancellationToken)
        {
            try
            {
                var result = await _voucherRepository.GetByVoucherNoAsync(voucherNo, cancellationToken);
                if (result is null)
                    return NotFound(new { message = "Voucher not found." });

                return Ok(result);
            }

            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in GetByVoucherNo for {VoucherNo}", voucherNo);
                return StatusCode(500, new { message = "An unexpected error occurred while fetching the voucher." });
            }
        }
    }
}