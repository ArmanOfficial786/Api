using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NexgenCosysReport.Dtos.ReportDtos;
using NexgenCosysReport.Dtos.RequestDtos.Account.ThirdLedgerDetailsReport;
using NexgenCosysReport.Inteface.ReportInterface;
using NexgenCosysReport.Inteface.ServiceInterface.Account.ThirdLedgerDetailsReport;
using NexgenCosysReport.Utils.Caching;
using NexgenCosysReport.Utils.Enum;
using NexgenCosysReport.Utils.Report;
using System.Security.Claims;

namespace NexgenCosysReport.Controllers.Account.MainLedgerReport
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ThirdLedgerDetailsController : ControllerBase
    {
        private const string ReportName = "ThirdLedgerDetails";

        private readonly IThirdLedgerDetailsRepository _repository;
        private readonly IReportHeaderProvider _headers;
        private readonly IJsReportService _jsReportService;
        private readonly IReportDataCache _dataCache;
        private readonly ILogger<ThirdLedgerDetailsController> _logger;
        private readonly IReportFileResponse _reportFileResponse;

        private static readonly PageSizeSetting NPageSetting =
            PageSizeSetting.Custom(297, 210, PageUnit.mm, landscape: true);
        private static readonly PageSizeSetting VPageSetting =
            PageSizeSetting.Custom(300, 297, PageUnit.mm, landscape: true);

        public ThirdLedgerDetailsController(
            IThirdLedgerDetailsRepository repository,
            IReportHeaderProvider headers,
            IJsReportService jsReportService,
            IReportDataCache dataCache,
            ILogger<ThirdLedgerDetailsController> logger,
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
            [FromBody] ThirdLedgerDetailsRequestDto request,
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

                var dataKey = ReportCacheKey.ForData(
                    request, ReportName, userIdClaim, nameof(request.VisualReport));
                var reportKey = $"{dataKey}_{(isVisual ? "VISUAL" : "NORMAL")}_{page.CacheTag}";

                var cached = await ReportExportHelper.TryServeAsync(
                    reportKey, upperFormat, ReportName, pdfView: false,
                    _jsReportService, _reportFileResponse, _logger, page, ct);
                if (cached != null) return cached;

                var headerTask = _headers.GetAsync(request.BranchIds, ct);
                var dataTask = _dataCache.GetOrLoadAsync(dataKey, async () =>
                {
                    var loaded = await _repository.GetThirdLedgerDetailsDataAsync(request);
                    if (loaded == null || !loaded.Rows.Any()) return null;
                    return loaded;
                }, ct);
                await Task.WhenAll(headerTask, dataTask);

                var data = await dataTask;
                if (data == null)
                    return NotFound(new { success = false, StatusCode = 404, message = "No data found" });

                var reportData = new Dictionary<string, object>
                {
                    { "Rows", data.Rows },
                    { "TotalRecords", data.TotalRecords },
                    { "TotalDebitAmount", data.TotalDebitAmount },
                    { "TotalCreditAmount", data.TotalCreditAmount },
                    { "TotalBalance", data.TotalBalance },
                    { "OpeningBalance", data.OpeningBalance },
                    { "ClosingBalance", data.ClosingBalance },
                    { "AccountType", data.AccountType },
                    { "FromDate", data.FromDateBs },
                    { "ToDate", data.ToDateBs },
                    { "BranchNames", data.BranchNames ?? "All Branches" },
                    { "LedgerName", data.LedgerName },
                    { "SubLedgerName", data.SubLedgerName },
                    { "SecondSubLedgerName", data.SecondSubLedgerName },
                    { "ThirdSubLedgerName", data.ThirdSubLedgerName },
                    { "VoucherType", data.VoucherType },
                    { "ReportType", data.ReportType },
                    { "ShowOpeningBalance", data.ShowOpeningBalance },
                    { "OrderBy", data.OrderBy },
                    { "LedgerHeadName", data.LedgerHeadName },
                    { "HeaderDataSet", await headerTask },
                    { "Format", upperFormat },
                    { "VisualReport", request.VisualReport },
                    { "PageSetting", page }
                };

                var viewPath = isVisual
                    ? "Views/VisualReport/VThirdLedgerDetailsReport.cshtml"
                    : "Views/Report/Account/MainLedgerReport/ThirdLedgerDetailsReport.cshtml";

                var html = await _jsReportService.RenderRazorToHtmlAndCacheAsync(
                    reportKey: reportKey, reportPath: viewPath, data: reportData, ct: ct);

                return await ReportExportHelper.CompleteAsync(
                    html, reportKey, upperFormat, ReportName, pdfView: false,
                    totalRecords: data.TotalRecords,
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