using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NexgenCosysReport.Dtos.ReportDtos;
using NexgenCosysReport.Dtos.RequestDtos.Member;
using NexgenCosysReport.Inteface.ReportInterface;
using NexgenCosysReport.Inteface.ServiceInterface.Member;
using NexgenCosysReport.Utils.Caching;
using NexgenCosysReport.Utils.Enum;
using NexgenCosysReport.Utils.Report;
using System.Security.Claims;

namespace NexgenCosysReport.Controllers.Member
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class MemberDetailsSummaryController : ControllerBase
    {
        private const string ReportName = "MemberDetailsSummary";

        private readonly IMemberDetailsSummary _repository;
        private readonly IReportHeaderProvider _headers;
        private readonly IJsReportService _jsReportService;
        private readonly IReportDataCache _dataCache;
        private readonly ILogger<MemberDetailsSummaryController> _logger;
        private readonly IReportFileResponse _reportFileResponse;

        // normal: A4 landscape (60-unit grid, the loan block has 12 columns); visual: same page as every visual report
        private static readonly PageSizeSetting NPageSetting =
            PageSizeSetting.Custom(297, 210, PageUnit.mm, landscape: true);
        private static readonly PageSizeSetting VPageSetting =
            PageSizeSetting.Custom(300, 297, PageUnit.mm, landscape: true);

        public MemberDetailsSummaryController(
            IMemberDetailsSummary repository,
            IReportHeaderProvider headers,
            IJsReportService jsReportService,
            IReportDataCache dataCache,
            ILogger<MemberDetailsSummaryController> logger,
            IReportFileResponse reportFileResponse)
        {
            _repository = repository;
            _headers = headers;
            _jsReportService = jsReportService;
            _dataCache = dataCache;
            _logger = logger;
            _reportFileResponse = reportFileResponse;
        }

        // POST api/MemberDetailsSummary?format=VIEW
        [HttpPost]
        public async Task<IActionResult> GenerateReport(
            [FromBody] MemberDetailsSummaryRequest request,
            [FromQuery] string format = "VIEW",
            CancellationToken ct = default)
        {
            try
            {
                var userIdClaim = User.FindFirst("UserId")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !long.TryParse(userIdClaim, out _))
                    return NotFound(new { success = false, StatusCode = 401, message = "Unauthorized" });

                if (request == null || !ModelState.IsValid || request.MemberRegistrationId <= 0)
                    return BadRequest(new { success = false, message = "Invalid request. MemberRegistrationId is required." });

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

                // the header is the company header (no branch)
                var headerTask = _headers.GetAsync("", ct);
                var dataTask = _dataCache.GetOrLoadAsync(dataKey, async () =>
                {
                    var loaded = await _repository.GetMemberDetailsSummary(request);
                    if (loaded == null || loaded.MemberInfo == null)
                        return null;
                    return loaded;
                }, ct);
                await Task.WhenAll(headerTask, dataTask);

                var data = await dataTask;
                if (data == null)
                    return NotFound(new { success = false, message = "Member not found" });

                var reportData = new Dictionary<string, object>
                {
                    { "MemberDetail", data.MemberInfo },
                    { "ShareAccounts", data.ShareAccounts },
                    { "SavingAccounts", data.SavingAccounts },
                    { "LoanIssues", data.LoanIssues },
                    { "GroupGuarantees", data.GroupGuarantees },
                    { "TotalShare", data.TotalShareRecords },
                    { "TotalSaving", data.TotalSavingRecords },
                    { "TotalLoan", data.TotalLoanRecords },
                    { "TotalGuarantee", data.TotalGuaranteeRecords },
                    { "HeaderDataSet", await headerTask },
                    { "FromDate", Convert.ToString(request.FromDate) ?? "" },
                    { "ToDate", Convert.ToString(request.ToDate) ?? "" },
                    { "Format", upperFormat },
                    { "PageSetting", page }
                };

                // NOTE: the visual view below must exist before a request with VisualReport = true is made
                var viewPath = isVisual
                    ? "Views/VisualReport/Member/VMemberDetailsSummaryReport.cshtml"
                    : "Views/Report/Member/MemberDetailsSummaryReport.cshtml";

                var html = await _jsReportService.RenderRazorToHtmlAndCacheAsync(
                    reportKey: reportKey, reportPath: viewPath, data: reportData, ct: ct);

                var totalRecords = (data.ShareAccounts?.Count ?? 0) + (data.SavingAccounts?.Count ?? 0) +
                                   (data.LoanIssues?.Count ?? 0) + (data.GroupGuarantees?.Count ?? 0);

                return await ReportExportHelper.CompleteAsync(
                    html, reportKey, upperFormat, ReportName, pdfView: false,
                    totalRecords: totalRecords,
                    _jsReportService, _reportFileResponse, _logger, page, ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating {Report} report", ReportName);
                return StatusCode(500, new { success = false, message = "An error occurred.", error = ex.Message });
            }
        }
    }
}