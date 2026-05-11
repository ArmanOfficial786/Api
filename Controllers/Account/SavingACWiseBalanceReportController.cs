using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using NexgenCosysReport.Dtos.ReportDtos;
using NexgenCosysReport.Dtos.RequestDtos.Account;
using NexgenCosysReport.Dtos.RequestDtos.Common;
using NexgenCosysReport.Inteface.ReportInterface;
using NexgenCosysReport.Inteface.ServiceInterface.Account;
using NexgenCosysReport.Inteface.ServiceInterface.Common;
using NexgenCosysReport.Utils.Enum;
using NexgenCosysReport.Utils.Report;

namespace NexgenCosysReport.Controllers.Account
{
    [Route("api/[controller]")]
    [ApiController]
    public class SavingACWiseBalanceReportController : ControllerBase
    {
        private readonly IJsReportService _jsReportService;
        private readonly ICommonHeaderRepository _commonHeaderRepository;
        private readonly ISavingAcWiseBalance _savingAcWiseBalanceRepository;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly IOptions<ReportSettings> _reportSettings;
        private readonly ILogger<SavingACWiseBalanceReportController> _logger;

        // ── Page size for this specific report ────────────────────────
        private static readonly PageSizeSetting _pageSetting =
         PageSizeSetting.Custom(240, 297, PageUnit.mm, landscape: false);

        //private static readonly PageSizeSetting _pageSetting =
        //        PageSizeSetting.A3Landscape;

        public SavingACWiseBalanceReportController(IJsReportService jsReportService, ICommonHeaderRepository commonHeaderRepository, ISavingAcWiseBalance savingAcWiseBalanceRepository, IWebHostEnvironment webHostEnvironment, IOptions<ReportSettings> reportSettings, ILogger<SavingACWiseBalanceReportController> logger)
        {
            _jsReportService = jsReportService;
            _commonHeaderRepository = commonHeaderRepository;
            _savingAcWiseBalanceRepository = savingAcWiseBalanceRepository;
            _webHostEnvironment = webHostEnvironment;
            _reportSettings = reportSettings;
            _logger = logger;
        }
        [HttpPost("SavingACWiseBalanceReport")]
        public async Task<ActionResult> SavingAcWiseReport([FromBody] SavingAcWiseBalanceRequest request, [FromQuery] string format = "VIEW")
        {
            var reportName = "SavingACWiseBalanceReport";
            var upperFormat = format.ToUpper();
            var response = new GeneralResponse<ReportResponseDtos>();
            try
            {
                if (request == null || !ModelState.IsValid)
                {
                    response.IsValid = false;
                    response.StatusCode = StatusCodes.Status400BadRequest;
                    response.Message = "Invalid request data.";
                    return BadRequest(response);
                }


                var reportKey = ReportUtils.GenerateReportKey(request, reportName) + $"_{upperFormat}";

                ReportExportHelper.LogCacheState(upperFormat, reportKey,
                    _jsReportService.IsHtmlCached(reportKey), _logger);

                // -- NO DB CALL — serving from cache ---------------------------------------
                if (upperFormat != "VIEW" && _jsReportService.IsHtmlCached(reportKey))
                {
                    return await ReportExportHelper.ExportFromCacheAsync(
                        reportKey,
                        upperFormat,
                        reportName,
                        _jsReportService,
                        _logger,
                        _pageSetting
                        );
                }


                var commonHeaderTask = _commonHeaderRepository.GetCommonHeaders();
                var spDataTask = _savingAcWiseBalanceRepository.GetSavingAcWiseBalanceAsync(request);

                await Task.WhenAll(commonHeaderTask, spDataTask);
                var commonHeader = await commonHeaderTask;
                var spData = await spDataTask;

                if (!spData.Any())
                {
                    response.IsValid = false;
                    response.StatusCode = 404;
                    response.Message = "No data found";
                    return NotFound(response);
                }

                var webRoot = ReportUtils.GetWebRootPath(
                     _webHostEnvironment, _reportSettings);

                await Task.Run(() => ReportUtils.ConvertUniqueImagesToBase64Async(
                   commonHeader,
                   nameof(CommonHeader.CompanyLogo),
                   webRoot));

                var reportData = new Dictionary<string, object>
                {
                    { "HeaderDataSet",commonHeader },
                    {"SavingAcWiseBalanceDataset", spData },
                    {"Format",   upperFormat},
                    {"SameCompanyName",request.SameCompanyName},
                    { "BranchName",          request.BranchName},
                    {"TillDate",request.TillDate},

                };

                var renderRazorpage = await Task.Run(() =>
                   _jsReportService.RenderRazorToHtmlAndCacheAsync(
                       reportKey: reportKey,
                       reportPath: "Views/Report/SavingAcWiseBalance.cshtml",
                       data: reportData));

                if (upperFormat == "VIEW")
                {
                    var pdfBytes = await Task.Run(() =>
                        _jsReportService.ExportReportToFormatAsync(renderRazorpage, "PDF", reportKey, _pageSetting));

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
                            TotalRecord = 1,
                            PageSize = 1,
                            HasNextPage = false,
                            HasPreviousPage = false
                        }
                    };
                    return Ok(response);
                }



                return await ReportExportHelper.ExportFromCacheAsync(
                    reportKey, upperFormat,
                    reportName,
                    _jsReportService, _logger, _pageSetting);


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
