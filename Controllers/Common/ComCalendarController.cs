using Microsoft.AspNetCore.Mvc;
using NexgenCosysReport.Dtos.RequestDtos.Common;
using NexgenCosysReport.Inteface.ServiceInterface.Common;

namespace NexgenCosysReport.Controllers.Common
{

    [ApiController]
    [Route("api/[controller]")]
    public class CalendarController : ControllerBase
    {
        private readonly IComCalendar _repository;

        public CalendarController(IComCalendar repository)
        {
            _repository = repository;
        }

        // GET api/calendar/years
        [HttpGet("years")]
        public ActionResult<YearsResponseDto> GetYears()
        {
            return Ok(_repository.GetYears());
        }

        // GET api/calendar/days?year=2081&month=1
        [HttpGet("days")]
        public ActionResult<DaysResponseDto> GetDays([FromQuery] int year, [FromQuery] int month)
        {
            if (year <= 0 || month < 1 || month > 12)
                return BadRequest("Valid year and month (1–12) are required.");

            var result = _repository.GetDays(year, month);

            if (result.Days.Count == 0)
                return NotFound("No calendar data found for the given year/month.");

            return Ok(result);
        }

        // POST api/calendar/convert
        [HttpPost("convert")]
        public ActionResult<ConvertResponseDto> ConvertDate([FromBody] ConvertRequestDto request)
        {
            try
            {
                return Ok(_repository.ConvertDate(request.Direction, request.Date));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return NotFound(ex.Message);
            }
        }
    }
}
