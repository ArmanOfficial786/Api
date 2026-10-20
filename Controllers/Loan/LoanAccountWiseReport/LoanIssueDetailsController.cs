using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NexgenCosysReport.Dtos.ReportDtos;
using NexgenCosysReport.Dtos.RequestDtos.Loan.LoanAccountWiseReport;
using NexgenCosysReport.Inteface.ReportInterface;
using NexgenCosysReport.Inteface.ServiceInterface.Loan.LoanAccountWiseReport;
using NexgenCosysReport.Utils.Caching;
using NexgenCosysReport.Utils.Report;
using System.Security.Claims;

namespace NexgenCosysReport.Controllers.Loan.LoanAccountWiseReport
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class LoanIssueDetailsController : ControllerBase
    {
        private const string ReportName = "LoanIssueDetails";

        private readonly ILoanIssueDetailsRepository _repository;
        private readonly IReportHeaderProvider _headers;
        private readonly IJsReportService _jsReportService;
        private readonly IReportDataCache _dataCache;
        private readonly ILogger<LoanIssueDetailsController> _logger;
        private readonly IReportFileResponse _reportFileResponse;

        private static readonly PageSizeSetting PageSetting = PageSizeSetting.A4Landscape;

        public LoanIssueDetailsController(
            ILoanIssueDetailsRepository repository,
            IReportHeaderProvider headers,
            IJsReportService jsReportService,
            IReportDataCache dataCache,
            ILogger<LoanIssueDetailsController> logger,
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
            [FromBody] LoanIssueDetailsRequestDto request,
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

                var dataKey = ReportCacheKey.ForData(
                    request, ReportName, userIdClaim, nameof(request.VisualReport));
                var reportKey = $"{dataKey}_{page.CacheTag}";

                var cached = await ReportExportHelper.TryServeAsync(
                    reportKey, upperFormat, ReportName, pdfView: false,
                    _jsReportService, _reportFileResponse, _logger, page, ct);
                if (cached != null) return cached;

                var headerTask = _headers.GetAsync(request.BranchIds ?? "", ct);
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
                    { "MemberId", data.MemberId ?? "" },
                    { "MemberName", data.MemberName ?? "" },
                    { "FromDate", data.FromDateBs ?? "" },
                    { "ToDate", data.ToDateBs ?? "" },
                    { "BranchName", data.BranchName ?? "All" },
                    { "ReportType", data.ReportType ?? "LIR" },
                    { "Format", upperFormat },
                    { "PageSetting", page }
                };

                var html = await _jsReportService.RenderRazorToHtmlAndCacheAsync(
                    reportKey: reportKey,
                    reportPath: "Views/Report/Loan/LoanAccountWiseReport/LoanIssueDetailsReport.cshtml",
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