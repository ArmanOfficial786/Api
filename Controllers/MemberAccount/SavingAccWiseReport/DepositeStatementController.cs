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
using System.Text.Json;

namespace NexgenCosysReport.Controllers.MemberAccount.SavingAccWiseReport
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class DepositStatementController : ControllerBase
    {
        private const string ReportName = "DepositStatementReport";

        private readonly IDepositeStatement _repository;
        private readonly IReportHeaderProvider _headers;
        private readonly IJsReportService _jsReportService;
        private readonly IReportDataCache _dataCache;
        private readonly ILogger<DepositStatementController> _logger;
        private readonly IReportFileResponse _reportFileResponse;

        private static readonly PageSizeSetting NPageSetting = PageSizeSetting.A4Portrait;
        private static readonly PageSizeSetting VPageSetting =
            PageSizeSetting.Custom(300, 297, PageUnit.mm, landscape: true);

        public DepositStatementController(
            IDepositeStatement repository,
            IReportHeaderProvider headers,
            IJsReportService jsReportService,
            IReportDataCache dataCache,
            ILogger<DepositStatementController> logger,
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
            [FromBody] DepositStatementRequestDto request,
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

                var data = await _dataCache.GetOrLoadAsync(dataKey, async () =>
                {
                    var loaded = await _repository.GetDepositStatement(request);
                    if (loaded == null || loaded.Rows == null || !loaded.Rows.Any())
                        return null;
                    return loaded;
                }, ct);

                if (data == null)
                    return NotFound(new { success = false, StatusCode = 400, message = "No transactions found for the specified period" });

                var branchIdForHeader = !request.SameCompanyName && data.OfficeId.HasValue
                    ? data.OfficeId.Value.ToString()
                    : "";
                var headerData = await _headers.GetAsync(branchIdForHeader, ct);

                var reportData = new Dictionary<string, object>
                {
                    { "Rows", data.Rows },
                    { "MemberDetail", data.MemberDetail! },
                    { "OpeningBalance", data.OpeningBalance },
                    { "ClosingBalance", data.ClosingBalance },
                    { "InterestAmount", data.InterestAmount },
                    { "TaxAmount", data.TaxAmount },
                    { "HeaderDataSet", headerData },
                    { "FromDate", request.FromDateBs ?? "" },
                    { "ToDate", request.ToDateBs ?? "" },
                    { "AccountNo", request.AccountNo ?? "" },
                    { "EnableInterest", request.EnableInterest },
                    { "EntryBy", request.EntryBy },
                    { "EnableBillNumber", request.EnableBillNumber },
                    { "ValueDate", request.ValueDate },
                    { "VerifiedTillBs", data.VerifiedTillBs ?? string.Empty },
                    { "Language", request.Language },
                    { "VisualReport", request.VisualReport },
                    { "Format", upperFormat },
                    { "PageSetting", page }
                };

                if (upperFormat == "VIEW")
                {
                    Response.Headers.Append("X-Verification-Status", JsonSerializer.Serialize(new
                    {
                        data.HasVerification,
                        data.VerifiedTillBs
                    }));
                }

                string viewPath = request.VisualReport
                    ? "Views/VisualReport/VDepositStatementReport.cshtml"
                    : "Views/Report/MemberAC/SavingAcWiseReport/DepositeStatement.cshtml";

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