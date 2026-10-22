using Microsoft.AspNetCore.Mvc;
using NexgenCosysReport.Dtos.ReportDtos;
using NexgenCosysReport.Dtos.RequestDtos.Account.SubLedgerDetailsReport;
using NexgenCosysReport.Inteface.ReportInterface;
using NexgenCosysReport.Inteface.ServiceInterface.Account.SubLedgerDetailsReport;
using NexgenCosysReport.Utils.Caching;
using NexgenCosysReport.Utils.Enum;
using NexgenCosysReport.Utils.Report;
using System.Security.Claims;

namespace NexgenCosysReport.Controllers.Account.MainLedgerReport
{
    [ApiController]
    [Route("api/[controller]")]
    public class LedgerDetailsController : ControllerBase
    {
        private const string ReportName = "SubLedgerDetails";

        private readonly ISubLedgerDetailsRepository _repository;
        private readonly IReportHeaderProvider _headers;
        private readonly IJsReportService _jsReportService;
        private readonly IReportDataCache _dataCache;
        private readonly ILogger<LedgerDetailsController> _logger;
        private readonly IReportFileResponse _reportFileResponse;

        private static readonly PageSizeSetting NPageSetting = PageSizeSetting.A4Portrait;
        private static readonly PageSizeSetting VPageSetting =
            PageSizeSetting.Custom(300, 297, PageUnit.mm, landscape: true);

        public LedgerDetailsController(
            ISubLedgerDetailsRepository repository,
            IReportHeaderProvider headers,
            IJsReportService jsReportService,
            IReportDataCache dataCache,
            ILogger<LedgerDetailsController> logger,
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
            [FromBody] SubLedgerDetailsRequestDto request,
            [FromQuery] string format = "VIEW",
            CancellationToken ct = default)
        {
            try
            {
                if (request == null || !ModelState.IsValid || request.LedgerHead == null || request.LedgerHead.Count == 0)
                    return BadRequest(new { success = false, StatusCode = 400, message = "Invalid request" });

                var userIdClaim = User.FindFirst("UserId")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "";

                var upperFormat = format.ToUpperInvariant();
                var isVisual = request.VisualReport;
                var page = isVisual ? VPageSetting : NPageSetting;

                var dataKey = ReportCacheKey.ForData(
                    request, ReportName, userIdClaim, nameof(request.VisualReport));
                var reportKey = $"{dataKey}_{(isVisual ? "VISUAL" : "NORMAL")}_{page.CacheTag}";

                var cached = await ReportExportHelper.TryServeAsync(
                    reportKey, upperFormat, ReportName, pdfView: request.VisualReport,
                    _jsReportService, _reportFileResponse, _logger, page, ct);
                if (cached != null) return cached;

                var branchIdForHeader = "";
                if (!string.IsNullOrEmpty(request.BranchId) &&
                    request.BranchId != "-1" &&
                    request.BranchId != "string" &&
                    long.TryParse(request.BranchId, out _))
                {
                    branchIdForHeader = request.BranchId;
                }

                var headerTask = _headers.GetAsync(branchIdForHeader, ct);
                var dataTask = _dataCache.GetOrLoadAsync(dataKey, async () =>
                {
                    var loaded = await _repository.GetReportDataAsync(request);
                    if (loaded == null || loaded.Rows == null || !loaded.Rows.Any())
                        return null;
                    return loaded;
                }, ct);
                await Task.WhenAll(headerTask, dataTask);

                var data = await dataTask;
                if (data == null)
                    return NotFound(new { success = false, StatusCode = 400, message = "No data found" });

                var reportData = new Dictionary<string, object>
                {
                    { "Rows", data.Rows },
                    { "TotalRecords", data.TotalRecords },
                    { "TotalDebitAmount", data.TotalDebitAmount },
                    { "TotalCreditAmount", data.TotalCreditAmount },
                    { "OpeningBalance", data.OpeningBalance },
                    { "ClosingBalance", data.ClosingBalance },
                    { "AccountType", data.AccountType ?? "" },
                    { "HeaderDataSet", await headerTask },
                    { "FromDate", request.FromDateBs ?? "" },
                    { "ToDate", request.ToDateBs ?? "" },
                    { "BranchName", data.BranchName ?? "All" },
                    { "LedgerHead", data.LedgerHead ?? new List<string>() },
                    { "ShowOpeningBalance", data.ShowOpeningBalance },
                    { "ReportType", data.ReportType ?? "LedgerDetailsReport" },
                    { "Format", upperFormat },
                    { "PageSetting", page }
                };

                string viewPath = request.VisualReport
                    ? "Views/VisualReport/VLedgerDetailsReport.cshtml"
                    : "Views/Report/Account/MainLedgerReport/SubLedgerDetailsReport.cshtml";

                var html = await _jsReportService.RenderRazorToHtmlAndCacheAsync(
                    reportKey: reportKey,
                    reportPath: viewPath,
                    data: reportData, ct: ct);

                return await ReportExportHelper.CompleteAsync(
                    html, reportKey, upperFormat, ReportName, pdfView: request.VisualReport,
                    totalRecords: data.Rows.Count,
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