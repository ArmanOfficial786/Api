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
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class SavingTypeWiseBalanceController : ControllerBase
    {
        private const string ReportName = "SavingTypeWiseBalance";

        private readonly ISavingTypeWiseBalance _savingTypeService;
        private readonly IReportHeaderProvider _headers;
        private readonly IJsReportService _jsReportService;
        private readonly IReportDataCache _dataCache;
        private readonly ILogger<SavingTypeWiseBalanceController> _logger;
        private readonly IReportFileResponse _reportFileResponse;

        private static readonly PageSizeSetting NPageSetting = PageSizeSetting.A4Landscape;
        private static readonly PageSizeSetting VPageSetting =
            PageSizeSetting.Custom(300, 297, PageUnit.mm, landscape: true);

        public SavingTypeWiseBalanceController(
            ISavingTypeWiseBalance savingTypeService,
            IReportHeaderProvider headers,
            IJsReportService jsReportService,
            IReportDataCache dataCache,
            ILogger<SavingTypeWiseBalanceController> logger,
            IReportFileResponse reportFileResponse)
        {
            _savingTypeService = savingTypeService;
            _headers = headers;
            _jsReportService = jsReportService;
            _dataCache = dataCache;
            _logger = logger;
            _reportFileResponse = reportFileResponse;
        }

        [HttpPost]
        public async Task<IActionResult> GenerateReport(
            [FromBody] SavingTypeWiseBalanceRequest request,
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

                var branchIdForHeader = !request.SameCompanyName
                    && !string.IsNullOrEmpty(request.BranchId)
                    && request.BranchId != "-1"
                        ? request.BranchId
                        : "";

                var headerTask = _headers.GetAsync(branchIdForHeader, ct);
                var dataTask = _dataCache.GetOrLoadAsync(dataKey, async () =>
                {
                    var loaded = await _savingTypeService.GetSavingTypeWiseBalance(request);
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
                    { "TotalOpening", data.TotalOpening },
                    { "TotalDeposit", data.TotalDeposit },
                    { "TotalWithdraw", data.TotalWithdraw },
                    { "TotalBalance", data.TotalBalance },
                    { "TotalClosing", data.TotalClosing },
                    { "TotalRecords", data.TotalRecords },
                    { "HeaderDataSet", await headerTask },
                    { "FromDate", request.FromDate ?? "" },
                    { "ToDate", request.ToDate ?? "" },
                    { "BranchName", request.BranchName ?? "" },
                    { "IsNepali", request.IsNepali },
                    { "OpeningBalance", request.OpeningBalance },
                    { "PercentageBalance", request.PercentageBalance },
                    { "GroupByBranch", request.GroupByBranch },
                    { "GroupByCollectionCenter", request.GroupByCollectionCenter },
                    { "GroupByMemberGroup", request.GroupByMemberGroup },
                    { "ViewCollector", request.ViewCollector },
                    { "ViewDetail", request.ViewDetail },
                    { "OrderBy", request.OrderBy },
                    { "VisualReport", request.VisualReport },
                    { "Format", upperFormat },
                    { "PageSetting", page }
                };

                string viewPath = request.VisualReport
                    ? "Views/VisualReport/VSavingTypeWiseBalanceReport.cshtml"
                    : "Views/Report/MemberAC/SavingAcWiseReport/SavingTypeWiseBalance.cshtml";

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