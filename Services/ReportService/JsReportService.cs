using jsreport.AspNetCore;
using jsreport.Types;
using JsSampleReport.Inteface;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Routing;

namespace JsSampleReport.Services.ReportService
{
    public class JsReportService : IJsReportService
    {
        private readonly ILogger<JsReportService> _logger;
        private readonly IJsReportMVCService _jsReportMVCService;
        private readonly IServiceProvider _serviceProvider;  // ✅ replaces IWebHostEnvironment

        public JsReportService(
            ILogger<JsReportService> logger,
            IJsReportMVCService jsReportMVCService,
            IServiceProvider serviceProvider)
        {
            _logger = logger;
            _jsReportMVCService = jsReportMVCService;
            _serviceProvider = serviceProvider;
        }

        public byte[] GenerateReport(string reportPath, object data, string format)
        {
            try
            {
                _logger.LogInformation($"Generating report: Path={reportPath}, Format={format}");

                // ✅ Render .cshtml to HTML string instead of reading .html file
                var htmlContent = RenderViewAsync(reportPath, data).GetAwaiter().GetResult();

                var renderRequest = new RenderRequest()
                {
                    Template = new Template()
                    {
                        Content = htmlContent,
                        Engine = Engine.None,      // ✅ already rendered, no engine needed
                        Recipe = GetRecipe(format.ToUpper())
                    },
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

        // ✅ Only new addition — renders .cshtml to raw HTML string
        private async Task<string> RenderViewAsync(string viewName, object model)
        {
            var httpContext = new DefaultHttpContext { RequestServices = _serviceProvider };
            var actionContext = new ActionContext(
                httpContext,
                new RouteData(),
                new ActionDescriptor()
            );

            var viewEngine = _serviceProvider.GetRequiredService<IRazorViewEngine>();
            var tempDataProvider = _serviceProvider.GetRequiredService<ITempDataProvider>();

            // Try absolute path first (e.g. "~/Views/Report/MemberReport.cshtml")
            var viewResult = viewEngine.GetView(null, viewName, false);
            if (!viewResult.Success)
                viewResult = viewEngine.FindView(actionContext, viewName, false);

            if (!viewResult.Success)
                throw new InvalidOperationException($"View '{viewName}' not found.");

            await using var sw = new StringWriter();

            var viewData = new ViewDataDictionary(
                new EmptyModelMetadataProvider(),
                new ModelStateDictionary())
            { Model = model };

            var viewContext = new ViewContext(
                actionContext,
                viewResult.View,
                viewData,
                new TempDataDictionary(httpContext, tempDataProvider),
                sw,
                new HtmlHelperOptions()
            );

            await viewResult.View.RenderAsync(viewContext);
            return sw.ToString();
        }

        // ✅ Unchanged — exactly same as before
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

        // ✅ Unchanged — exactly same as before
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
                    request.Template.HtmlToXlsx = new HtmlToXlsx { HtmlEngine = "chrome" };
                    break;
            }
        }
    }
}

