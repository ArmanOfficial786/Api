// Controllers/Loan/OtherReports/LoanDueInstallmentController.cs
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using NexgenCosysReport.Dtos.ReportDtos;
using NexgenCosysReport.Dtos.RequestDtos.Common;
using NexgenCosysReport.Dtos.RequestDtos.Loan.OtherReports;
using NexgenCosysReport.Inteface.ReportInterface;
using NexgenCosysReport.Inteface.ServiceInterface.Common;
using NexgenCosysReport.Inteface.ServiceInterface.Loan.OtherReports;
using NexgenCosysReport.Services.ReportService;
using NexgenCosysReport.Utils.Report;
using System.Text.Json;

namespace NexgenCosysReport.Controllers.Loan.OtherReports
{
    [ApiController]
    [Route("api/[controller]")]
    //[Authorize]
    public class LoanDueInstallmentController : ControllerBase
    {
        private readonly ILoanDueInstallmentRepository _repository;
        private readonly ICommonHeaderRepository _commonHeaderRepository;
        private readonly IJsReportService _jsReportService;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly CustomHeaderResponse _headerResponse;
        private readonly IOptions<ReportSettings> _reportSettings;
        private readonly ILogger<LoanDueInstallmentController> _logger;
        private readonly IDateConverterService _dateConverter;

        public LoanDueInstallmentController(
            ILoanDueInstallmentRepository repository,
            ICommonHeaderRepository commonHeaderRepository,
            IJsReportService jsReportService,
            IWebHostEnvironment webHostEnvironment,
            CustomHeaderResponse headerResponse,
            IOptions<ReportSettings> reportSettings,
            ILogger<LoanDueInstallmentController> logger,
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

        // POST api/LoanDueInstallment?format=VIEW
        // Body: { "fromDateBs": "2080-01-01", "toDateBs": "2080-12-30", "memberId": null, "branchIds": "1,2", "memberGroupId": "-1", "lmtPaymentDurationTypeId": -1, "orderBy": "MemberId" }
        [HttpPost()]
        public async Task<IActionResult> GenerateReport(
            [FromBody] LoanDueInstallmentRequestDto request,
            [FromQuery] string format = "VIEW")
        {
            try
            {
                //var userIdClaim = User.FindFirst("UserId")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                //if (string.IsNullOrEmpty(userIdClaim) || !long.TryParse(userIdClaim, out var userId))
                //{
                //    return NotFound(new { success = false, StatusCode = 401, message = "Unauthorized" });
                //}

                if (request == null || !ModelState.IsValid)
                {
                    return BadRequest(new { success = false, StatusCode = 400, message = "Invalid request" });
                }

                if (string.IsNullOrEmpty(request.BranchIds) || request.BranchIds == "-1")
                {
                    // Mirrors legacy: "Please select Branch Name" validation
                    return BadRequest(new { success = false, StatusCode = 400, message = "Please select Branch Name" });
                }

                // Mirrors legacy: "Please select MemberId or ALL" validation
                // If MemberId is not provided, it means ALL members (equivalent to chkAll.Checked = true)

                var reportName = "LoanDueInstallment";
                var upperFormat = format.ToUpper();

                var reportKey = ReportUtils.GenerateReportKey(request, reportName) + $"_{upperFormat}";

                ReportExportHelper.LogCacheState(upperFormat, reportKey,
                    _jsReportService.TryGetCachedHtml(reportKey, out _), _logger);

                if (upperFormat != "VIEW" && _jsReportService.TryGetCachedHtml(reportKey, out _))
                {
                    return await ReportExportHelper.ExportFromCacheAsync(
                        reportKey, upperFormat,
                        reportName,
                        _jsReportService, _logger);
                }

                string? branchIdForHeader = null;
                if (!string.IsNullOrEmpty(request.BranchIds) &&
                    request.BranchIds != "-1" && !request.BranchIds.Contains(','))
                {
                    branchIdForHeader = request.BranchIds;
                }

                var dataTask = _repository.GetReportDataAsync(request);
                var headerTask = _commonHeaderRepository.GetCommonHeaders(branchIdForHeader ?? "");

                await Task.WhenAll(dataTask, headerTask);

                var data = await dataTask;
                var headerData = await headerTask;

                if (!data.Rows.Any())
                {
                    return NotFound(new { success = false, StatusCode = 400, message = "No data found" });
                }

                var webRoot = ReportUtils.GetWebRootPath(_webHostEnvironment, _reportSettings);

                await ReportUtils.ConvertUniqueImagesToBase64Async(
                    headerData, nameof(CommonHeader.CompanyLogo), webRoot);

                var reportData = new Dictionary<string, object>
                {
                    { "Rows", data.Rows },
                    { "TotalRecords", data.TotalRecords },
                    { "TotalLoanIssueAmount", data.TotalLoanIssueAmount },
                    { "TotalPrincipleAmount", data.TotalPrincipleAmount },
                    { "TotalInterestAmount", data.TotalInterestAmount },
                    { "TotalInstallmentAmount", data.TotalInstallmentAmount },
                    { "TotalBalanceAmount", data.TotalBalanceAmount },
                    { "HeaderDataSet", headerData },
                    { "FromDate", data.FromDateBs ?? "" },
                    { "ToDate", data.ToDateBs ?? "" },
                    { "BranchNames", data.BranchNames ?? "All" },
                    { "MemberName", data.MemberName ?? "" },
                    { "MemberGroupName", data.MemberGroupName ?? "" },
                    { "PaymentDurationTypeName", data.PaymentDurationTypeName ?? "" },
                    { "OrderBy", request.OrderBy },
                    { "Format", upperFormat }
                };

                string viewPath = "Views/Report/Loan/OtherReports/LoanDueInstallmentReport.cshtml";

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
            catch (ArgumentException ex)
            {
                return BadRequest(new { success = false, StatusCode = 400, message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating LoanDueInstallment report");
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