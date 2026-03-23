using JsSampleProject.Dtos.MemberDtos;
using JsSampleProject.Interface;
using JsSampleReport.Dtos.ReportDtos;
using JsSampleReport.Inteface;
using Microsoft.AspNetCore.Mvc;

namespace JsSampleReport.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MemberDetailController : ControllerBase
    {
        private readonly IMemberDetail _memberDetail;
        private readonly IJsReportService _jsReportService;
        private readonly ILogger<MemberDetailController> _logger;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public MemberDetailController(
            IMemberDetail memberDetail,
            IJsReportService jsReportService,
            ILogger<MemberDetailController> logger,
            IWebHostEnvironment webHostEnvironment)
        {
            _memberDetail = memberDetail;
            _jsReportService = jsReportService;
            _logger = logger;
            _webHostEnvironment = webHostEnvironment;
        }

        [HttpPost("generate-report")]
        public IActionResult GenerateReport(
            [FromBody] MemberDetailRequest request,
            [FromQuery] string format = "PDF")
        {
            try
            {
                if (request == null || !ModelState.IsValid)
                {
                    return BadRequest(new { success = false, message = "Invalid request" });
                }

                var upperFormat = format.ToUpper();

                // Fetch data
                var allMemberData = _memberDetail.GetMemberRegistrationDetail(request);

                if (allMemberData == null || !allMemberData.Any())
                {
                    return NotFound(new { success = false, message = "No data found for the specified criteria" });
                }

                var headerData = _memberDetail.GetCommonHeaders();
                // ✅ Convert CompanyLogo relative path → base64 data URL
                ConvertLogoToBase64(headerData);



                // Prepare complete data model - wrapped in a dictionary
                var reportData = new Dictionary<string, object>
                {
                    { "StudentDataSet", allMemberData },
                    //{ "TotalRecords", allMemberData.Count },
                    //{ "ReportTitle", "Member Detail Report" },
                    {"HeaderDataSet", headerData }
                                   };

                var reportBytes = _jsReportService.GenerateReport(
                    reportPath: "Views/Report/MemberReport.cshtml",
                    data: reportData,
                    format: upperFormat
                );

                // Handle VIEW mode - return as base64 PDF
                if (upperFormat == "VIEW")
                {
                    return Ok(new
                    {
                        success = true,
                        pdfData = Convert.ToBase64String(reportBytes),
                        reportName = "Member Detail Report"
                    });
                }

                // Download mode
                var (contentType, extension) = GetContentTypeAndExtension(upperFormat);
                return File(reportBytes, contentType, $"MemberDetailReport_{DateTime.Now:yyyyMMddHHmmss}.{extension}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating report");
                return StatusCode(500, new
                {
                    success = false,
                    message = "An error occurred while generating the report",
                    error = ex.Message
                });
            }
        }
        // ✅ Converts relative logo path to base64 data URL for each header
        private void ConvertLogoToBase64(IEnumerable<CommonHeader> headers)
        {
            if (headers == null) return;

            foreach (var header in headers)
            {
                if (string.IsNullOrWhiteSpace(header.CompanyLogo))
                    continue;

                try
                {
                    // ✅ Use WebRootPath if available, otherwise fallback to C:\inetpub\wwwroot
                    var webRoot = @"C:\inetpub\wwwroot";

                    var relativePath = header.CompanyLogo.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
                    var fullPath = Path.Combine(webRoot, relativePath);

                    _logger.LogInformation($"Looking for logo at: {fullPath}");

                    if (!System.IO.File.Exists(fullPath))
                    {
                        _logger.LogWarning($"Company logo not found at path: {fullPath}");
                        header.CompanyLogo = string.Empty;
                        continue;
                    }

                    var bytes = System.IO.File.ReadAllBytes(fullPath);
                    var base64 = Convert.ToBase64String(bytes);

                    var ext = Path.GetExtension(fullPath).TrimStart('.').ToLower();
                    var mimeType = ext switch
                    {
                        "png" => "image/png",
                        "jpg" or "jpeg" => "image/jpeg",
                        "gif" => "image/gif",
                        "bmp" => "image/bmp",
                        "webp" => "image/webp",
                        _ => "image/png"
                    };

                    header.CompanyLogo = $"data:{mimeType};base64,{base64}";
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Failed to convert logo to base64: {header.CompanyLogo}");
                    header.CompanyLogo = string.Empty;
                }
            }
        }
        private (string contentType, string extension) GetContentTypeAndExtension(string format)
        {
            return format switch
            {
                "VIEW" => ("application/pdf", "pdf"),
                "HTML" => ("text/html", "html"),
                "PDF" => ("application/pdf", "pdf"),
                "EXCEL" or "XLSX" => ("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "xlsx"),
                "WORD" or "DOCX" => ("application/vnd.openxmlformats-officedocument.wordprocessingml.document", "docx"),
                "PNG" => ("image/png", "png"),
                "CSV" => ("text/csv", "csv"),
                _ => ("application/pdf", "pdf")
            };
        }
    }
}




//using JsSampleProject.Dtos.MemberDtos;
//using JsSampleProject.Interface;
//using JsSampleReport.Inteface;
//using Microsoft.AspNetCore.Mvc;
//using System.Text.Json;

//namespace JsSampleReport.Controllers
//{
//    [ApiController]
//    [Route("api/[controller]")]
//    public class MemberDetailController : ControllerBase
//    {
//        private readonly IMemberDetail _memberDetail;
//        private readonly IJsReportService _jsReportService;
//        private readonly ILogger<MemberDetailController> _logger;

//        public MemberDetailController(
//            IMemberDetail memberDetail,
//            IJsReportService jsReportService,
//            ILogger<MemberDetailController> logger)
//        {
//            _memberDetail = memberDetail;
//            _jsReportService = jsReportService;
//            _logger = logger;
//        }

//        [HttpPost("generate-report")]
//        public IActionResult GenerateReport(
//            [FromBody] MemberDetailRequest request,
//            [FromQuery] string format = "PDF")
//        {
//            try
//            {
//                _logger.LogInformation("=== REPORT GENERATION START ===");
//                _logger.LogInformation($"Request format: {format}");

//                if (request == null || !ModelState.IsValid)
//                {
//                    _logger.LogWarning("Invalid request received");
//                    return BadRequest(new { success = false, message = "Invalid request" });
//                }

//                var upperFormat = format.ToUpper();

//                // Fetch data
//                var allMemberData = _memberDetail.GetMemberRegistrationDetail(request);

//                _logger.LogInformation($"Data retrieved: {allMemberData?.Count() ?? 0} records");

//                if (allMemberData == null || !allMemberData.Any())
//                {
//                    _logger.LogWarning("No data found for criteria");
//                    return NotFound(new { success = false, message = "No data found for the specified criteria" });
//                }

//                var headerData = _memberDetail.GetCommonHeaders();
//                _logger.LogInformation($"Header data retrieved: {headerData != null}");

//                // Map member data to template-friendly format with null checks
//                var mappedData = allMemberData.Select(m => new
//                {
//                    MemberId = m.MemberId ?? "",
//                    FullName = m.Name ?? "",
//                    Email = m.EmailId ?? "",
//                    MobileNo = m.MobileNo ?? "",
//                    Address = m.PermanentAddress ?? "",
//                    Nationality = m.Nationality ?? "",
//                    Occupation = m.Occupation ?? ""
//                }).ToList();

//                _logger.LogInformation($"Mapped {mappedData.Count} records");

//                // Log first record for debugging
//                if (mappedData.Any())
//                {
//                    _logger.LogInformation($"Sample record: {JsonSerializer.Serialize(mappedData.First())}");
//                }

//                // Prepare complete data model
//                var reportData = new Dictionary<string, object>
//                {
//                    // Main dataset - THIS IS THE KEY PART!
//                    { "StudentDataSet", mappedData },
//                    { "TotalRecords", mappedData.Count },
                    
//                    // Report parameters
//                    { "ReportTitle", "Member Detail Report" },
//                    { "GeneratedBy", "System" },
                    
//                    // Header data
//                    { "HeaderReport", new { CommonHeader = headerData } },
                    
//                    // System data
//                    { "GeneratedDate", DateTime.Now },
//                    { "GeneratedDateString", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") }
//                };

//                _logger.LogInformation($"Report data prepared with keys: {string.Join(", ", reportData.Keys)}");
//                _logger.LogInformation($"StudentDataSet in dictionary: {reportData.ContainsKey("StudentDataSet")}");

//                var reportBytes = _jsReportService.GenerateReport(
//                    reportPath: "Views/Reports/MemberReport.html",
//                    data: reportData,
//                    format: upperFormat
//                );

//                _logger.LogInformation($"Report generated: {reportBytes.Length} bytes");

//                // Handle VIEW mode - return as base64 PDF
//                if (upperFormat == "VIEW")
//                {
//                    return Ok(new
//                    {
//                        success = true,
//                        pdfData = Convert.ToBase64String(reportBytes),
//                        reportName = "Member Detail Report"
//                    });
//                }

//                // Download mode
//                var (contentType, extension) = GetContentTypeAndExtension(upperFormat);
//                var fileName = $"MemberDetailReport_{DateTime.Now:yyyyMMddHHmmss}.{extension}";

//                _logger.LogInformation($"Returning file: {fileName}");

//                return File(reportBytes, contentType, fileName);
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, "Error generating report: {Message}", ex.Message);
//                return StatusCode(500, new
//                {
//                    success = false,
//                    message = "An error occurred while generating the report",
//                    error = ex.Message,
//                    stackTrace = ex.StackTrace
//                });
//            }
//        }

//        private (string contentType, string extension) GetContentTypeAndExtension(string format)
//        {
//            return format switch
//            {
//                "VIEW" => ("application/pdf", "pdf"),
//                "HTML" => ("text/html", "html"),
//                "PDF" => ("application/pdf", "pdf"),
//                "EXCEL" or "XLSX" => ("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "xlsx"),
//                "WORD" or "DOCX" => ("application/vnd.openxmlformats-officedocument.wordprocessingml.document", "docx"),
//                "PNG" => ("image/png", "png"),
//                "CSV" => ("text/csv", "csv"),
//                _ => ("application/pdf", "pdf")
//            };
//        }
//    }
//}