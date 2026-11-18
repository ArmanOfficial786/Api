//// Controllers/Loan/LoanAnalysisReports/LoanDetailsController.cs
//using Microsoft.AspNetCore.Authorization;
//using Microsoft.AspNetCore.Mvc;
//using NexgenCosysReport.Dtos.ReportDtos;
//using NexgenCosysReport.Dtos.RequestDtos.Loan.LoanAnalysisReport;
//using NexgenCosysReport.Inteface.ReportInterface;
//using NexgenCosysReport.Inteface.ServiceInterface.Loan.LoanAnalysisReport;
//using NexgenCosysReport.Utils.Caching;
//using NexgenCosysReport.Utils.Enum;
//using NexgenCosysReport.Utils.Report;
//using System.Security.Claims;

//namespace NexgenCosysReport.Controllers.Loan.LoanAnalysisReport
//{
//    [ApiController]
//    [Route("api/[controller]")]
//    [Authorize]
//    public class LoanDetailsController : ControllerBase
//    {
//        private const string ReportName = "LoanDetails";

//        private readonly ILoanDetailsRepository _repository;
//        private readonly IReportHeaderProvider _headers;
//        private readonly IJsReportService _jsReportService;
//        private readonly IReportDataCache _dataCache;
//        private readonly ILogger<LoanDetailsController> _logger;
//        private readonly IReportFileResponse _reportFileResponse;

//        private static readonly PageSizeSetting NPageSetting = PageSizeSetting.Custom(230, 297, PageUnit.mm, landscape: false);
//        private static readonly PageSizeSetting VPageSetting =
//            PageSizeSetting.Custom(300, 297, PageUnit.mm, landscape: true);

//        public LoanDetailsController(
//            ILoanDetailsRepository repository,
//            IReportHeaderProvider headers,
//            IJsReportService jsReportService,
//            IReportDataCache dataCache,
//            ILogger<LoanDetailsController> logger,
//            IReportFileResponse reportFileResponse)
//        {
//            _repository = repository;
//            _headers = headers;
//            _jsReportService = jsReportService;
//            _dataCache = dataCache;
//            _logger = logger;
//            _reportFileResponse = reportFileResponse;
//        }

//        [HttpPost()]
//        public async Task<IActionResult> GenerateReport(
//            [FromBody] LoanDetailsRequestDto request,
//            [FromQuery] string format = "VIEW",
//            CancellationToken ct = default)
//        {
//            try
//            {
//                var userIdClaim = User.FindFirst("UserId")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
//                if (string.IsNullOrEmpty(userIdClaim) || !long.TryParse(userIdClaim, out _))
//                    return NotFound(new { success = false, StatusCode = 401, message = "Unauthorized" });

//                if (request == null || !ModelState.IsValid)
//                    return BadRequest(new { success = false, StatusCode = 400, message = "Invalid request" });

//                var upperFormat = format.ToUpperInvariant();
//                var isVisual = request.VisualReport;
//                var page = isVisual ? VPageSetting : NPageSetting;


//                var dataKey = ReportCacheKey.ForData(
//                    request, ReportName, userIdClaim, nameof(request.VisualReport));
//                var reportKey = $"{dataKey}_{(isVisual ? "VISUAL" : "NORMAL")}_{page.CacheTag}";

//                var cached = await ReportExportHelper.TryServeAsync(
//                    reportKey, upperFormat, ReportName, pdfView: false,
//                    _jsReportService, _reportFileResponse, _logger, page, ct);
//                if (cached != null) return cached;


//                var headerTask = _headers.GetAsync(request.BranchIds, ct);
//                var dataTask = _dataCache.GetOrLoadAsync(dataKey, async () =>
//                {
//                    var loaded = await _repository.GetReportDataAsync(request);
//                    if (loaded == null || !loaded.Rows.Any()) return null;
//                    return loaded;
//                }, ct);
//                await Task.WhenAll(headerTask, dataTask);

//                var data = await dataTask;
//                if (data == null)
//                    return NotFound(new { success = false, StatusCode = 400, message = "No data found" });

//                var reportData = new Dictionary<string, object>
//                {
//                    { "Rows", data.Rows },
//                    { "TotalRecords", data.TotalRecords },
//                    { "TotalDisburseAmount", data.TotalDisburseAmount },
//                    { "TotalRepaidTill", data.TotalRepaidTill },
//                    { "TotalRepaid", data.TotalRepaid },
//                    { "TotalBalanceAmount", data.TotalBalanceAmount },
//                    { "HeaderDataSet", await headerTask },
//                    { "FromDate", data.FromDateBs ?? "" },
//                    { "ToDate", data.ToDateBs ?? "" },
//                    { "BranchName", data.BranchName ?? "All" },
//                    { "LoanTypeName", data.LoanTypeName ?? "" },
//                    { "StatusName", data.StatusName ?? "All" },
//                    { "CollectionCenterName", data.CollectionCenterName ?? "" },
//                    { "CollectorName", data.CollectorName ?? "" },
//                    { "OrderBy", data.OrderBy ?? "-1" },
//                    { "ReportType", data.ReportType ?? "D" },
//                    { "EnableCollectionCenter", request.EnableCollectionCenter },
//                    { "Format", upperFormat },
//                    { "PageSetting", page }
//                };

//                var viewPath = isVisual
//                    ? "Views/VisualReport/Loan/LoanAnalysisReports/VLoanDetailsReport.cshtml"
//                    : "Views/Report/Loan/LoanAnalysisReports/LoanDetailsReport.cshtml";

//                var html = await _jsReportService.RenderRazorToHtmlAndCacheAsync(
//                    reportKey: reportKey, reportPath: viewPath, data: reportData, ct: ct);

//                return await ReportExportHelper.CompleteAsync(
//                    html, reportKey, upperFormat, ReportName, pdfView: false,
//                    totalRecords: data.TotalRecords,
//                    _jsReportService, _reportFileResponse, _logger, page, ct);
//            }
//            catch (ArgumentException ex)
//            {
//                return BadRequest(new { success = false, StatusCode = 400, message = ex.Message });
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, "Error generating {Report} report", ReportName);
//                return StatusCode(500, new { message = ex.Message, inner = ex.InnerException?.Message });
//            }
//        }
//    }
//}





using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NexgenCosysReport.Dtos.ReportDtos;
using NexgenCosysReport.Dtos.RequestDtos.Loan.LoanAnalysisReport;
using NexgenCosysReport.Inteface.ReportInterface;
using NexgenCosysReport.Inteface.ServiceInterface.Loan.LoanAnalysisReport;
using NexgenCosysReport.Utils.Caching;
using NexgenCosysReport.Utils.Enum;
using NexgenCosysReport.Utils.Report;
using System.Security.Claims;

namespace NexgenCosysReport.Controllers.Loan.LoanAnalysisReport
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class LoanDetailsController : ControllerBase
    {
        private const string ReportName = "LoanDetails";

        private readonly ILoanDetailsRepository _repository;
        private readonly IReportHeaderProvider _headers;
        private readonly IJsReportService _jsReportService;
        private readonly IReportDataCache _dataCache;
        private readonly ILogger<LoanDetailsController> _logger;
        private readonly IReportFileResponse _reportFileResponse;

        private static readonly PageSizeSetting NPageSetting = PageSizeSetting.Custom(230, 297, PageUnit.mm, landscape: false);
        private static readonly PageSizeSetting VPageSetting =
            PageSizeSetting.Custom(300, 297, PageUnit.mm, landscape: true);

        public LoanDetailsController(
            ILoanDetailsRepository repository,
            IReportHeaderProvider headers,
            IJsReportService jsReportService,
            IReportDataCache dataCache,
            ILogger<LoanDetailsController> logger,
            IReportFileResponse reportFileResponse)
        {
            _repository = repository;
            _headers = headers;
            _jsReportService = jsReportService;
            _dataCache = dataCache;
            _logger = logger;
            _reportFileResponse = reportFileResponse;
        }

        [HttpPost()]
        public async Task<IActionResult> GenerateReport(
            [FromBody] LoanDetailsRequestDto request,
            [FromQuery] string format = "VIEW",
            CancellationToken ct = default)
        {
            try
            {
                var userIdClaim = User.FindFirst("UserId")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !long.TryParse(userIdClaim, out var userId))
                    return Unauthorized(new { success = false, StatusCode = 401, message = "Unauthorized" });

                if (request == null || !ModelState.IsValid)
                    return BadRequest(new { success = false, StatusCode = 400, message = "Invalid request" });

                // Webform: both dates are required
                if (string.IsNullOrWhiteSpace(request.FromDateBs) || string.IsNullOrWhiteSpace(request.ToDateBs)
                    || request.FromDateBs == "-1" || request.ToDateBs == "-1")
                    return BadRequest(new { success = false, StatusCode = 400, message = "From Date and To Date are required." });

                // Webform default: Detail. Normalize before the cache key so a missing value and "D" share one entry.
                request.ReportType = string.Equals(request.ReportType?.Trim(), "S", StringComparison.OrdinalIgnoreCase)
                                     || string.Equals(request.ReportType?.Trim(), "Summary", StringComparison.OrdinalIgnoreCase)
                    ? "S" : "D";

                var upperFormat = format.ToUpperInvariant();
                var isVisual = request.VisualReport;
                var page = isVisual ? VPageSetting : NPageSetting;

                var dataKey = ReportCacheKey.ForData(
                    request, ReportName, userIdClaim, nameof(request.VisualReport));
                var reportKey = $"{dataKey}_{(isVisual ? "VISUAL" : "NORMAL")}_{page.CacheTag}";

                var cached = await ReportExportHelper.TryServeAsync(
                    reportKey, upperFormat, ReportName, pdfView: false,
                    _jsReportService, _reportFileResponse, _logger, page, ct);
                if (cached != null) return cached;

                var headerTask = _headers.GetAsync(request.BranchIds ?? "", ct);
                var dataTask = _dataCache.GetOrLoadAsync(dataKey, async () =>
                {
                    var loaded = await _repository.GetReportDataAsync(request, userId);
                    if (loaded == null || !loaded.Rows.Any()) return null;
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
                    { "TotalDisburseAmount", data.TotalDisburseAmount },
                    { "TotalRepaidTill", data.TotalRepaidTill },
                    { "TotalRepaid", data.TotalRepaid },
                    { "TotalBalanceAmount", data.TotalBalanceAmount },
                    { "HeaderDataSet", await headerTask },
                    { "FromDate", data.FromDateBs ?? "" },
                    { "ToDate", data.ToDateBs ?? "" },
                    { "BranchName", data.BranchName ?? "All" },
                    { "LoanTypeName", data.LoanTypeName ?? "" },
                    { "StatusName", data.StatusName ?? "All" },
                    { "CollectionCenterName", data.CollectionCenterName ?? "" },
                    { "CollectorName", data.CollectorName ?? "" },
                    { "OrderBy", data.OrderBy ?? "-1" },
                    { "ReportType", data.ReportType ?? "D" },
                    { "EnableCollectionCenter", request.EnableCollectionCenter },
                    { "Format", upperFormat },
                    { "PageSetting", page }
                };

                var viewPath = isVisual
                    ? "Views/VisualReport/Loan/LoanAnalysisReports/VLoanDetailsReport.cshtml"
                    : "Views/Report/Loan/LoanAnalysisReports/LoanDetailsReport.cshtml";

                var html = await _jsReportService.RenderRazorToHtmlAndCacheAsync(
                    reportKey: reportKey, reportPath: viewPath, data: reportData, ct: ct);

                return await ReportExportHelper.CompleteAsync(
                    html, reportKey, upperFormat, ReportName, pdfView: false,
                    totalRecords: data.TotalRecords,
                    _jsReportService, _reportFileResponse, _logger, page, ct);
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "{Report}: access denied", ReportName);
                return StatusCode(403, new { success = false, StatusCode = 403, message = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { success = false, StatusCode = 400, message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating {Report} report", ReportName);
                return StatusCode(500, new { message = ex.Message, inner = ex.InnerException?.Message });
            }
        }
    }
}