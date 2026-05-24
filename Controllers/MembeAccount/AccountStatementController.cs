using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using NexgenCosysReport.Dtos.ReportDtos;
using NexgenCosysReport.Dtos.RequestDtos.Common;
using NexgenCosysReport.Dtos.RequestDtos.MemberAccount;
using NexgenCosysReport.Inteface.ReportInterface;
using NexgenCosysReport.Inteface.ServiceInterface.Common;
using NexgenCosysReport.Inteface.ServiceInterface.Member;
using NexgenCosysReport.Inteface.ServiceInterface.MemberAccount;
using NexgenCosysReport.Services.ReportService;
using NexgenCosysReport.Utils.Report;

namespace NexgenCosysReport.Controllers.MembeAccount
{
    [ApiController]
    [Route("api/[controller]")]
    public class AccountStatementController : ControllerBase
    {
        private readonly IAccountStatement _accountStatementService;
        private readonly IMemberDetail _memberDetail;
        private readonly ICommonHeaderRepository _commonHeaderRepository;
        private readonly IJsReportService _jsReportService;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly IOptions<ReportSettings> _reportSettings;
        private readonly ILogger<AccountStatementController> _logger;

        public AccountStatementController(
            IAccountStatement accountStatementService,
            IMemberDetail memberDetail,
            IJsReportService jsReportService,
            IWebHostEnvironment webHostEnvironment,
            IOptions<ReportSettings> reportSettings,
            ILogger<AccountStatementController> logger,
            ICommonHeaderRepository commonHeaderRepository)
        {
            _accountStatementService = accountStatementService;
            _memberDetail = memberDetail;
            _jsReportService = jsReportService;
            _webHostEnvironment = webHostEnvironment;
            _reportSettings = reportSettings;
            _logger = logger;
            _commonHeaderRepository = commonHeaderRepository;
        }

        [HttpPost("AccountStatementReport")]
        public async Task<ActionResult<GeneralResponse<ReportResponseDtos>>> GenerateReport(
            [FromBody] AccountStatementRequest request,
            [FromQuery] string format = "VIEW")
        {
            try
            {
                var reportName = "AccountStatement";
                var upperFormat = format.ToUpper();
                var response = new GeneralResponse<ReportResponseDtos>();

                if (request == null || !ModelState.IsValid)
                {
                    response.isValid = false;
                    response.statusCode = 400;
                    response.message = "Invalid request";
                    return BadRequest(response);
                }

                var reportKey = ReportUtils.GenerateReportKey(request, reportName) + $"_{upperFormat}";

                ReportExportHelper.LogCacheState(upperFormat, reportKey,
                    _jsReportService.TryGetCachedHtml(reportKey, out _), _logger);


                // -- NO DB CALL — serving from cache ---------------------------------------
                if (upperFormat != "VIEW" && _jsReportService.TryGetCachedHtml(reportKey, out _))
                {
                    return await ReportExportHelper.ExportFromCacheAsync(
                        reportKey, upperFormat,
                        reportName,
                        _jsReportService, _logger);
                }

                // -- DB: All three calls in parallel -------------------
                var statementTask = _accountStatementService
                                        .GetAccountStatementTypeAsync(request);

                var balanceTask = _accountStatementService
                                        .GetCashAndBankBalanceOpeningClosingAsync(request);

                //var headerTask = _memberDetail.GetCommonHeaders();

                string? branchIdForHeader = null;
                if (
                   !request.SameCompanyName &&
                   !string.IsNullOrEmpty(request.BranchSelected) &&
                   request.BranchSelected != "-1" &&
                   !request.BranchSelected.Contains(','))
                {
                    branchIdForHeader = request.BranchSelected;
                }
                var headerTask = _commonHeaderRepository.GetCommonHeaders(branchIdForHeader ?? "");

                await Task.WhenAll(statementTask, balanceTask, headerTask);

                var statementData = await statementTask;
                var balanceData = await balanceTask;
                var headerData = await headerTask;

                if (!statementData.Any())
                {
                    response.isValid = false;
                    response.statusCode = 404;
                    response.message = "No data found";
                    return NotFound(response);
                }

                var webRoot = ReportUtils.GetWebRootPath(
                    _webHostEnvironment, _reportSettings);


                await Task.Run(() => ReportUtils.ConvertUniqueImagesToBase64Async(
                    headerData,
                    nameof(CommonHeader.CompanyLogo),
                    webRoot));



                var reportData = new Dictionary<string, object>
                {
                    { "AccountStatementDataSet", statementData           },
                    { "CashBankBalanceDataSet",  balanceData             },
                    { "HeaderDataSet",           headerData              },
                    { "TotalRecords",            statementData.Count     },
                    { "ReportType",              request.ReportType      },
                    { "TransactionType",         request.TransactionType },
                    { "BranchName",              request.BranchName      },
                    { "FromDate",                request.FromDate        },
                    { "ToDate",                  request.ToDate          },
                    { "Format",                  upperFormat             },
                    { "SameCompanyName",         request.SameCompanyName }
                };

                // -- Razor render --------------------------------------

                var htmlContent = await Task.Run(() =>
                    _jsReportService.RenderRazorToHtmlAndCacheAsync(
                        reportKey: reportKey,
                        reportPath: "Views/Report/AccountStatementReport.cshtml",
                        data: reportData));



                if (upperFormat == "VIEW")
                {
                    // -- jsreport PDF ----------------------------------

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
                            totalRecord = statementData.Count(),
                            pageSize = 1,
                            hasNextPage = totalPages > 1,
                            hasPreviousPage = false
                        }
                    };
                    return Ok(response);
                }



                return await ReportExportHelper.ExportFromCacheAsync(
                    reportKey, upperFormat,
                    reportName,
                    _jsReportService, _logger);
            }
            catch (Exception ex)
            {

                return StatusCode(500, new
                {
                    message = ex.Message,
                    inner = ex.InnerException?.Message,
                    stack = ex.StackTrace
                });
            }
            ;
        }
    }
}