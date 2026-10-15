//using Microsoft.AspNetCore.Authorization;
//using Microsoft.AspNetCore.Mvc;
//using Microsoft.Extensions.Options;
//using NexgenCosysReport.Dtos.ReportDtos;
//using NexgenCosysReport.Dtos.RequestDtos.Loan.LoanAnalysisReport;
//using NexgenCosysReport.Inteface.ReportInterface;
//using NexgenCosysReport.Inteface.ServiceInterface.Common;
//using NexgenCosysReport.Inteface.ServiceInterface.Loan.LoanAnalysisReport;
//using NexgenCosysReport.Services.ReportService;
//using NexgenCosysReport.Utils.Enum;
//using NexgenCosysReport.Utils.Report;
//using System.Security.Claims;

//namespace NexgenCosysReport.Controllers.Loan.LoanAnalysisReport
//{
//    [ApiController]
//    [Route("api/[controller]")]
//    [Authorize]
//    public class LoanPortfolioController : ControllerBase
//    {
//        private readonly ILoanPortfolioRepository _repository;
//        private readonly ICommonHeaderRepository _commonHeaderRepository;
//        private readonly IJsReportService _jsReportService;
//        private readonly IWebHostEnvironment _webHostEnvironment;
//        private readonly CustomHeaderResponse _headerResponse;
//        private readonly IOptions<ReportSettings> _reportSettings;
//        private readonly ILogger<LoanPortfolioController> _logger;
//        private readonly IReportFileResponse _reportFileResponse;

//        public LoanPortfolioController(
//            ILoanPortfolioRepository repository,
//            ICommonHeaderRepository commonHeaderRepository,
//            IJsReportService jsReportService,
//            IWebHostEnvironment webHostEnvironment,
//            CustomHeaderResponse headerResponse,
//            IOptions<ReportSettings> reportSettings,
//            ILogger<LoanPortfolioController> logger,
//            IReportFileResponse reportFileResponse)
//        {
//            _repository = repository;
//            _commonHeaderRepository = commonHeaderRepository;
//            _jsReportService = jsReportService;
//            _webHostEnvironment = webHostEnvironment;
//            _headerResponse = headerResponse;
//            _reportSettings = reportSettings;
//            _logger = logger;
//            _reportFileResponse = reportFileResponse;
//        }

//        private static readonly PageSizeSetting NPageSetting = PageSizeSetting.A4Portrait;
//        private static readonly PageSizeSetting VPageSetting =
//            PageSizeSetting.Custom(300, 297, PageUnit.mm, landscape: true);

//        [HttpPost()]
//        public async Task<IActionResult> GenerateReport(
//            [FromBody] LoanPortfolioRequestDto request,
//            [FromQuery] string format = "VIEW", CancellationToken ct = default)
//        {
//            try
//            {
//                var userIdClaim = User.FindFirst("UserId")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
//                if (string.IsNullOrEmpty(userIdClaim) || !long.TryParse(userIdClaim, out var userId))
//                {
//                    return NotFound(new { success = false, StatusCode = 401, message = "Unauthorized" });
//                }

//                if (request == null || !ModelState.IsValid)
//                {
//                    return BadRequest(new { success = false, StatusCode = 400, message = "Invalid request" });
//                }



//                var reportName = "LoanPortfolio";
//                var upperFormat = format.ToUpper();
//                var isVisual = request.VisualReport;
//                var page = isVisual ? VPageSetting : NPageSetting;

//                //var reportKey = ReportUtils.GenerateReportKey(request, reportName);
//                var reportKey = ReportUtils.GenerateReportKey(
//                request, reportName, $"{(isVisual ? "VISUAL" : "NORMAL")}_{page.CacheTag}");

//                var cached = await ReportExportHelper.TryServeAsync(
//                 reportKey, upperFormat, reportName, pdfView: false,
//                 _jsReportService, _reportFileResponse, _logger, page, ct);
//                if (cached != null) return cached;

//                string? branchIdForHeader = null;
//                if (!string.IsNullOrEmpty(request.BranchIds) &&
//                    request.BranchIds != "-1" && !request.BranchIds.Contains(','))
//                {
//                    branchIdForHeader = request.BranchIds;
//                }

//                var dataTask = _repository.GetReportDataAsync(request);
//                var headerTask = _commonHeaderRepository.GetCommonHeaders(branchIdForHeader ?? "");

//                await Task.WhenAll(dataTask, headerTask);

//                var data = await dataTask;
//                var headerData = await headerTask;

//                if (!data.LoanTypeWise.Any() && !data.Particulars.Any() && !data.Outstanding.Any())
//                {
//                    return NotFound(new { success = false, StatusCode = 400, message = "No data found" });
//                }

//                var webRoot = ReportUtils.GetWebRootPath(_webHostEnvironment, _reportSettings);

//                await ReportUtils.ConvertUniqueImagesToBase64Async(
//                    headerData, nameof(CommonHeader.CompanyLogo), webRoot);

//                var reportData = new Dictionary<string, object>
//                {
//                    { "Particulars", data.Particulars ?? new List<LoanPortfolioParticularsDto>() },
//                    { "Outstanding", data.Outstanding ?? new List<LoanPortfolioOutstandingDto>() },
//                    { "LoanTypeWise", data.LoanTypeWise ?? new List<LoanPortfolioLoanTypeWiseDto>() },
//                    { "TotalRecords", data.TotalRecords },
//                    { "TotalLoans", data.TotalLoans },
//                    { "TotalLoanIssueAmount", data.TotalLoanIssueAmount },
//                    { "HeaderDataSet", headerData ?? new List<CommonHeader>() },
//                    { "BranchName", data.BranchName ?? "All" },
//                    { "Format", upperFormat },
//                    { "PageSetting", page }
//                };

//                string viewPath = isVisual
//                    ? "Views/VisualReport/Loan/LoanAnalysisReports/VLoanPortfolioReport.cshtml"
//                    : "Views/Report/Loan/LoanAnalysisReports/LoanPortfolioReport.cshtml";

//                var htmlContent = await Task.Run(() =>
//                    _jsReportService.RenderRazorToHtmlAndCacheAsync(
//                        reportKey: reportKey,
//                        reportPath: viewPath,
//                        data: reportData));

//                if (upperFormat == "VIEW")
//                {
//                    //var pdfBytes = await _jsReportService.ExportReportToFormatAsync(htmlContent, "PDF", reportKey);
//                    //var totalPages = JsReportService.CountPdfPages(pdfBytes);
//                    //var pagination = new Pagination
//                    //{
//                    //    currentPage = 1,
//                    //    totalPages = totalPages,
//                    //    pageSize = 1,
//                    //    hasNextPage = totalPages > 1,
//                    //    hasPreviousPage = false,
//                    //    totalRecord = data.TotalRecords
//                    //};

//                    //_headerResponse.SetResponseHeaders(true, 200, "Report generated successfully.");
//                    //Response.Headers.Append("X-Pagination", JsonSerializer.Serialize(pagination));
//                    //Response.Headers.Append("Content-Disposition", $"inline; filename=\"{reportName}.pdf\"");

//                    //return new FileContentResult(pdfBytes, "application/pdf");

//                    var viewHtml = await _jsReportService.ExportReportToRawHtmlAsync(
//                       htmlContent, reportKey, page, ct);
//                    _reportFileResponse.AppendPaginationHeaders(
//                        SheetHtml.CountSheets(viewHtml), data.TotalRecords);
//                    return Content(viewHtml, "text/html");
//                }
//                return await ReportExportHelper.ExportFromCacheAsync(
//                 reportKey, upperFormat,
//                 reportName,
//                 _jsReportService, _logger, page, ct);
//            }

//            catch (Exception ex)
//            {
//                _logger.LogError(ex, "Error generating LoanPortfolio report");
//                return StatusCode(500, new
//                {
//                    message = ex.Message,
//                    inner = ex.InnerException?.Message,
//                    stack = ex.StackTrace
//                });
//            }
//        }
//    }
//}




// Controllers/Loan/LoanAnalysisReports/LoanPortfolioController.cs
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
    public class LoanPortfolioController : ControllerBase
    {
        private const string ReportName = "LoanPortfolio";

        private readonly ILoanPortfolioRepository _repository;
        private readonly IReportHeaderProvider _headers;
        private readonly IJsReportService _jsReportService;
        private readonly IReportDataCache _dataCache;
        private readonly ILogger<LoanPortfolioController> _logger;
        private readonly IReportFileResponse _reportFileResponse;

        private static readonly PageSizeSetting NPageSetting = PageSizeSetting.A4Portrait;
        private static readonly PageSizeSetting VPageSetting =
            PageSizeSetting.Custom(300, 297, PageUnit.mm, landscape: true);

        public LoanPortfolioController(
            ILoanPortfolioRepository repository,
            IReportHeaderProvider headers,
            IJsReportService jsReportService,
            IReportDataCache dataCache,
            ILogger<LoanPortfolioController> logger,
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
            [FromBody] LoanPortfolioRequestDto request,
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

                // data key: every filter + user (normal and visual share it); html key: + view type + page size
                var dataKey = ReportCacheKey.ForData(
                    request, ReportName, userIdClaim, nameof(request.VisualReport));
                var reportKey = $"{dataKey}_{(isVisual ? "VISUAL" : "NORMAL")}_{page.CacheTag}";

                // HTML hit: no database, no Razor
                var cached = await ReportExportHelper.TryServeAsync(
                    reportKey, upperFormat, ReportName, pdfView: false,
                    _jsReportService, _reportFileResponse, _logger, page, ct);
                if (cached != null) return cached;

                // header and data load together; both are cached
                var headerTask = _headers.GetAsync(request.BranchIds, ct);
                var dataTask = _dataCache.GetOrLoadAsync(dataKey, async () =>
                {
                    var loaded = await _repository.GetReportDataAsync(request);
                    return loaded ?? null;
                }, ct);
                await Task.WhenAll(headerTask, dataTask);

                var data = await dataTask;
                if (data == null)
                    return NotFound(new { success = false, StatusCode = 400, message = "No data found" });

                var reportData = new Dictionary<string, object>
                {
                    { "Particulars", data.Particulars ?? new List<LoanPortfolioParticularsDto>() },
                    { "Outstanding", data.Outstanding ?? new List<LoanPortfolioOutstandingDto>() },
                    { "LoanTypeWise", data.LoanTypeWise ?? new List<LoanPortfolioLoanTypeWiseDto>() },
                    { "TotalRecords", data.TotalRecords },
                    { "TotalLoans", data.TotalLoans },
                    { "TotalLoanIssueAmount", data.TotalLoanIssueAmount },
                    { "HeaderDataSet", await headerTask },
                    { "BranchName", data.BranchName ?? "All" },
                    { "Format", upperFormat },
                    { "PageSetting", page }
                };

                var viewPath = isVisual
                    ? "Views/VisualReport/Loan/LoanAnalysisReports/VLoanPortfolioReport.cshtml"
                    : "Views/Report/Loan/LoanAnalysisReports/LoanPortfolioReport.cshtml";

                var html = await _jsReportService.RenderRazorToHtmlAndCacheAsync(
                    reportKey: reportKey, reportPath: viewPath, data: reportData, ct: ct);

                return await ReportExportHelper.CompleteAsync(
                    html, reportKey, upperFormat, ReportName, pdfView: false,
                    totalRecords: data.TotalRecords,
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

