using NexgenCosysReport.Dtos.ReportDtos;
using NexgenCosysReport.Dtos.RequestDtos;
using NexgenCosysReport.Dtos.RequestDtos.Common;
using NexgenCosysReport.Inteface.ReportInterface;
using NexgenCosysReport.Inteface.ServiceInterface;
using NexgenCosysReport.Utils.Report;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

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
                    response.IsValid = false;
                    response.StatusCode = 400;
                    response.Message = "Invalid request";
                    return BadRequest(response);
                }
                    

                
                var reportKey = ReportUtils.GenerateReportKey(request, reportName) + $"_{upperFormat}"; 

                ReportExportHelper.LogCacheState(upperFormat, reportKey,
                    _jsReportService.IsHtmlCached(reportKey), _logger);

                // -- NO DB CALL — serving from cache ---------------------------------------
                if (upperFormat != "VIEW" && _jsReportService.IsHtmlCached(reportKey))
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
                    response.IsValid = false;
                    response.StatusCode = 404;
                    response.Message = "No data found";
                    return NotFound(response);
                }

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
                   
                    response.IsValid = true;
                    response.StatusCode = 200;
                    response.Message = "Report generated successfully";
                    response.Data = new ReportResponseDtos
                    {
                        PdfData = Convert.ToBase64String(pdfBytes),
                        ReportName = reportName,
                        Pagination = new Pagination
                        {
                            CurrentPage = 1,
                            TotalPages = 1,
                            TotalRecord =1,  
                            PageSize =   1,
                            HasNextPage = false,
                            HasPreviousPage = false
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
                    success = false,
                    message = "An error occurred while processing your request.",
                    error = ex.Message
                });
            };
        }
    }
}