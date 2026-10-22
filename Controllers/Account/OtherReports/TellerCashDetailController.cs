using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NexgenCosysReport.Dtos.ReportDtos;
using NexgenCosysReport.Dtos.RequestDtos.AccountOperation.OthersReport;
using NexgenCosysReport.Inteface.ReportInterface;
using NexgenCosysReport.Inteface.ServiceInterface.AccountOperation.OthersReport;
using NexgenCosysReport.Utils.Caching;
using NexgenCosysReport.Utils.Enum;
using NexgenCosysReport.Utils.Report;
using System.Security.Claims;

namespace NexgenCosysReport.Controllers.Account.OtherReports
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class TellerCashDetailController : ControllerBase
    {
        private const string ReportName = "TellerCashDetail";

        private readonly ITellerCashDetailRepository _repository;
        private readonly IReportHeaderProvider _headers;
        private readonly IJsReportService _jsReportService;
        private readonly IReportDataCache _dataCache;
        private readonly ILogger<TellerCashDetailController> _logger;
        private readonly IReportFileResponse _reportFileResponse;

        // normal: A4 portrait (24-unit grid); visual: same page as every visual report
        private static readonly PageSizeSetting NPageSetting = PageSizeSetting.A4Portrait;
        private static readonly PageSizeSetting VPageSetting =
            PageSizeSetting.Custom(300, 297, PageUnit.mm, landscape: true);

        public TellerCashDetailController(
            ITellerCashDetailRepository repository,
            IReportHeaderProvider headers,
            IJsReportService jsReportService,
            IReportDataCache dataCache,
            ILogger<TellerCashDetailController> logger,
            IReportFileResponse reportFileResponse)
        {
            _repository = repository;
            _headers = headers;
            _jsReportService = jsReportService;
            _dataCache = dataCache;
            _logger = logger;
            _reportFileResponse = reportFileResponse;
        }

        // POST api/TellerCashDetail?format=VIEW
        [HttpPost]
        public async Task<IActionResult> GenerateReport(
            [FromBody] TellerCashDetailRequestDto request,
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

                // the header shows one branch only when a single branch is selected
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
                    if (loaded == null || (!loaded.TransactionRows.Any() && !loaded.ManualRows.Any()))
                        return null;
                    return loaded;
                }, ct);
                await Task.WhenAll(headerTask, dataTask);

                var data = await dataTask;
                if (data == null)
                    return NotFound(new { success = false, StatusCode = 400, message = "No data found" });

                var reportData = new Dictionary<string, object>
                {
                    { "TransactionRows", data.TransactionRows },
                    { "ManualRows", data.ManualRows },
                    { "TransactionCashBalanceDR", data.TransactionCashBalanceDR },
                    { "TransactionCashBalanceCR", data.TransactionCashBalanceCR },
                    { "VoucherCashBalanceDR", data.VoucherCashBalanceDR },
                    { "VoucherCashBalanceCR", data.VoucherCashBalanceCR },
                    { "TellerCashFromVault", data.TellerCashFromVault },
                    { "TellerCashFromTeller", data.TellerCashFromTeller },
                    { "TellerCashToTeller", data.TellerCashToTeller },
                    { "TellerCashToVault", data.TellerCashToVault },
                    { "TotalDR", data.TotalDR },
                    { "TotalCR", data.TotalCR },
                    { "TotalBalance", data.TotalBalance },
                    { "GrandTotal", data.GrandTotal },
                    { "HeaderDataSet", await headerTask },
                    { "TransactionDate", data.TransactionDateBs ?? "" },
                    { "BranchName", data.BranchName ?? "All" },
                    { "TellerName", data.TellerName ?? "All Teller" },
                    { "OrderBy", data.OrderBy ?? "" },
                    { "ReportType", data.ReportType ?? "D" },
                    { "Format", upperFormat },
                    { "PageSetting", page }
                };

                // NOTE: the visual view below must exist before a request with VisualReport = true is made
                var viewPath = isVisual
                    ? "Views/VisualReport/VTellerCashDetailReport.cshtml"
                    : "Views/Report/AccountOperation/OthersReport/TellerCashDetailReport.cshtml";

                var html = await _jsReportService.RenderRazorToHtmlAndCacheAsync(
                    reportKey: reportKey, reportPath: viewPath, data: reportData, ct: ct);

                return await ReportExportHelper.CompleteAsync(
                    html, reportKey, upperFormat, ReportName, pdfView: false,
                    totalRecords: data.TransactionRows.Count + data.ManualRows.Count,
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