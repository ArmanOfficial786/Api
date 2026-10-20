using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NexgenCosysReport.Dtos.ReportDtos;
using NexgenCosysReport.Dtos.RequestDtos.Loan.OtherReports;
using NexgenCosysReport.Inteface.ReportInterface;
using NexgenCosysReport.Inteface.ServiceInterface.Loan.OtherReports;
using NexgenCosysReport.Utils.Caching;
using NexgenCosysReport.Utils.Report;
using System.Security.Claims;

namespace NexgenCosysReport.Controllers.Loan.OtherReports
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class LoanStatementController : ControllerBase
    {
        private const string ReportName = "LoanStatement";

        private readonly ILoanStatementRepository _repository;
        private readonly IReportHeaderProvider _headers;
        private readonly IJsReportService _jsReportService;
        private readonly IReportDataCache _dataCache;
        private readonly ILogger<LoanStatementController> _logger;
        private readonly IReportFileResponse _reportFileResponse;

        private static readonly PageSizeSetting PageSetting = PageSizeSetting.A4Portrait;

        public LoanStatementController(
            ILoanStatementRepository repository,
            IReportHeaderProvider headers,
            IJsReportService jsReportService,
            IReportDataCache dataCache,
            ILogger<LoanStatementController> logger,
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
            [FromBody] LoanStatementRequestDto request,
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

                var headerTask = _headers.GetAsync("", ct);
                var dataTask = _dataCache.GetOrLoadAsync(dataKey, async () =>
                {
                    var loaded = await _repository.GetReportDataAsync(request);
                    if (loaded == null)
                        return null;
                    if ((loaded.Transactions == null || !loaded.Transactions.Any()) && loaded.MemberInfo == null)
                        return null;
                    return loaded;
                }, ct);
                await Task.WhenAll(headerTask, dataTask);

                var data = await dataTask;
                if (data == null)
                    return NotFound(new { success = false, StatusCode = 400, message = "No data found" });

                var reportData = new Dictionary<string, object>
                {
                    { "MemberInfo", data.MemberInfo ?? new LoanStatementMemberInfoDto() },
                    { "Transactions", data.Transactions ?? new List<LoanStatementTransactionDto>() },
                    { "Guarantees", data.Guarantees ?? new List<LoanStatementGuaranteeDto>() },
                    { "HeaderDataSet", await headerTask },
                    { "FromDate", data.FromDateBs ?? "" },
                    { "ToDate", data.ToDateBs ?? "" },
                    { "Format", upperFormat },
                    { "Language", request.Language },
                    { "PageSetting", page }
                };

                var html = await _jsReportService.RenderRazorToHtmlAndCacheAsync(
                    reportKey: reportKey,
                    reportPath: "Views/Report/Loan/LoanAccountWiseReport/LoanStatementReport.cshtml",
                    data: reportData, ct: ct);

                return await ReportExportHelper.CompleteAsync(
                    html, reportKey, upperFormat, ReportName, pdfView: false,
                    totalRecords: data.Transactions?.Count ?? 0,
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