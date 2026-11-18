using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NexgenCosysReport.Dtos.ReportDtos;
using NexgenCosysReport.Dtos.RequestDtos.MemberAccount.InterestPayableReport;
using NexgenCosysReport.Inteface.ReportInterface;
using NexgenCosysReport.Inteface.ServiceInterface.MemberAccount.InterestPayableReport;
using NexgenCosysReport.Utils.Caching;
using NexgenCosysReport.Utils.Report;
using System.Security.Claims;

namespace NexgenCosysReport.Controllers.MemberAccount.InterestPayableReport
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class FixedDepositCertificateScheduleController : ControllerBase
    {
        private readonly IFixedDepositCertificateScheduleRepository _repository;
        private readonly IReportHeaderProvider _headers;
        private readonly IJsReportService _jsReportService;
        private readonly IReportDataCache _dataCache;
        private readonly ILogger<FixedDepositCertificateScheduleController> _logger;
        private readonly IReportFileResponse _reportFileResponse;

        private static readonly PageSizeSetting PageSetting = PageSizeSetting.A4Portrait;

        public FixedDepositCertificateScheduleController(
            IFixedDepositCertificateScheduleRepository repository,
            IReportHeaderProvider headers,
            IJsReportService jsReportService,
            IReportDataCache dataCache,
            ILogger<FixedDepositCertificateScheduleController> logger,
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
            [FromBody] FixedDepositCertificateScheduleRequestDto request,
            [FromQuery] string format = "VIEW",
            CancellationToken ct = default)
        {
            var reportName = $"FixedDeposit{request?.ReportType}";

            try
            {
                var userIdClaim = User.FindFirst("UserId")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !long.TryParse(userIdClaim, out var userId))
                    return NotFound(new { success = false, StatusCode = 401, message = "Unauthorized" });

                if (request == null || !ModelState.IsValid)
                    return BadRequest(new { success = false, StatusCode = 400, message = "Invalid request" });

                var upperFormat = format.ToUpperInvariant();
                var page = PageSetting;

                var dataKey = ReportCacheKey.ForData(request, reportName, userIdClaim);
                var reportKey = $"{dataKey}_{page.CacheTag}";

                var cached = await ReportExportHelper.TryServeAsync(
                    reportKey, upperFormat, reportName, pdfView: false,
                    _jsReportService, _reportFileResponse, _logger, page, ct);
                if (cached != null) return cached;

                var officeIdClaim = User.FindFirst("OfficeId")?.Value;
                var branchIdForHeader = long.TryParse(officeIdClaim, out var officeId) ? officeId.ToString() : "";

                var headerTask = _headers.GetAsync(branchIdForHeader, ct);
                var dataTask = _dataCache.GetOrLoadAsync(dataKey, async () =>
                {
                    var loaded = request.ReportType == "Schedule"
                        ? await _repository.GetScheduleDataAsync(request, userId)
                        : await _repository.GetCertificateDataAsync(request, userId);
                    if (loaded == null || loaded.CertificateDetail == null)
                        return null;
                    return loaded;
                }, ct);
                await Task.WhenAll(headerTask, dataTask);

                var data = await dataTask;
                if (data == null)
                    return NotFound(new { success = false, StatusCode = 400, message = "No data found" });

                var reportData = new Dictionary<string, object>
                {
                    { "CertificateDetail", data.CertificateDetail },
                    { "ScheduleRows", data.ScheduleRows },
                    { "TotalRecords", data.TotalRecords },
                    { "TotalInterest", data.TotalInterest },
                    { "TotalTax", data.TotalTax },
                    { "TotalNetAmount", data.TotalNetAmount },
                    { "HeaderDataSet", await headerTask },
                    { "AccountNo", data.AccountNo ?? "" },
                    { "MemberId", data.MemberId ?? "" },
                    { "MemberName", data.MemberName ?? "" },
                    { "ReportType", request.ReportType },
                    { "Format", upperFormat },
                    { "PageSetting", page }
                };

                var html = await _jsReportService.RenderRazorToHtmlAndCacheAsync(
                    reportKey: reportKey,
                    reportPath: "Views/Report/MemberAC/InterestPayableReport/FixedDepositCertificateScheduleReport.cshtml",
                    data: reportData, ct: ct);

                return await ReportExportHelper.CompleteAsync(
                    html, reportKey, upperFormat, reportName, pdfView: false,
                    totalRecords: request.ReportType == "Schedule" ? data.TotalRecords : 1,
                    _jsReportService, _reportFileResponse, _logger, page, ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating {Report} report", reportName);
                return StatusCode(500, new { message = ex.Message, inner = ex.InnerException?.Message });
            }
        }
    }
}