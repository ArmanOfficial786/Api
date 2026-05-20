using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using NexgenCosysReport.Dtos.ReportDtos;
using NexgenCosysReport.Dtos.RequestDtos.Account;
using NexgenCosysReport.Dtos.RequestDtos.Common;
using NexgenCosysReport.Inteface.ReportInterface;
using NexgenCosysReport.Inteface.ServiceInterface.Account;
using NexgenCosysReport.Inteface.ServiceInterface.Common;
using NexgenCosysReport.Services.ReportService;
using NexgenCosysReport.Utils.Enum;
using NexgenCosysReport.Utils.Report;
using System.Text.Json;


namespace NexgenCosysReport.Controllers.Account
{
    [ApiController]
    [Route("api/[controller]")]

    public class SavingACWiseBalanceReportController : ControllerBase
    {
        private readonly IJsReportService _jsReportService;
        private readonly ICommonHeaderRepository _commonHeaderRepository;
        private readonly ISavingAcWiseBalance _savingAcWiseBalanceRepository;
        private readonly CustomHeaderResponse _headerResponse;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly IOptions<ReportSettings> _reportSettings;
        private readonly ILogger<SavingACWiseBalanceReportController> _logger;

        // ── Page size for this specific report ────────────────────────
        private static readonly PageSizeSetting _pageSetting =
         PageSizeSetting.Custom(240, 297, PageUnit.mm, landscape: false);

        //private static readonly PageSizeSetting _pageSetting =
        //        PageSizeSetting.A3Landscape;

        public SavingACWiseBalanceReportController(IJsReportService jsReportService, ICommonHeaderRepository commonHeaderRepository, ISavingAcWiseBalance savingAcWiseBalanceRepository, IWebHostEnvironment webHostEnvironment, IOptions<ReportSettings> reportSettings, ILogger<SavingACWiseBalanceReportController> logger, CustomHeaderResponse headerResponse)
        {
            _jsReportService = jsReportService;
            _commonHeaderRepository = commonHeaderRepository;
            _savingAcWiseBalanceRepository = savingAcWiseBalanceRepository;
            _webHostEnvironment = webHostEnvironment;
            _reportSettings = reportSettings;
            _logger = logger;
            _headerResponse = headerResponse;
        }
        [HttpPost]
        public async Task<ActionResult> SavingAcWiseReport([FromBody] SavingAcWiseBalanceRequest request, [FromQuery] string format = "VIEW")
        {
            var reportName = "SavingACWiseBalanceReport";
            var upperFormat = format.ToUpper();
            var response = new GeneralResponse<ReportResponseDtos>();
            try
            {
                if (request == null || !ModelState.IsValid)
                {
                    response.isValid = false;
                    response.statusCode = StatusCodes.Status400BadRequest;
                    response.message = "Invalid request data.";
                    return BadRequest(response);
                }

                // ── Normalize filters: 0 or negative → -1 (all) ──────────────────────
                request.DepositId = request.DepositId <= 0 ? -1 : request.DepositId;
                request.CollectorId = request.CollectorId <= 0 ? -1 : request.CollectorId;
                request.MemberGroupId = request.MemberGroupId <= 0 ? -1 : request.MemberGroupId;
                request.CollectionCenterId = request.CollectionCenterId <= 0 ? -1 : request.CollectionCenterId;
                if (request.BranchSelected == "0") request.BranchSelected = "-1";




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
                    response.isValid = false;
                    response.statusCode = 404;
                    response.message = "No data found";
                    return NotFound(response);
                }

                if (spData.Count > 0)
                {
                    // Multiply to reach target row count (e.g., 1500)
                    int targetRows = 50000;
                    int repeatCount = (int)Math.Ceiling((double)targetRows / spData.Count);
                    spData = Enumerable.Repeat(spData, repeatCount)
                                       .SelectMany(x => x)
                                       .Take(targetRows)
                                       .ToList();
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


                    // Log PDF size
                    var fileSizeBytes = pdfBytes.Length;
                    var fileSizeKB = fileSizeBytes / 1024.0;
                    var fileSizeMB = fileSizeKB / 1024.0;

                    _logger.LogInformation(
                        "Generated PDF Size: {Bytes} bytes ({KB:F2} KB, {MB:F2} MB)",
                        fileSizeBytes,
                        fileSizeKB,
                        fileSizeMB
                    );

                    var totalPages = JsReportService.CountPdfPages(pdfBytes);
                    var pagination = new Pagination
                    {
                        currentPage = 1,
                        totalPages = totalPages,
                        totalRecord = spData.Count(),
                        pageSize = 1,
                        hasNextPage = totalPages > 1,
                        hasPreviousPage = false
                    };
                    _headerResponse.SetResponseHeaders(true, 200, "Report generated successfully.");
                    Response.Headers.Append("X-Pagination", JsonSerializer.Serialize(pagination));

                    Response.Headers.Append(
                        "Content-Disposition",
                        "inline; filename=\"MemberIdCardReport.pdf\"");

                    return new FileContentResult(pdfBytes, "application/pdf");



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
