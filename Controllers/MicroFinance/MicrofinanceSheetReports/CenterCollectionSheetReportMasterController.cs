using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NexgenCosysReport.Dtos.ReportDtos;
using NexgenCosysReport.Dtos.RequestDtos.Microfinance.MicrofinanceSheetReports;
using NexgenCosysReport.Inteface.ReportInterface;
using NexgenCosysReport.Inteface.ServiceInterface.Microfinance.MicrofinanceSheetReports;
using NexgenCosysReport.Utils.Report;
using System.Security.Claims;

namespace NexgenCosysReport.Controllers.Microfinance.MicrofinanceSheetReports
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class CenterCollectionSheetReportMasterController : ControllerBase
    {
        private const string ReportName = "CenterCollectionSheetReportMaster";

        // bumped by every successful Update, so cached report HTML for the old list is never served again
        private static long _dataVersion;

        private readonly ICenterCollectionSheetReportMasterRepository _repository;
        private readonly IReportHeaderProvider _headers;
        private readonly IJsReportService _jsReportService;
        private readonly ILogger<CenterCollectionSheetReportMasterController> _logger;
        private readonly IReportFileResponse _reportFileResponse;

        private static readonly PageSizeSetting NPageSetting = PageSizeSetting.A4Portrait;

        public CenterCollectionSheetReportMasterController(
            ICenterCollectionSheetReportMasterRepository repository,
            IReportHeaderProvider headers,
            IJsReportService jsReportService,
            ILogger<CenterCollectionSheetReportMasterController> logger,
            IReportFileResponse reportFileResponse)
        {
            _repository = repository;
            _headers = headers;
            _jsReportService = jsReportService;
            _logger = logger;
            _reportFileResponse = reportFileResponse;
        }

        private bool TryGetUserId(out long userId)
        {
            userId = 0;
            var userIdClaim = User.FindFirst("UserId")?.Value
                              ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return !string.IsNullOrEmpty(userIdClaim) && long.TryParse(userIdClaim, out userId);
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            try
            {
                if (!TryGetUserId(out _))
                    return Unauthorized(new { success = false, StatusCode = 401, message = "Unauthorized" });

                var data = await _repository.GetAllAsync();
                return Ok(new { success = true, StatusCode = 200, data });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetAll (CenterCollectionSheetReportMaster)");
                return StatusCode(500, new { success = false, StatusCode = 500, message = ex.Message });
            }
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            try
            {
                if (!TryGetUserId(out _))
                    return Unauthorized(new { success = false, StatusCode = 401, message = "Unauthorized" });

                var data = await _repository.GetByIdAsync(id);
                if (data == null)
                    return NotFound(new { success = false, StatusCode = 404, message = "Scheme not found" });

                return Ok(new { success = true, StatusCode = 200, data });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { success = false, StatusCode = 400, message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetById (CenterCollectionSheetReportMaster)");
                return StatusCode(500, new { success = false, StatusCode = 500, message = ex.Message });
            }
        }

        [HttpPut]
        public async Task<IActionResult> Update([FromBody] CenterCollectionSheetReportMasterUpdateDto dto)
        {
            try
            {
                if (!TryGetUserId(out _))
                    return Unauthorized(new { success = false, StatusCode = 401, message = "Unauthorized" });

                if (dto == null || !ModelState.IsValid)
                    return BadRequest(new { success = false, StatusCode = 400, message = "Invalid request" });

                if (dto.SycCollectionCenterScemeId <= 0)
                    return BadRequest(new { success = false, StatusCode = 400, message = "Select scheme." });

                var result = await _repository.UpdateAsync(dto);
                if (!result.Success)
                    return BadRequest(new { success = false, StatusCode = 400, message = result.Message });

                Interlocked.Increment(ref _dataVersion);

                return Ok(new { success = true, StatusCode = 200, message = result.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Update (CenterCollectionSheetReportMaster)");
                return StatusCode(500, new { success = false, StatusCode = 500, message = ex.Message });
            }
        }

        // GET api/CenterCollectionSheetReportMaster/report?format=VIEW&canView=true&canModify=true
        [HttpGet("report")]
        public async Task<IActionResult> GenerateReport(
            [FromQuery] string format = "VIEW",
            [FromQuery] bool canView = false,
            [FromQuery] bool canModify = false,
            CancellationToken ct = default)
        {
            try
            {
                if (!TryGetUserId(out _))
                    return Unauthorized(new { success = false, StatusCode = 401, message = "Unauthorized" });

                var upperFormat = format.ToUpperInvariant();
                var page = NPageSetting;

                // the list is editable, so the key carries a version; the link columns change the markup, so they are part of it too
                var reportKey = $"{ReportName}_v{Interlocked.Read(ref _dataVersion)}" +
                                $"_V{(canView ? 1 : 0)}M{(canModify ? 1 : 0)}_{page.CacheTag}";

                var cached = await ReportExportHelper.TryServeAsync(
                    reportKey, upperFormat, ReportName, pdfView: false,
                    _jsReportService, _reportFileResponse, _logger, page, ct);
                if (cached != null) return cached;

                var headerTask = _headers.GetAsync((string?)null, ct);
                var schemesTask = _repository.GetAllAsync();
                await Task.WhenAll(headerTask, schemesTask);

                var schemes = (await schemesTask).ToList();

                var reportData = new Dictionary<string, object>
                {
                    { "Schemes", schemes },
                    { "TotalRecords", schemes.Count },
                    { "CanView", canView },
                    { "CanModify", canModify },
                    { "HeaderDataSet", await headerTask },
                    { "Format", upperFormat },
                    { "PageSetting", page }
                };

                var html = await _jsReportService.RenderRazorToHtmlAndCacheAsync(
                    reportKey: reportKey,
                    reportPath: "Views/Report/Microfinance/MicrofinanceSheetReports/CenterCollectionSheetReportMaster.cshtml",
                    data: reportData, ct: ct);

                return await ReportExportHelper.CompleteAsync(
                    html, reportKey, upperFormat, ReportName, pdfView: false,
                    totalRecords: schemes.Count,
                    _jsReportService, _reportFileResponse, _logger, page, ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating {Report} report", ReportName);
                return StatusCode(500, new { message = ex.Message, inner = ex.InnerException?.Message });
            }
        }
    }
}