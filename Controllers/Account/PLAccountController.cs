using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using NexgenCosysReport.Dtos.ReportDtos;
using NexgenCosysReport.Dtos.RequestDtos.Account;
using NexgenCosysReport.Dtos.RequestDtos.Common;
using NexgenCosysReport.Inteface.ReportInterface;
using NexgenCosysReport.Inteface.ServiceInterface.Account;
using NexgenCosysReport.Inteface.ServiceInterface.Common;
using NexgenCosysReport.Services.ReportService;
using NexgenCosysReport.Utils.Report;

namespace NexgenCosysReport.Controllers.Account
{
    [ApiController]
    [Route("api/[controller]")]
    public class PLAccountController : ControllerBase
    {
        private readonly IPLAccount _plAccountService;
        private readonly ICommonHeaderRepository _commonHeaderRepository;
        private readonly IJsReportService _jsReportService;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly IOptions<ReportSettings> _reportSettings;
        private readonly ILogger<PLAccountController> _logger;

        public PLAccountController(
            IPLAccount plAccountService,
            ICommonHeaderRepository commonHeaderRepository,
            IJsReportService jsReportService,
            IWebHostEnvironment webHostEnvironment,
            IOptions<ReportSettings> reportSettings,
            ILogger<PLAccountController> logger)
        {
            _plAccountService = plAccountService;
            _commonHeaderRepository = commonHeaderRepository;
            _jsReportService = jsReportService;
            _webHostEnvironment = webHostEnvironment;
            _reportSettings = reportSettings;
            _logger = logger;
        }

        [HttpPost("GenerateReport")]
        public async Task<ActionResult<GeneralResponse<ReportResponseDtos>>> GenerateReport(
            [FromBody] PLAccountRequest request,
            [FromQuery] string format = "VIEW")
        {
            try
            {
                var reportName = "PLAccount";
                var upperFormat = format.ToUpper();
                var response = new GeneralResponse<ReportResponseDtos>();

                if (request == null || string.IsNullOrEmpty(request.FromDate) || string.IsNullOrEmpty(request.ToDate))
                {
                    response.isValid = false;
                    response.statusCode = 400;
                    response.message = "FromDate and ToDate are required.";
                    return BadRequest(response);
                }

                var reportKey = ReportUtils.GenerateReportKey(request, reportName) + $"_{upperFormat}";

                ReportExportHelper.LogCacheState(upperFormat, reportKey,
                    _jsReportService.TryGetCachedHtml(reportKey, out _), _logger);

                if (upperFormat != "VIEW" && _jsReportService.TryGetCachedHtml(reportKey, out _))
                {
                    return await ReportExportHelper.ExportFromCacheAsync(
                        reportKey, upperFormat, reportName,
                        _jsReportService, _logger);
                }

                // Fetch data and header
                var dataTask = _plAccountService.GetPLAccountReport(request);
                string? branchIdForHeader = null;
                if (!request.SameCompanyName && !string.IsNullOrEmpty(request.BranchIds) &&
                    request.BranchIds != "-1" && !request.BranchIds.Contains(','))
                {
                    branchIdForHeader = request.BranchIds;
                }
                var headerTask = _commonHeaderRepository.GetCommonHeaders(branchIdForHeader ?? "");

                await Task.WhenAll(dataTask, headerTask);

                var data = await dataTask;
                var headerData = await headerTask;

                // Determine if data is empty
                bool hasData = false;
                if (data is PLAccountHorizontalData hData)
                    hasData = hData.IncomeRows.Any() || hData.ExpenseRows.Any();
                else if (data is PLAccountVerticalData vData)
                    hasData = vData.Rows.Any();

                if (!hasData)
                {
                    response.isValid = false;
                    response.statusCode = 404;
                    response.message = "No data found for the given criteria.";
                    return NotFound(response);
                }

                var webRoot = ReportUtils.GetWebRootPath(_webHostEnvironment, _reportSettings);

                await Task.Run(() => ReportUtils.ConvertUniqueImagesToBase64Async(
                    headerData,
                    nameof(CommonHeader.CompanyLogo),
                    webRoot));

                var reportData = new Dictionary<string, object>
                {
                    { "PLData", data },
                    { "DisplayType", request.DisplayType },
                    { "IsNepali", request.IsNepaliReport },
                    { "HeaderDataSet", headerData },
                    { "FromDate", request.FromDate },
                    { "ToDate", request.ToDate },
                    { "BranchName", request.BranchName },
                    { "ReportType", request.ReportType },
                    { "OrderBy", request.OrderBy },
                    { "Format", upperFormat }
                };

                var htmlContent = await Task.Run(() =>
                    _jsReportService.RenderRazorToHtmlAndCacheAsync(
                        reportKey: reportKey,
                        reportPath: request.VisualReport
                            ? "Views/VisualReport/VPLAccountReport.cshtml"
                            : "Views/Report/Account/PLAccountReport.cshtml",
                        data: reportData));

                if (upperFormat == "VIEW")
                {
                    var pdfBytes = await Task.Run(() =>
                        _jsReportService.ExportReportToFormatAsync(htmlContent, "PDF", reportKey));
                    var totalPages = JsReportService.CountPdfPages(pdfBytes);
                    response.isValid = true;
                    response.statusCode = 200;
                    response.message = "Report generated successfully";
                    response.data = new ReportResponseDtos
                    {
                        pdfData = Convert.ToBase64String(pdfBytes),
                        reportName = reportName,
                        pagination = new Pagination
                        {
                            currentPage = 1,
                            totalPages = totalPages,
                            totalRecord = (data is PLAccountHorizontalData h) ? h.IncomeRows.Count + h.ExpenseRows.Count : (data as PLAccountVerticalData)?.Rows.Count ?? 0,
                            pageSize = 1,
                            hasNextPage = totalPages > 1,
                            hasPreviousPage = false
                        }
                    };
                    return Ok(response);
                }

                return await ReportExportHelper.ExportFromCacheAsync(
                    reportKey, upperFormat, reportName,
                    _jsReportService, _logger);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "PLAccount report generation failed");
                return StatusCode(500, new { message = ex.Message, inner = ex.InnerException?.Message, stack = ex.StackTrace });
            }
        }
    }
}