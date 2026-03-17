using JsSampleProject.Dtos.MemberDtos;
using JsSampleProject.Interface;
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

        public MemberDetailController(
            IMemberDetail memberDetail,
            IJsReportService jsReportService,
            ILogger<MemberDetailController> logger)
        {
            _memberDetail = memberDetail;
            _jsReportService = jsReportService;
            _logger = logger;
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

                // Map member data to template-friendly format with null checks
                var mappedData = allMemberData.Select(m => new
                {
                    MemberId = m.MemberId ?? "",
                    FullName = m.Name ?? "",
                    Email = m.EmailId ?? "",
                    MobileNo = m.MobileNo ?? "",
                    Address = m.PermanentAddress ?? "",
                    Nationality = m.Nationality ?? "",
                    Occupation = m.Occupation ?? ""
                }).ToList();

                // Prepare complete data model - wrapped in a dictionary
                var reportData = new Dictionary<string, object>
                {
                    { "StudentDataSet", mappedData },
                    { "TotalRecords", mappedData.Count },
                    { "ReportTitle", "Member Detail Report" },
                    { "GeneratedBy", "System" },
                    { "GeneratedDate", DateTime.Now },
                    { "GeneratedDateString", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") }
                };

                var reportBytes = _jsReportService.GenerateReport(
                    reportPath: "Views/Report/MemberReport.html",
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