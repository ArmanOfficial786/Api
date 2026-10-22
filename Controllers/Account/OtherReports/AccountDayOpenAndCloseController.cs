using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NexgenCosysReport.Dtos.ReportDtos;
using NexgenCosysReport.Dtos.RequestDtos.Account.OtherReports;
using NexgenCosysReport.Inteface.ReportInterface;
using NexgenCosysReport.Inteface.ServiceInterface.Account.OtherReports;
using NexgenCosysReport.Utils.Caching;
using NexgenCosysReport.Utils.Report;
using System.Security.Claims;

namespace NexgenCosysReport.Controllers.Account.OtherReports
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class AccountDayOpenAndCloseController : ControllerBase
    {
        private const string ReportName = "AccountDayOpenAndClose";

        private readonly IAccountDayOpenAndClose _repository;
        private readonly IReportHeaderProvider _headers;
        private readonly IJsReportService _jsReportService;
        private readonly IReportDataCache _dataCache;
        private readonly ILogger<AccountDayOpenAndCloseController> _logger;
        private readonly IReportFileResponse _reportFileResponse;

        // 7 columns, so A4 portrait is enough
        private static readonly PageSizeSetting PageSetting = PageSizeSetting.A4Portrait;

        public AccountDayOpenAndCloseController(
            IAccountDayOpenAndClose repository,
            IReportHeaderProvider headers,
            IJsReportService jsReportService,
            IReportDataCache dataCache,
            ILogger<AccountDayOpenAndCloseController> logger,
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
            [FromBody] AccountDayOpenAndCloseRequestDto request,
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
                var page = PageSetting;

                // the DB data is cached once; the visual flag is left out of the key because this report has no visual view
                var dataKey = ReportCacheKey.ForData(
                    request, ReportName, userIdClaim, nameof(request.VisualReport));
                var reportKey = $"{dataKey}_{page.CacheTag}";

                var cached = await ReportExportHelper.TryServeAsync(
                    reportKey, upperFormat, ReportName, pdfView: false,
                    _jsReportService, _reportFileResponse, _logger, page, ct);
                if (cached != null) return cached;

                // the header shows one office only when a single numeric branch is selected (same rule as before)
                var officeIdForHeader = "";
                if (!string.IsNullOrEmpty(request.branchId) &&
                    request.branchId != "-1" &&
                    request.branchId != "string" &&
                    long.TryParse(request.branchId, out _))
                {
                    officeIdForHeader = request.branchId;
                }

                var headerTask = _headers.GetAsync(officeIdForHeader, ct);
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
                    { "HeaderDataSet", await headerTask },
                    { "FromDate", request.FromDateBs ?? "" },
                    { "ToDate", request.ToDateBs ?? "" },
                    { "BranchNames", data.BranchNames ?? "All Branches" },
                    { "OrderBy", request.OrderBy ?? "" },
                    { "Format", upperFormat },
                    { "PageSetting", page }
                };

                var html = await _jsReportService.RenderRazorToHtmlAndCacheAsync(
                    reportKey: reportKey,
                    reportPath: "Views/Report/Account/OtherReports/AccountDayOpenAndCloseReport.cshtml",
                    data: reportData, ct: ct);

                return await ReportExportHelper.CompleteAsync(
                    html, reportKey, upperFormat, ReportName, pdfView: false,
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