using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NexgenCosysReport.Dtos.ReportDtos;
using NexgenCosysReport.Dtos.RequestDtos.MemberAccount.SavingAcWiseReport;
using NexgenCosysReport.Inteface.ReportInterface;
using NexgenCosysReport.Inteface.ServiceInterface.MemberAccount.SavingAcWiseReport;
using NexgenCosysReport.Utils.Caching;
using NexgenCosysReport.Utils.Enum;
using NexgenCosysReport.Utils.Report;
using System.Security.Claims;

namespace NexgenCosysReport.Controllers.MemberAccount.SavingAccWiseReport
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class MemberAccountDetailController : ControllerBase
    {
        private const string ReportName = "MemberAccountDetail";

        private readonly IMemberAccountDetail _memberAccountDetail;
        private readonly IReportHeaderProvider _headers;
        private readonly IJsReportService _jsReportService;
        private readonly IReportDataCache _dataCache;
        private readonly ILogger<MemberAccountDetailController> _logger;
        private readonly IReportFileResponse _reportFileResponse;

        private static readonly PageSizeSetting NPageSetting =
            PageSizeSetting.Custom(420, 297, PageUnit.mm, landscape: true);
        private static readonly PageSizeSetting VPageSetting =
            PageSizeSetting.Custom(300, 297, PageUnit.mm, landscape: true);

        public MemberAccountDetailController(
            IMemberAccountDetail memberAccountDetail,
            IReportHeaderProvider headers,
            IJsReportService jsReportService,
            IReportDataCache dataCache,
            ILogger<MemberAccountDetailController> logger,
            IReportFileResponse reportFileResponse)
        {
            _memberAccountDetail = memberAccountDetail;
            _headers = headers;
            _jsReportService = jsReportService;
            _dataCache = dataCache;
            _logger = logger;
            _reportFileResponse = reportFileResponse;
        }

        private static string GetStatusLabel(int status) => status switch
        {
            1 => "Opened",
            2 => "Closed",
            3 => "With Balance",
            4 => "Suspended",
            5 => "Disable",
            _ => "All"
        };

        [HttpPost]
        public async Task<IActionResult> GenerateReport(
            [FromBody] MemberAccountDetailRequest request,
            [FromQuery] string format = "VIEW",
            CancellationToken ct = default)
        {
            try
            {
                var userIdClaim = User.FindFirst("UserId")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !long.TryParse(userIdClaim, out _))
                    return NotFound(new { success = false, StatusCode = 401, message = "Unauthorized" });

                if (request == null)
                    return BadRequest(new { success = false, StatusCode = 400, message = "Invalid request" });

                if (string.IsNullOrEmpty(request.TillDate) || request.TillDate == "-1")
                    return BadRequest(new { success = false, StatusCode = 400, message = "Till Date is required" });

                if ((string.IsNullOrEmpty(request.BranchIds) || request.BranchIds == "-1") && !request.SameCompanyName)
                    return BadRequest(new { success = false, StatusCode = 400, message = "Please select Branch Office" });

                if (request.SelectedColumns == null || !request.SelectedColumns.Any())
                    return BadRequest(new { success = false, StatusCode = 400, message = "Please select at least one field to display" });

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

                var headerTask = _headers.GetAsync("", ct);
                var dataTask = _dataCache.GetOrLoadAsync(dataKey, async () =>
                {
                    var loaded = await _memberAccountDetail.GetMemberAccountDetailReport(request, ct);
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
                    { "TotalDeposit", data.TotalDeposit },
                    { "TotalWithdraw", data.TotalWithdraw },
                    { "TotalBalance", data.TotalBalance },
                    { "HeaderDataSet", await headerTask },
                    { "TillDate", request.TillDate },
                    { "BranchName", request.SameCompanyName ? "Same Company" : (request.BranchName ?? "") },
                    { "MemberId", request.MemberId ?? "" },
                    { "MemberName", request.MemberName ?? "" },
                    { "Status", GetStatusLabel(request.Status) },
                    { "OrderBy", request.OrderBy },
                    { "EnableCollectionCenterGroup", request.EnableCollectionCenterGroup },
                    { "EnableMemberGroupGroup", request.EnableMemberGroupGroup },
                    { "SelectedColumns", request.SelectedColumns },
                    { "VisualReport", request.VisualReport },
                    { "Format", upperFormat },
                    { "PageSetting", page }
                };

                string viewPath = request.VisualReport
                    ? "Views/VisualReport/AccountOperation/VMemberAccountDetailReport.cshtml"
                    : "Views/Report/MemberAC/SavingAcWiseReport/MemberAccountDetail.cshtml";

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