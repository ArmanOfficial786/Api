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
    public class MemberPenaltyDepositWithdrawController : ControllerBase
    {
        private const string ReportName = "MemberPenaltyDepositWithdrawReport";

        private readonly IMemberPenaltyDepositWithdraw _penaltyDepositWithdrawService;
        private readonly IReportHeaderProvider _headers;
        private readonly IJsReportService _jsReportService;
        private readonly IReportDataCache _dataCache;
        private readonly ILogger<MemberPenaltyDepositWithdrawController> _logger;
        private readonly IReportFileResponse _reportFileResponse;

        private static readonly PageSizeSetting NPageSetting =
            PageSizeSetting.Custom(240, 297, PageUnit.mm, landscape: false);
        private static readonly PageSizeSetting VPageSetting =
            PageSizeSetting.Custom(300, 297, PageUnit.mm, landscape: true);

        public MemberPenaltyDepositWithdrawController(
            IMemberPenaltyDepositWithdraw penaltyDepositWithdrawService,
            IReportHeaderProvider headers,
            IJsReportService jsReportService,
            IReportDataCache dataCache,
            ILogger<MemberPenaltyDepositWithdrawController> logger,
            IReportFileResponse reportFileResponse)
        {
            _penaltyDepositWithdrawService = penaltyDepositWithdrawService;
            _headers = headers;
            _jsReportService = jsReportService;
            _dataCache = dataCache;
            _logger = logger;
            _reportFileResponse = reportFileResponse;
        }

        private static string GetTransactionTypeLabel(int type) => type switch
        {
            1 => "Penalty",
            2 => "Deposit",
            3 => "Withdraw",
            4 => "Balance",
            _ => "Penalty"
        };

        [HttpPost]
        public async Task<IActionResult> GenerateReport(
            [FromBody] MemberPenaltyDepositWithdrawRequest request,
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

                if (string.IsNullOrEmpty(request.FromDate) || request.FromDate == "-1")
                    return BadRequest(new { success = false, StatusCode = 400, message = "From Date is required" });

                if (string.IsNullOrEmpty(request.ToDate) || request.ToDate == "-1")
                    return BadRequest(new { success = false, StatusCode = 400, message = "To Date is required" });

                if ((string.IsNullOrEmpty(request.BranchIds) || request.BranchIds == "-1") && !request.SameCompanyName)
                    return BadRequest(new { success = false, StatusCode = 400, message = "Please select Branch Office" });

                if (request.Amount < 0)
                    return BadRequest(new { success = false, StatusCode = 400, message = "Amount must be greater than or equal to 0" });

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
                    var loaded = await _penaltyDepositWithdrawService.GetMemberPenaltyDepositWithdrawReport(request);
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
                    { "TotalPenalty", data.TotalPenalty },
                    { "TotalDeposit", data.TotalDeposit },
                    { "TotalWithdraw", data.TotalWithdraw },
                    { "TotalBalance", data.TotalBalance },
                    { "HeaderDataSet", await headerTask },
                    { "FromDate", request.FromDate },
                    { "ToDate", request.ToDate },
                    { "BranchName", request.SameCompanyName ? "Same Company" : (request.BranchName ?? "") },
                    { "TransactionType", GetTransactionTypeLabel(request.TransactionType) },
                    { "TransactionTypeValue", request.TransactionType },
                    { "Amount", request.Amount },
                    { "OrderBy", request.OrderBy },
                    { "VisualReport", request.VisualReport },
                    { "Format", upperFormat },
                    { "PageSetting", page }
                };

                string viewPath = request.VisualReport
                    ? "Views/VisualReport/AccountOperation/VMemberPenaltyDepositWithdrawReport.cshtml"
                    : "Views/Report/MemberAC/SavingAcWiseReport/MemberPenaltyDepositWithdraw.cshtml";

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