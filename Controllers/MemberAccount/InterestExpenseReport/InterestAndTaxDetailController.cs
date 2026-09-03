//// Controllers/MemberAccount/InterestExpenseReport/InterestAndTaxDetailController.cs
//using Microsoft.AspNetCore.Authorization;
//using Microsoft.AspNetCore.Mvc;
//using Microsoft.Extensions.Options;
//using NexgenCosysReport.Dtos.ReportDtos;
//using NexgenCosysReport.Dtos.RequestDtos.Common;
//using NexgenCosysReport.Dtos.RequestDtos.MemberAccount.InterestExpenseReport;
//using NexgenCosysReport.Inteface.ReportInterface;
//using NexgenCosysReport.Inteface.ServiceInterface.Common;
//using NexgenCosysReport.Inteface.ServiceInterface.MemberAccount.InterestExpenseReportInterface;
//using NexgenCosysReport.Services.ReportService;
//using NexgenCosysReport.Utils.Report;
//using System.Security.Claims;
//using System.Text.Json;

//namespace NexgenCosysReport.Controllers.MemberAccount.InterestExpenseReport
//{
//    [ApiController]
//    [Route("api/[controller]")]
//    [Authorize]
//    public class InterestAndTaxDetailController : ControllerBase
//    {
//        private readonly IInterestAndTaxDetailRepository _repository;
//        private readonly ICommonHeaderRepository _commonHeaderRepository;
//        private readonly IJsReportService _jsReportService;
//        private readonly IWebHostEnvironment _webHostEnvironment;
//        private readonly CustomHeaderResponse _headerResponse;
//        private readonly IOptions<ReportSettings> _reportSettings;
//        private readonly ILogger<InterestAndTaxDetailController> _logger;
//        private readonly IDateConverterService _dateConverter;

//        public InterestAndTaxDetailController(
//            IInterestAndTaxDetailRepository repository,
//            ICommonHeaderRepository commonHeaderRepository,
//            IJsReportService jsReportService,
//            IWebHostEnvironment webHostEnvironment,
//            CustomHeaderResponse headerResponse,
//            IOptions<ReportSettings> reportSettings,
//            ILogger<InterestAndTaxDetailController> logger,
//            IDateConverterService dateConverter)
//        {
//            _repository = repository;
//            _commonHeaderRepository = commonHeaderRepository;
//            _jsReportService = jsReportService;
//            _webHostEnvironment = webHostEnvironment;
//            _headerResponse = headerResponse;
//            _reportSettings = reportSettings;
//            _logger = logger;
//            _dateConverter = dateConverter;
//        }

//        [HttpPost()]
//        public async Task<IActionResult> GenerateReport(
//            [FromBody] InterestAndTaxDetailRequestDto request,
//            [FromQuery] string format = "VIEW")
//        {
//            try
//            {
//                // Extract userId from JWT (converted from Page_Load)
//                var userIdClaim = User.FindFirst("UserId")?.Value
//                                  ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
//                if (string.IsNullOrEmpty(userIdClaim) || !long.TryParse(userIdClaim, out var userId))
//                {
//                    return NotFound(new { success = false, StatusCode = 401, message = "Unauthorized" });
//                }


//                var reportName = "InterestAndTaxDetail";
//                var upperFormat = format.ToUpper();
//                if (request == null || !ModelState.IsValid)
//                {
//                    return NotFound(new { success = false, StatusCode = 400, message = "Invalid request" });
//                }

//                var reportKey = ReportUtils.GenerateReportKey(request, reportName);

//                // Check cache
//                ReportExportHelper.LogCacheState(upperFormat, reportKey,
//                    _jsReportService.TryGetCachedHtml(reportKey, out _), _logger);

//                if (upperFormat != "VIEW" && _jsReportService.TryGetCachedHtml(reportKey, out _))
//                {
//                    return await ReportExportHelper.ExportFromCacheAsync(
//                        reportKey, upperFormat,
//                        reportName,
//                        _jsReportService, _logger);
//                }

//                // Get report data
//                var dataTask = _repository.GetReportDataAsync(request);

//                var headerTask = _commonHeaderRepository.GetCommonHeaders();

//                await Task.WhenAll(dataTask, headerTask);

//                var data = await dataTask;
//                var headerData = await headerTask;

//                if (!data.Rows.Any())
//                {
//                    return NotFound(new { success = false, StatusCode = 400, message = "No data found" });
//                }

//                var webRoot = ReportUtils.GetWebRootPath(_webHostEnvironment, _reportSettings);
//                await Task.Run(() => ReportUtils.ConvertUniqueImagesToBase64Async(
//                    headerData, nameof(CommonHeader.CompanyLogo), webRoot));

//                // Build report data
//                var reportData = new Dictionary<string, object>
//                {
//                    { "Rows", data.Rows },
//                    { "TotalRecords", data.TotalRecords },
//                    { "TotalInterest", data.TotalInterest },
//                    { "TotalTax", data.TotalTax },
//                    { "TotalNetAmount", data.TotalNetAmount },
//                    { "HeaderDataSet", headerData },
//                    { "FromDate", request.FromDateBs },
//                    { "ToDate", request.ToDateBs },
//                    { "BranchNames", request.BranchName },
//                    { "OrderBy", request.OrderBy },
//                    { "MemberId", request.MemberId },
//                    { "MemberName", request.MemberName },
//                    { "Format", upperFormat },
//                    { "VisualReport", request.VisualReport },
//                    { "TotalDepositTypes", data.TotalDepositTypes }
//                };

//                // Render view
//                string viewPath = request.VisualReport
//                    ? "Views/VisualReport/VInterestAndTaxDetailReport.cshtml"
//                    : "Views/Report/MemberAC/InterestAndTaxDetailReport.cshtml";

//                var htmlContent = await Task.Run(() =>
//                    _jsReportService.RenderRazorToHtmlAndCacheAsync(
//                        reportKey: reportKey,
//                        reportPath: viewPath,
//                        data: reportData));

//                if (upperFormat == "VIEW")
//                {
//                    var pdfBytes = await _jsReportService.ExportReportToFormatAsync(htmlContent, "PDF", reportKey);
//                    var totalPages = JsReportService.CountPdfPages(pdfBytes);
//                    var pagination = new Pagination
//                    {
//                        currentPage = 1,
//                        totalPages = totalPages,
//                        pageSize = 1,
//                        hasNextPage = totalPages > 1,
//                        hasPreviousPage = false,
//                        totalRecord = data.Rows.Count
//                    };

//                    _headerResponse.SetResponseHeaders(true, 200, "Report generated successfully.");
//                    Response.Headers.Append("X-Pagination", JsonSerializer.Serialize(pagination));
//                    Response.Headers.Append("Content-Disposition", $"inline; filename=\"{reportName}.pdf\"");

//                    return new FileContentResult(pdfBytes, "application/pdf");
//                }


//                return await ReportExportHelper.ExportFromCacheAsync(
//                    reportKey, upperFormat,
//                    reportName,
//                    _jsReportService, _logger);
//            }
//            catch (Exception ex)
//            {
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






// Controllers/MemberAccount/InterestExpenseReport/InterestAndTaxDetailController.cs
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using NexgenCosysReport.Dtos.ReportDtos;
using NexgenCosysReport.Dtos.RequestDtos.Common;
using NexgenCosysReport.Dtos.RequestDtos.MemberAccount.InterestExpenseReport;
using NexgenCosysReport.Inteface.ReportInterface;
using NexgenCosysReport.Inteface.ServiceInterface.Common;
using NexgenCosysReport.Inteface.ServiceInterface.MemberAccount.InterestExpenseReportInterface;
using NexgenCosysReport.Services.ReportService;
using NexgenCosysReport.Utils.Report;
using System.Text.Json;

namespace NexgenCosysReport.Controllers.MemberAccount.InterestExpenseReport
{
    [ApiController]
    [Route("api/[controller]")]
    //[Authorize]
    public class InterestAndTaxDetailController : ControllerBase
    {
        private readonly IInterestAndTaxDetailRepository _repository;
        private readonly ICommonHeaderRepository _commonHeaderRepository;
        private readonly IJsReportService _jsReportService;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly CustomHeaderResponse _headerResponse;
        private readonly IOptions<ReportSettings> _reportSettings;
        private readonly ILogger<InterestAndTaxDetailController> _logger;
        private readonly IDateConverterService _dateConverter;

        public InterestAndTaxDetailController(
            IInterestAndTaxDetailRepository repository,
            ICommonHeaderRepository commonHeaderRepository,
            IJsReportService jsReportService,
            IWebHostEnvironment webHostEnvironment,
            CustomHeaderResponse headerResponse,
            IOptions<ReportSettings> reportSettings,
            ILogger<InterestAndTaxDetailController> logger,
            IDateConverterService dateConverter)
        {
            _repository = repository;
            _commonHeaderRepository = commonHeaderRepository;
            _jsReportService = jsReportService;
            _webHostEnvironment = webHostEnvironment;
            _headerResponse = headerResponse;
            _reportSettings = reportSettings;
            _logger = logger;
            _dateConverter = dateConverter;
        }

        [HttpPost]
        public async Task<IActionResult> GenerateReport(
            [FromBody] InterestAndTaxDetailRequestDto request,
            [FromQuery] string format = "VIEW")
        {
            try
            {
                // Extract userId from JWT
                //var userIdClaim = User.FindFirst("UserId")?.Value
                //                  ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                //if (string.IsNullOrEmpty(userIdClaim) || !long.TryParse(userIdClaim, out var userId))
                //{
                //    return Unauthorized(new { success = false, StatusCode = 401, message = "Unauthorized" });
                //}

                var reportName = "InterestAndTaxDetail";
                var upperFormat = format.ToUpper();

                // Validate request
                if (request == null)
                {
                    return BadRequest(new { success = false, StatusCode = 400, message = "Invalid request" });
                }

                if (string.IsNullOrEmpty(request.FromDateBs) || string.IsNullOrEmpty(request.ToDateBs))
                {
                    return BadRequest(new { success = false, StatusCode = 400, message = "From Date and To Date are required" });
                }

                var reportKey = ReportUtils.GenerateReportKey(request, reportName);

                // Check cache
                ReportExportHelper.LogCacheState(upperFormat, reportKey,
                    _jsReportService.TryGetCachedHtml(reportKey, out _), _logger);

                if (upperFormat != "VIEW" && _jsReportService.TryGetCachedHtml(reportKey, out _))
                {
                    return await ReportExportHelper.ExportFromCacheAsync(
                        reportKey, upperFormat,
                        reportName,
                        _jsReportService, _logger);
                }

                // Get report data
                var data = await _repository.GetReportDataAsync(request);

                if (!data.Rows.Any())
                {
                    return NotFound(new { success = false, StatusCode = 400, message = "No data found" });
                }

                var headerData = await _commonHeaderRepository.GetCommonHeaders();

                var webRoot = ReportUtils.GetWebRootPath(_webHostEnvironment, _reportSettings);
                await Task.Run(() => ReportUtils.ConvertUniqueImagesToBase64Async(
                    headerData, nameof(CommonHeader.CompanyLogo), webRoot));

                // Build report data
                var reportData = new Dictionary<string, object>
                {
                    { "Rows", data.Rows },
                    { "TotalRecords", data.TotalRecords },
                    { "TotalInterest", data.TotalInterest },
                    { "TotalTax", data.TotalTax },
                    { "TotalNetAmount", data.TotalNetAmount },
                    { "HeaderDataSet", headerData },
                    { "FromDate", request.FromDateBs },
                    { "ToDate", request.ToDateBs },
                    { "BranchNames", data.BranchNames ?? "All Branches" },
                    { "OrderBy", request.OrderBy },
                    { "MemberId", data.MemberId },
                    { "MemberName", data.MemberName },
                    { "Format", upperFormat },
                    { "VisualReport", request.VisualReport },
                    { "TotalDepositTypes", data.TotalDepositTypes },
                    { "GeneratedOn", DateTime.Now.ToString("yyyy/MM/dd HH:mm") }
                };

                // Render view
                string viewPath = request.VisualReport
                    ? "Views/VisualReport/VInterestAndTaxDetailReport.cshtml"
                    : "Views/Report/MemberAC/InterestExpenseReport/InterestAndTaxDetailReport.cshtml";

                var htmlContent = await Task.Run(() =>
                    _jsReportService.RenderRazorToHtmlAndCacheAsync(
                        reportKey: reportKey,
                        reportPath: viewPath,
                        data: reportData));

                if (upperFormat == "VIEW")
                {
                    var pdfBytes = await _jsReportService.ExportReportToFormatAsync(htmlContent, "PDF", reportKey);
                    var totalPages = JsReportService.CountPdfPages(pdfBytes);
                    var pagination = new Pagination
                    {
                        currentPage = 1,
                        totalPages = totalPages,
                        pageSize = 1,
                        hasNextPage = totalPages > 1,
                        hasPreviousPage = false,
                        totalRecord = data.Rows.Count
                    };

                    _headerResponse.SetResponseHeaders(true, 200, "Report generated successfully.");
                    Response.Headers.Append("X-Pagination", JsonSerializer.Serialize(pagination));
                    Response.Headers.Append("Content-Disposition", $"inline; filename=\"{reportName}.pdf\"");

                    return new FileContentResult(pdfBytes, "application/pdf");
                }

                return await ReportExportHelper.ExportFromCacheAsync(
                    reportKey, upperFormat,
                    reportName,
                    _jsReportService, _logger);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating Interest and Tax Detail Report");
                return StatusCode(500, new
                {
                    success = false,
                    message = ex.Message,
                    inner = ex.InnerException?.Message,
                    stack = ex.StackTrace
                });
            }
        }
    }
}