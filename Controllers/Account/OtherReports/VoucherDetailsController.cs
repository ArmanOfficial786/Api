using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using NexgenCosysReport.Dtos.ReportDtos;
using NexgenCosysReport.Dtos.RequestDtos.Account.OtherReports;
using NexgenCosysReport.Dtos.RequestDtos.Common;
using NexgenCosysReport.Inteface.ReportInterface;
using NexgenCosysReport.Inteface.ServiceInterface.Account.OtherReports;
using NexgenCosysReport.Inteface.ServiceInterface.Common;
using NexgenCosysReport.Services.ReportService;
using NexgenCosysReport.Utils.Report;
using System.Text.Json;

namespace NexgenCosysReport.Controllers.Account.OthersReport
{
    [ApiController]
    [Route("api/account/[controller]")]
    //[Authorize]
    public class VoucherDetailsController : ControllerBase
    {
        private readonly IVoucherDetailsRepository _repository;
        private readonly ICommonHeaderRepository _commonHeaderRepository;
        private readonly IJsReportService _jsReportService;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly CustomHeaderResponse _headerResponse;
        private readonly IOptions<ReportSettings> _reportSettings;
        private readonly ILogger<VoucherDetailsController> _logger;

        private const string ViewPath = "Views/Report/Account/OtherReports/VoucherDetailsReport.cshtml";
        private const string VisualViewPath = "Views/VisualReport/VVoucherDetailsReport.cshtml";

        public VoucherDetailsController(
            IVoucherDetailsRepository repository,
            ICommonHeaderRepository commonHeaderRepository,
            IJsReportService jsReportService,
            IWebHostEnvironment webHostEnvironment,
            CustomHeaderResponse headerResponse,
            IOptions<ReportSettings> reportSettings,
            ILogger<VoucherDetailsController> logger)
        {
            _repository = repository;
            _commonHeaderRepository = commonHeaderRepository;
            _jsReportService = jsReportService;
            _webHostEnvironment = webHostEnvironment;
            _headerResponse = headerResponse;
            _reportSettings = reportSettings;
            _logger = logger;
        }

        [HttpPost]
        public async Task<IActionResult> GenerateReport(
            [FromBody] VoucherDetailsRequestDto request,
            [FromQuery] string format = "VIEW")
        {
            try
            {
                if (request == null || !ModelState.IsValid)
                {
                    return BadRequest(new { success = false, StatusCode = 400, message = "Invalid request" });
                }

                var reportName = "VoucherDetails";
                var upperFormat = format.ToUpper();
                var reportKey = ReportUtils.GenerateReportKey(request, reportName);

                ReportExportHelper.LogCacheState(upperFormat, reportKey,
                    _jsReportService.TryGetCachedHtml(reportKey, out _), _logger);

                if (upperFormat != "VIEW" && _jsReportService.TryGetCachedHtml(reportKey, out _))
                {
                    return await ReportExportHelper.ExportFromCacheAsync(
                        reportKey, upperFormat,
                        reportName,
                        _jsReportService, _logger);
                }

                // -- DB: parallel calls -------------------------------------------------
                var dataTask = _repository.GetVoucherDetailsDataAsync(request);

                string? branchIdForHeader = null;
                if (!string.IsNullOrEmpty(request.BranchIds) &&
                    request.BranchIds != "-1" && !request.BranchIds.Contains(','))
                {
                    branchIdForHeader = request.BranchIds;
                }
                var headerTask = _commonHeaderRepository.GetCommonHeaders(branchIdForHeader ?? "");

                await Task.WhenAll(dataTask, headerTask);

                var data = await dataTask;
                var headerData = await headerTask;

                if (!data.Rows.Any())
                {
                    return NotFound(new { success = false, StatusCode = 404, message = "No data found" });
                }

                var webRoot = ReportUtils.GetWebRootPath(_webHostEnvironment, _reportSettings);
                await ReportUtils.ConvertUniqueImagesToBase64Async(
                    headerData, nameof(CommonHeader.CompanyLogo), webRoot);

                var reportData = new Dictionary<string, object>
                {
                    { "Rows", data.Rows },
                    { "TotalRecords", data.TotalRecords },
                    { "TotalDebitAmount", data.TotalDebitAmount },
                    { "TotalCreditAmount", data.TotalCreditAmount },
                    { "FromDate", data.FromDateBs ?? string.Empty },
                    { "ToDate", data.ToDateBs ?? string.Empty },
                    { "BranchNames", data.BranchNames ?? "All Branches" },
                    { "OrderBy", data.OrderBy ?? string.Empty },
                    { "ViewType", data.ViewType ?? "None" },
                    { "VoucherId", data.VoucherId ?? -1 },
                    { "VoucherNo", data.VoucherNo ?? string.Empty },
                    { "HeaderDataSet", headerData },
                    { "Format", upperFormat },
                    { "VisualReport", request.VisualReport }
                };

                var viewPath = request.VisualReport ? VisualViewPath : ViewPath;

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
                _logger.LogError(ex, "Error generating Voucher Details Report");
                return StatusCode(500, new
                {
                    message = ex.Message,
                    inner = ex.InnerException?.Message,
                    stack = ex.StackTrace
                });
            }
        }
    }
}