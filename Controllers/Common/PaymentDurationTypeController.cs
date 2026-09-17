// Controllers/Common/PaymentDurationTypeController.cs
using Microsoft.AspNetCore.Mvc;
using NexgenCosysReport.Dtos.RequestDtos.Common;
using NexgenCosysReport.Inteface.ServiceInterface.Common;

namespace NexgenCosysReport.Controllers.Common
{
    [ApiController]
    [Route("api/[controller]")]
    //[Authorize]
    public class PaymentDurationTypeController : ControllerBase
    {
        private readonly IPaymentDurationType _paymentDurationTypeService;
        private readonly ILogger<PaymentDurationTypeController> _logger;

        public PaymentDurationTypeController(IPaymentDurationType paymentDurationTypeService, ILogger<PaymentDurationTypeController> logger)
        {
            _paymentDurationTypeService = paymentDurationTypeService;
            _logger = logger;
        }

        [HttpGet()]
        public async Task<ActionResult<List<PaymentDurationTypeResponse>>> GetAll()
        {
            try
            {
                var paymentDurationTypes = await _paymentDurationTypeService.GetAllAsync();
                return Ok(paymentDurationTypes);
            }
            catch (Exception ex)
            {
                throw new Exception("Error occurred while fetching payment duration types.", ex);
            }
        }
    }
}