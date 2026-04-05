using JsSampleReport.Dtos.ReportDtos;
using JsSampleReport.Dtos.RequestDtos;
using JsSampleReport.Inteface.ReportInterface;
using JsSampleReport.Inteface.ServiceInterface;
using JsSampleReport.Utils.Report;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Diagnostics;

namespace JsSampleReport.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AccountStatementController : ControllerBase
    {
        private readonly IAccountStatement _accountStatementService;
        private readonly IMemberDetail _memberDetail;
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
            ILogger<AccountStatementController> logger)
        {
            _accountStatementService = accountStatementService;
            _memberDetail = memberDetail;
            _jsReportService = jsReportService;
            _webHostEnvironment = webHostEnvironment;
            _reportSettings = reportSettings;
            _logger = logger;
        }

        [HttpPost("AccountStatementReport")]
        public async Task<ActionResult<GeneralResponse<ReportResponseDtos>>> GenerateReport(
            [FromBody] AccountStatementRequest request,
            [FromQuery] string format = "VIEW")
        {
           

            try
            {
                var response = new GeneralResponse<ReportResponseDtos>();
                if (request == null || !ModelState.IsValid)
                {
                    response.IsValid = false;
                    response.StatusCode = 400;
                    response.Message = "Invalid request";
                    return BadRequest(response);
                }
                    //return BadRequest(new { success = false, message = "Invalid request" });

                var upperFormat = format.ToUpper();
                var reportKey = ReportUtils.GenerateReportKey(request, "AccountStatement");

                ReportExportHelper.LogCacheState(upperFormat, reportKey,
                    _jsReportService.IsCached(reportKey), _logger);

                // ── EXPORT PATH ───────────────────────────────────────
                if (upperFormat != "VIEW" && _jsReportService.IsCached(reportKey))
                {
                    _logger.LogInformation("✅ NO DB CALL — serving from cache");
                    return await ReportExportHelper.ExportFromCacheAsync(
                        reportKey, upperFormat,
                        "AccountStatementReport",
                        _jsReportService, _logger);
                }

                // ── DB: All three calls in parallel ───────────────────
           

                var statementTask = _accountStatementService
                                        .GetAccountStatementTypeAsync(request);

                var balanceTask = _accountStatementService
                                        .GetCashAndBankBalanceOpeningClosingAsync(request);

                var headerTask = _memberDetail.GetCommonHeaders();

                await Task.WhenAll(statementTask, balanceTask, headerTask);

             

                var statementData = await statementTask;
                var balanceData = await balanceTask;
                var headerData = await headerTask;

                _logger.LogInformation(
                    $"⏱️ Records            : Statement={statementData.Count}" +
                    $" | Balance={balanceData.Count}" +
                    $" | Headers={headerData.Count}");

                if (!statementData.Any())
                {
                    response.IsValid = false;
                    response.StatusCode = 404;
                    response.Message = "No data found";
                    return NotFound(response);
                }
                    //return NotFound(new { success = false, message = "No data found" });

                var webRoot = ReportUtils.GetWebRootPath(
                    _webHostEnvironment, _reportSettings, _logger);

          
                await Task.Run(() => ReportUtils.ConvertUniqueImagesToBase64Async(
                    headerData,
                    nameof(CommonHeader.CompanyLogo),
                    webRoot, _logger));
              
             

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
                    { "ToDate",                  request.ToDate          }
                };

                // ── Razor render ──────────────────────────────────────
            
                var htmlContent = await Task.Run(() =>
                    _jsReportService.RenderAndCacheReportAsync(
                        reportKey: reportKey,
                        reportPath: "Views/Report/AccountStatementReport.cshtml",
                        data: reportData));
              
              

                if (upperFormat == "VIEW")
                {
                    // ── jsreport PDF ──────────────────────────────────
                   
                    var pdfBytes = await Task.Run(() =>
                        _jsReportService.GenerateReportFromHtmlAsync(htmlContent, "PDF"));
                

                   

                    //return Ok(new
                    //{
                    //    success = true,
                    //    pdfData = Convert.ToBase64String(pdfBytes),
                    //    reportName = "Account Statement Report"
                    //});

                    response.IsValid = true;
                    response.StatusCode = 200;
                    response.Message = "Report generated successfully";
                    response.Data = new ReportResponseDtos
                    {
                        PdfData = Convert.ToBase64String(pdfBytes),
                        ReportName = "MemberIdCard Report",
                        Pagination = new Pagination
                        {
                            CurrentPage = 1,//request.currentPage,
                            TotalPages = 1,
                            TotalRecord =1,  //memberIdCardData.Count,
                            PageSize =   1,
                            HasNextPage = false,
                            HasPreviousPage = false
                        }
                    };
                    return Ok(response);
                }

           

                return await ReportExportHelper.ExportFromCacheAsync(
                    reportKey, upperFormat,
                    "AccountStatementReport",
                    _jsReportService, _logger);
            }
            catch (Exception ex)
            {
             
                return StatusCode(500, new
                {
                    success = false,
                    message = "An error occurred while processing your request.",
                    error = ex.Message
                });
            }
        }
    }
}