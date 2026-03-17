using jsreport.AspNetCore;
using jsreport.Types;
using JsSampleReport.Inteface;
using Microsoft.AspNetCore.Hosting;
using System.Runtime.Intrinsics.X86;

namespace JsSampleReport.Services.ReportService
{
    /// <summary>
    /// Simplified jsreport service for generating reports
    /// </summary>
    public class JsReportService : IJsReportService
    {
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly ILogger<JsReportService> _logger;
        private readonly IJsReportMVCService _jsReportMVCService;

        public JsReportService(
            IWebHostEnvironment webHostEnvironment,
            ILogger<JsReportService> logger,
            IJsReportMVCService jsReportMVCService)
        {
            _webHostEnvironment = webHostEnvironment;
            _logger = logger;
            _jsReportMVCService = jsReportMVCService;
        }

        /// <summary>
        /// Generate report from HTML template
        /// </summary>
        //public byte[] GenerateReport(string reportPath, object data, string format)
        //{
        //    try
        //    {
        //        _logger.LogInformation($"Generating report: Path={reportPath}, Format={format}");

        //        var templateFilePath = Path.Combine(_webHostEnvironment.ContentRootPath, reportPath);

        //        if (!File.Exists(templateFilePath))
        //        {
        //            throw new FileNotFoundException($"Report template not found: {templateFilePath}");
        //        }

        //        // Read the HTML template
        //        var templateContent = File.ReadAllText(templateFilePath);

        //        // Define custom Handlebars helpers
        //        var helpers = @"
        //            function inc(value) {
        //                return parseInt(value) + 1;
        //            }

        //            function year(date) {
        //                if (!date) return '';
        //                var d = new Date(date);
        //                return d.getFullYear();
        //            }

        //            function formatDate(date, format) {
        //                if (!date) return '';
        //                var d = new Date(date);
        //                return d.toISOString().split('T')[0];
        //            }
        //        ";

        //        // Create jsreport render request
        //        var renderRequest = new RenderRequest()
        //        {
        //            Template = new Template()
        //            {
        //                Content = templateContent,
        //                Engine = Engine.Handlebars,
        //                Recipe = GetRecipe(format.ToUpper()),
        //                Helpers = helpers
        //            },
        //            Data = data  // Pass data directly
        //        };

        //        // Configure recipe-specific options
        //        ConfigureRecipeOptions(renderRequest, format.ToUpper());

        //        _logger.LogInformation("Rendering report with jsreport...");

        //        // Render the report using jsreport
        //        var result = _jsReportMVCService.RenderAsync(renderRequest).GetAwaiter().GetResult();

        //        // Convert stream to byte array
        //        using (var memoryStream = new MemoryStream())
        //        {
        //            result.Content.CopyTo(memoryStream);
        //            var reportBytes = memoryStream.ToArray();

        //            _logger.LogInformation($"Report generated successfully in {format} format. Size: {reportBytes.Length} bytes");

        //            return reportBytes;
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, $"Report generation error: {ex.Message}");
        //        throw new Exception($"Report rendering failed: {ex.Message}", ex);
        //    }
        //}

        public byte[] GenerateReport(string reportPath, object data, string format)
        {
            try
            {
                _logger.LogInformation($"Generating report: Path={reportPath}, Format={format}");

                var templateFilePath = Path.Combine(_webHostEnvironment.ContentRootPath, reportPath);

                if (!File.Exists(templateFilePath))
                    throw new FileNotFoundException($"Report template not found: {templateFilePath}");

                // ✅ Load the main template
                var templateContent = File.ReadAllText(templateFilePath);

                // ✅ Load CommonHeader partial from the same folder as the template
                var templateDir = Path.GetDirectoryName(templateFilePath)!;
                var commonHeaderPath = Path.Combine(templateDir, "CommonHeader.html");

                if (!File.Exists(commonHeaderPath))
                    throw new FileNotFoundException($"CommonHeader partial not found: {commonHeaderPath}");

                // ✅ Escape the partial content for safe JS string embedding
                var commonHeaderContent = File.ReadAllText(commonHeaderPath)
                    .Replace("\\", "\\\\")   // escape backslashes first
                    .Replace("`", "\\`");    // escape backticks (we'll use template literals)

                // ✅ Register partial + custom helpers
                var helpers = $@"
            // Register CommonHeader as a Handlebars partial
            Handlebars.registerPartial('CommonHeader', `{commonHeaderContent}`);

            function inc(value) {{
                return parseInt(value) + 1;
            }}

            function year(date) {{
                if (!date) return '';
                var d = new Date(date);
                return d.getFullYear();
            }}

            function formatDate(date, format) {{
                if (!date) return '';
                var d = new Date(date);
                return d.toISOString().split('T')[0];
            }}
        ";

                var renderRequest = new RenderRequest()
                {
                    Template = new Template()
                    {
                        Content = templateContent,
                        Engine = Engine.Handlebars,
                        Recipe = GetRecipe(format.ToUpper()),
                        Helpers = helpers
                    },
                    Data = data
                };

                ConfigureRecipeOptions(renderRequest, format.ToUpper());

                _logger.LogInformation("Rendering report with jsreport...");

                var result = _jsReportMVCService.RenderAsync(renderRequest).GetAwaiter().GetResult();

                using var memoryStream = new MemoryStream();
                result.Content.CopyTo(memoryStream);
                var reportBytes = memoryStream.ToArray();

                _logger.LogInformation($"Report generated: {format}, {reportBytes.Length} bytes");
                return reportBytes;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Report generation error: {ex.Message}");
                throw new Exception($"Report rendering failed: {ex.Message}", ex);
            }
        }



        private Recipe GetRecipe(string format)
        {
            return format switch
            {
                "PDF" or "VIEW" => Recipe.ChromePdf,
                "HTML" => Recipe.Html,
                "EXCEL" or "XLSX" => Recipe.HtmlToXlsx,
                "DOCX" or "WORD" => Recipe.Docx,
                "PNG" => Recipe.ChromeImage,
                _ => Recipe.ChromePdf
            };
        }

        /// <summary>
        /// Configure recipe-specific options
        /// </summary>
        private void ConfigureRecipeOptions(RenderRequest request, string format)
        {
            switch (format)
            {
                case "PDF":
                case "VIEW":
                    request.Template.Chrome = new Chrome
                    {
                        MarginTop = "1cm",
                        MarginBottom = "1cm",
                        MarginLeft = "1cm",
                        MarginRight = "1cm",
                        DisplayHeaderFooter = false,
                        PrintBackground = true,
                        Format = "A4",
                        Landscape = false
                    };
                    break;

                case "EXCEL":
                case "XLSX":
                    request.Template.HtmlToXlsx = new HtmlToXlsx
                    {
                        HtmlEngine = "chrome"
                    };
                    break;

                //case "PNG":
                //    request.Template.ChromeImage = new ChromeImage
                //    {
                //        Type = ChromeImageType.Png,
                //        Quality = 100,
                //        WaitForJS = false
                //    };
                //    request.Template.Chrome = new Chrome
                //    {
                //        PrintBackground = true
                //    };
                //    break;

                case "DOCX":
                case "WORD":
                    // Docx recipe doesn't require additional configuration
                    break;
            }
        }
    }
}



