using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NexgenCosysReport.Dtos.ReportDtos;
using NexgenCosysReport.Dtos.RequestDtos.Microfinance.MicrofinanceSheetReports;
using NexgenCosysReport.Inteface.ReportInterface;
using NexgenCosysReport.Inteface.ServiceInterface.Microfinance.MicrofinanceSheetReports;
using NexgenCosysReport.Utils.Caching;
using NexgenCosysReport.Utils.Enum;
using NexgenCosysReport.Utils.Report;
using System.Security.Claims;

namespace NexgenCosysReport.Controllers.Microfinance.MicrofinanceSheetReports
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class CenterCollectionSheetAccountWiseController : ControllerBase
    {
        private const string ReportName = "CenterCollectionSheetAccountWise";

        private readonly ICenterCollectionSheetAccountWiseRepository _repository;
        private readonly IReportHeaderProvider _headers;
        private readonly IJsReportService _jsReportService;
        private readonly IReportDataCache _dataCache;
        private readonly ILogger<CenterCollectionSheetAccountWiseController> _logger;
        private readonly IReportFileResponse _reportFileResponse;

        // normal: A4 landscape (10 columns); visual: same page as every visual report
        private static readonly PageSizeSetting NPageSetting =
            PageSizeSetting.Custom(297, 210, PageUnit.mm, landscape: true);
        private static readonly PageSizeSetting VPageSetting =
            PageSizeSetting.Custom(300, 297, PageUnit.mm, landscape: true);

        public CenterCollectionSheetAccountWiseController(
            ICenterCollectionSheetAccountWiseRepository repository,
            IReportHeaderProvider headers,
            IJsReportService jsReportService,
            IReportDataCache dataCache,
            ILogger<CenterCollectionSheetAccountWiseController> logger,
            IReportFileResponse reportFileResponse)
        {
            _repository = repository;
            _headers = headers;
            _jsReportService = jsReportService;
            _dataCache = dataCache;
            _logger = logger;
            _reportFileResponse = reportFileResponse;
        }

        [HttpPost]
        public async Task<IActionResult> GenerateReport(
            [FromBody] CenterCollectionSheetAccountWiseRequestDto request,
            [FromQuery] string format = "VIEW",
            CancellationToken ct = default)
        {
            try
            {
                var userIdClaim = User.FindFirst("UserId")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !long.TryParse(userIdClaim, out _))
                    return NotFound(new { success = false, StatusCode = 401, message = "Unauthorized" });

                if (request == null || !ModelState.IsValid)
                    return BadRequest(new { success = false, StatusCode = 400, message = "Invalid request" });

                var upperFormat = format.ToUpperInvariant();
                var isVisual = request.VisualReport;
                var page = isVisual ? VPageSetting : NPageSetting;

                // the DB data is cached once and shared by the normal and the visual view
                var dataKey = ReportCacheKey.ForData(
                    request, ReportName, userIdClaim, nameof(request.VisualReport));
                var reportKey = $"{dataKey}_{(isVisual ? "VISUAL" : "NORMAL")}_{page.CacheTag}";

                var cached = await ReportExportHelper.TryServeAsync(
                    reportKey, upperFormat, ReportName, pdfView: false,
                    _jsReportService, _reportFileResponse, _logger, page, ct);
                if (cached != null) return cached;

                // this report always uses the default company header (empty branch id, as before)
                var headerTask = _headers.GetAsync("", ct);
                var dataTask = _dataCache.GetOrLoadAsync(dataKey, async () =>
                {
                    var loaded = await _repository.GetReportDataAsync(request);
                    if (loaded == null || loaded.Members == null || !loaded.Members.Any())
                        return null;
                    return loaded;
                }, ct);
                await Task.WhenAll(headerTask, dataTask);

                var data = await dataTask;
                if (data == null)
                    return NotFound(new { success = false, StatusCode = 400, message = "No data found" });

                var reportData = new Dictionary<string, object>
                {
                    { "Members", data.Members },
                    { "Receivables", data.Receivables },
                    { "SavingSummary", data.SavingSummary },
                    { "LoanSummary", data.LoanSummary },
                    { "Evaluation", data.Evaluation ?? new CenterCollectionSheetAccountEvaluationDto() },
                    { "TotalMembers", data.TotalMembers },
                    { "TotalSaving1", data.TotalSaving1 },
                    { "TotalSaving2", data.TotalSaving2 },
                    { "TotalSaving3", data.TotalSaving3 },
                    { "TotalSaving4", data.TotalSaving4 },
                    { "TotalSaving5", data.TotalSaving5 },
                    { "TotalSaving6", data.TotalSaving6 },
                    { "TotalShare", data.TotalShare },
                    { "TotalLoan1", data.TotalLoan1 },
                    { "TotalLoan2", data.TotalLoan2 },
                    { "TotalLoan3", data.TotalLoan3 },
                    { "TotalLoan4", data.TotalLoan4 },
                    { "TotalLoan5", data.TotalLoan5 },
                    { "TotalLoan6", data.TotalLoan6 },
                    { "TotalReceivable", data.TotalReceivable },
                    { "HeaderDataSet", await headerTask },
                    { "TillDate", data.TillDateBs ?? "" },
                    { "TillDateAd", data.TillDateAd ?? "" },
                    { "CollectionCenterName", data.CollectionCenterName ?? "" },
                    { "Saving1SchemeName", data.Saving1SchemeName ?? "" },
                    { "Loan1SchemeName", data.Loan1SchemeName ?? "" },
                    { "Loan2SchemeName", data.Loan2SchemeName ?? "" },
                    { "Loan3SchemeName", data.Loan3SchemeName ?? "" },
                    { "AgentName", data.AgentName ?? "-" },
                    { "MeetingDateBs", data.MeetingDateBs ?? "-" },
                    { "ChangedDateBs", data.ChangedDateBs ?? "-" },
                    { "NextMeetingDateBs", data.NextMeetingDateBs ?? "-" },
                    { "Format", upperFormat },
                    { "PageSetting", page }
                };

                // NOTE: the visual view below must exist before a request with VisualReport = true is made
                var viewPath = isVisual
                    ? "Views/VisualReport/Microfinance/VCenterCollectionSheetAccountWiseReport.cshtml"
                    : "Views/Report/Microfinance/MicrofinanceSheetReports/CenterCollectionSheetAccountWiseReport.cshtml";

                var html = await _jsReportService.RenderRazorToHtmlAndCacheAsync(
                    reportKey: reportKey, reportPath: viewPath, data: reportData, ct: ct);

                return await ReportExportHelper.CompleteAsync(
                    html, reportKey, upperFormat, ReportName, pdfView: false,
                    totalRecords: data.Members.Count,
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