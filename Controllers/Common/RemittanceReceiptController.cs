using Microsoft.AspNetCore.Mvc;
using NexgenCosysReport.Dtos.RequestDtos.Common;
using NexgenCosysReport.Inteface.ServiceInterface.Common;

namespace NexgenCosysReport.Controllers.Common
{
    [ApiController]
    [Route("api/[controller]")]
    public class RemittanceReceiptController : ControllerBase
    {
        private const string DateFormat = "yyyy-MM-dd";

        private readonly IRemittanceReceipt _remittanceReceiptService;
        private readonly ILogger<RemittanceReceiptController> _logger;
        private readonly IDateConverterService _dateConverter;

        public RemittanceReceiptController(
            IRemittanceReceipt remittanceReceiptService,
            ILogger<RemittanceReceiptController> logger,
            IDateConverterService dateConverter)
        {
            _remittanceReceiptService = remittanceReceiptService;
            _logger = logger;
            _dateConverter = dateConverter;
        }

        [HttpGet("GetRemittanceReceipts")]
        public async Task<ActionResult<GeneralResponse<List<RemittanceReceiptResponse>>>> GetRemittanceReceipts(
            [FromQuery] bool? isReceived,
            [FromQuery] long? createdBy,
            [FromQuery] string? fromDateBs,   // Nepali (BS) date, e.g. 2071/05/22
            [FromQuery] string? toDateBs,     // Nepali (BS) date, e.g. 2071/05/30
            [FromQuery] List<long>? officeIds)
        {
            try
            {
                DateTime? fromDate = null;
                DateTime? toDate = null;

                // Empty or "-1" (the old webform convention) means "no date filter".
                if (!string.IsNullOrWhiteSpace(fromDateBs) && fromDateBs != "-1")
                    fromDate = await _dateConverter.NepaliToEnglishAsync(fromDateBs);

                if (!string.IsNullOrWhiteSpace(toDateBs) && toDateBs != "-1")
                    toDate = await _dateConverter.NepaliToEnglishAsync(toDateBs);

                if (fromDate.HasValue && toDate.HasValue && fromDate > toDate)
                {
                    return BadRequest(new GeneralResponse<List<RemittanceReceiptResponse>>
                    {
                        isValid = false,
                        statusCode = 400,
                        message = "From date cannot be after to date"
                    });
                }

                var data = await _remittanceReceiptService.GetRemittanceReceipts(new RemittanceReceiptRequest
                {
                    IsReceived = isReceived,
                    CreatedBy = createdBy,
                    FromDate = fromDate?.ToString(DateFormat),
                    ToDate = toDate?.ToString(DateFormat),
                    OfficeIds = officeIds
                });

                return Ok(new GeneralResponse<List<RemittanceReceiptResponse>>
                {
                    isValid = true,
                    statusCode = 200,
                    data = data
                });
            }
            catch (ArgumentException ex)
            {
                // Invalid date format (from the repository) or an unparseable BS date
                return BadRequest(new GeneralResponse<List<RemittanceReceiptResponse>>
                {
                    isValid = false,
                    statusCode = 400,
                    message = ex.Message
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching remittance receipts");
                return StatusCode(500, new GeneralResponse<List<RemittanceReceiptResponse>>
                {
                    isValid = false,
                    statusCode = 500,
                    message = "An error occurred while fetching remittance receipts"
                });
            }
        }
    }
}