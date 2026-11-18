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
    public class DepositWithdrawMaxAmountRangeController : ControllerBase
    {
        private const string ReportName = "DepositWithdrawMaximumAmountRange";

        private readonly IDepositWithdrawMaxAmountRange _depositWithdrawService;
        private readonly IReportHeaderProvider _headers;
        private readonly IJsReportService _jsReportService;
        private readonly IReportDataCache _dataCache;
        private readonly ILogger<DepositWithdrawMaxAmountRangeController> _logger;
        private readonly IReportFileResponse _reportFileResponse;

        private static readonly PageSizeSetting NPageSetting = PageSizeSetting.A4Portrait;
        private static readonly PageSizeSetting VPageSetting =
            PageSizeSetting.Custom(300, 297, PageUnit.mm, landscape: true);

        public DepositWithdrawMaxAmountRangeController(
            IDepositWithdrawMaxAmountRange depositWithdrawService,
            IReportHeaderProvider headers,
            IJsReportService jsReportService,
            IReportDataCache dataCache,
            ILogger<DepositWithdrawMaxAmountRangeController> logger,
            IReportFileResponse reportFileResponse)
        {
            _depositWithdrawService = depositWithdrawService;
            _headers = headers;
            _jsReportService = jsReportService;
            _dataCache = dataCache;
            _logger = logger;
            _reportFileResponse = reportFileResponse;
        }

        private static string GetTransactionTypeLabel(int type) => type switch
        {
            1 => "Deposit",
            2 => "Withdraw",
            _ => "Both"
        };

        [HttpPost("GenerateReport")]
        public async Task<IActionResult> GenerateReport(
            [FromBody] DepositWithdrawMaxAmountRangeRequest request,
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
                    reportKey, upperFormat, ReportName, pdfView: request.VisualReport,
                    _jsReportService, _reportFileResponse, _logger, page, ct);
                if (cached != null) return cached;

                var branchIdForHeader = !request.SameCompanyName
                    && !string.IsNullOrEmpty(request.BranchIds)
                    && request.BranchIds != "-1"
                    && !request.BranchIds.Contains(',')
                        ? request.BranchIds
                        : "";

                var headerTask = _headers.GetAsync(branchIdForHeader, ct);
                var dataTask = _dataCache.GetOrLoadAsync(dataKey, async () =>
                {
                    var loaded = await _depositWithdrawService.GetDepositWithdrawMaxAmountRange(request);
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
                    { "HeaderDataSet", await headerTask },
                    { "FromDate", request.FromDate ?? "" },
                    { "ToDate", request.ToDate ?? "" },
                    { "BranchName", string.IsNullOrEmpty(request.BranchName) ? "All" : request.BranchName },
                    { "TransactionType", GetTransactionTypeLabel(request.TransactionType) },
                    { "TransactionTypeValue", request.TransactionType },
                    { "Amount", request.Amount },
                    { "OrderBy", request.OrderBy },
                    { "VisualReport", request.VisualReport },
                    { "Format", upperFormat },
                    { "PageSetting", page }
                };

                string viewPath = request.VisualReport
                    ? "Views/VisualReport/VDepositWithdrawMaximumAmountRangeReport.cshtml"
                    : "Views/Report/MemberAC/SavingAcWiseReport/DepositWithdrawMaximumAmountRange .cshtml";

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