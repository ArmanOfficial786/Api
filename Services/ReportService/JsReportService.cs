//using jsreport.AspNetCore;
//using jsreport.Types;
//using JsSampleReport.Inteface.ReportInterface;
//using Microsoft.AspNetCore.Mvc;
//using Microsoft.AspNetCore.Mvc.Abstractions;
//using Microsoft.AspNetCore.Mvc.ModelBinding;
//using Microsoft.AspNetCore.Mvc.Razor;
//using Microsoft.AspNetCore.Mvc.Rendering;
//using Microsoft.AspNetCore.Mvc.ViewFeatures;
//using Microsoft.AspNetCore.Routing;

//namespace JsSampleReport.Services.ReportService
//{
//    public class JsReportService : IJsReportService
//    {
//        private readonly ILogger<JsReportService> _logger;
//        private readonly IJsReportMVCService _jsReportMVCService;
//        private readonly IServiceProvider _serviceProvider;  // ✅ replaces IWebHostEnvironment

//        public JsReportService(
//            ILogger<JsReportService> logger,
//            IJsReportMVCService jsReportMVCService,
//            IServiceProvider serviceProvider)
//        {
//            _logger = logger;
//            _jsReportMVCService = jsReportMVCService;
//            _serviceProvider = serviceProvider;
//        }

//        public byte[] GenerateReport(string reportPath, object data, string format)
//        {
//            try
//            {
//                _logger.LogInformation($"Generating report: Path={reportPath}, Format={format}");

//                // ✅ Render .cshtml to HTML string instead of reading .html file
//                var htmlContent = RenderViewAsync(reportPath, data).GetAwaiter().GetResult();

//                var renderRequest = new RenderRequest()
//                {
//                    Template = new Template()
//                    {
//                        Content = htmlContent,
//                        Engine = Engine.None,      // ✅ already rendered, no engine needed
//                        Recipe = GetRecipe(format.ToUpper())
//                    },
//                };

//                ConfigureRecipeOptions(renderRequest, format.ToUpper());

//                _logger.LogInformation("Rendering report with jsreport...");

//                var result = _jsReportMVCService.RenderAsync(renderRequest).GetAwaiter().GetResult();

//                using var memoryStream = new MemoryStream();
//                result.Content.CopyTo(memoryStream);
//                var reportBytes = memoryStream.ToArray();

//                _logger.LogInformation($"Report generated: {format}, {reportBytes.Length} bytes");
//                return reportBytes;
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, $"Report generation error: {ex.Message}");
//                throw new Exception($"Report rendering failed: {ex.Message}", ex);
//            }
//        }

//        // ✅ Only new addition — renders .cshtml to raw HTML string
//        private async Task<string> RenderViewAsync(string viewName, object model)
//        {
//            var httpContext = new DefaultHttpContext { RequestServices = _serviceProvider };
//            var actionContext = new ActionContext(
//                httpContext,
//                new RouteData(),
//                new ActionDescriptor()
//            );

//            var viewEngine = _serviceProvider.GetRequiredService<IRazorViewEngine>();
//            var tempDataProvider = _serviceProvider.GetRequiredService<ITempDataProvider>();

//            // Try absolute path first (e.g. "~/Views/Report/MemberReport.cshtml")
//            var viewResult = viewEngine.GetView(null, viewName, false);
//            if (!viewResult.Success)
//                viewResult = viewEngine.FindView(actionContext, viewName, false);

//            if (!viewResult.Success)
//                throw new InvalidOperationException($"View '{viewName}' not found.");

//            await using var sw = new StringWriter();

//            var viewData = new ViewDataDictionary(
//                new EmptyModelMetadataProvider(),
//                new ModelStateDictionary())
//            { Model = model };

//            var viewContext = new ViewContext(
//                actionContext,
//                viewResult.View,
//                viewData,
//                new TempDataDictionary(httpContext, tempDataProvider),
//                sw,
//                new HtmlHelperOptions()
//            );

//            await viewResult.View.RenderAsync(viewContext);
//            return sw.ToString();
//        }

//        // ✅ Unchanged — exactly same as before
//        private Recipe GetRecipe(string format)
//        {
//            return format switch
//            {
//                "PDF" or "VIEW" => Recipe.ChromePdf,
//                "HTML" => Recipe.Html,
//                "EXCEL" or "XLSX" => Recipe.HtmlToXlsx,
//                "DOCX" or "WORD" => Recipe.Docx,
//                "PNG" => Recipe.ChromeImage,
//                _ => Recipe.ChromePdf
//            };
//        }

//        // ✅ Unchanged — exactly same as before
//        private void ConfigureRecipeOptions(RenderRequest request, string format)
//        {
//            switch (format)
//            {
//                case "PDF":
//                case "VIEW":
//                    request.Template.Chrome = new Chrome
//                    {
//                        MarginTop = "1mm",
//                        MarginBottom = "1mm",
//                        MarginLeft = "5mm",
//                        MarginRight = "5mm",
//                        DisplayHeaderFooter = false,
//                        PrintBackground = true,
//                        Format = "A4",
//                        Landscape = false
//                    };
//                    break;
//                case "EXCEL":
//                case "XLSX":
//                    request.Template.HtmlToXlsx = new HtmlToXlsx { HtmlEngine = "chrome" };
//                    break;
//            }
//        }
//    }
//}



//============This is exactly how SSRS, Crystal Reports, FastReport work internally — render once, export many times from server-side cache.=========================

using jsreport.AspNetCore;
using jsreport.Types;
using JsSampleReport.Inteface.ReportInterface;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Caching.Memory;

namespace JsSampleReport.Services.ReportService
{
    public class JsReportService : IJsReportService
    {
        private readonly ILogger<JsReportService> _logger;
        private readonly IJsReportMVCService _jsReportMVCService;
        private readonly IServiceProvider _serviceProvider;
        private readonly IMemoryCache _cache;

        public JsReportService(
            ILogger<JsReportService> logger,
            IJsReportMVCService jsReportMVCService,
            IServiceProvider serviceProvider,
            IMemoryCache cache)
        {
            _logger = logger;
            _jsReportMVCService = jsReportMVCService;
            _serviceProvider = serviceProvider;
            _cache = cache;
        }

        // ── Check if key exists in cache ──────────────────────────────────────
        public bool IsCached(string reportKey)
            => _cache.TryGetValue(reportKey, out _);

        // ── Pull HTML string from cache ───────────────────────────────────────
        public string? GetFromCache(string reportKey)
            => _cache.TryGetValue(reportKey, out string? html) ? html : null;

        // ── Original — direct generate (used if needed) ───────────────────────
        public byte[] GenerateReport(string reportPath, object data, string format)
        {
            try
            {
                _logger.LogInformation($"GenerateReport: {reportPath}, Format={format}");
                var html = RenderViewAsync(reportPath, data).GetAwaiter().GetResult();
                return GenerateReportFromHtml(html, format);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"GenerateReport error: {ex.Message}");
                throw new Exception($"Report rendering failed: {ex.Message}", ex);
            }
        }

        // ── Render .cshtml → HTML string, cache it, return HTML ───────────────
        public string RenderAndCacheReport(string reportKey,
                                           string reportPath,
                                           object data)
        {
            try
            {
                // Return from cache if already rendered
                if (_cache.TryGetValue(reportKey, out string? cachedHtml))
                {
                    _logger.LogInformation($"✅ Cache HIT : {reportKey}");
                    return cachedHtml!;
                }

                _logger.LogInformation($"🔄 Cache MISS — Rendering: {reportKey}");

                var html = RenderViewAsync(reportPath, data).GetAwaiter().GetResult();

                // Cache for 30 min sliding, max 2 hours absolute
                _cache.Set(reportKey, html, new MemoryCacheEntryOptions()
                    .SetSlidingExpiration(TimeSpan.FromMinutes(30))
                    .SetAbsoluteExpiration(TimeSpan.FromHours(2)));

                _logger.LogInformation($"✅ Cached: {reportKey}");
                return html;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"RenderAndCacheReport error: {ex.Message}");
                throw new Exception($"Render and cache failed: {ex.Message}", ex);
            }
        }

        // ── Convert HTML → any format via jsreport ────────────────────────────
        public byte[] GenerateReportFromHtml(string htmlContent, string format)
        {
            try
            {
                _logger.LogInformation($"GenerateReportFromHtml: Format={format}");

                var renderRequest = new RenderRequest()
                {
                    Template = new Template()
                    {
                        Content = htmlContent,
                        Engine = Engine.None,
                        Recipe = GetRecipe(format.ToUpper())
                    }
                };

                ConfigureRecipeOptions(renderRequest, format.ToUpper());

                var result = _jsReportMVCService
                                 .RenderAsync(renderRequest)
                                 .GetAwaiter().GetResult();

                using var ms = new MemoryStream();
                result.Content.CopyTo(ms);
                var bytes = ms.ToArray();

                _logger.LogInformation($"✅ Generated {format}: {bytes.Length} bytes");
                return bytes;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"GenerateReportFromHtml error: {ex.Message}");
                throw new Exception($"Generation from HTML failed: {ex.Message}", ex);
            }
        }

        // ── Render Razor .cshtml → raw HTML string ────────────────────────────
        // ✅ Uses CreateScope() — safe for Singleton registration
        private async Task<string> RenderViewAsync(string viewName, object model)
        {
            // ✅ Create a new scope per render call
            // This prevents captive dependency issues when JsReportService
            // is registered as Singleton but Razor services are Scoped
            using var scope = _serviceProvider.CreateScope();
            var scopedProvider = scope.ServiceProvider;

            var httpContext = new DefaultHttpContext
            { RequestServices = scopedProvider };

            var actionContext = new ActionContext(
                httpContext,
                new RouteData(),
                new ActionDescriptor());

            var viewEngine = scopedProvider.GetRequiredService<IRazorViewEngine>();
            var tempDataProvider = scopedProvider.GetRequiredService<ITempDataProvider>();

            // Try GetView first (absolute path), then FindView (by name)
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
                new HtmlHelperOptions());

            await viewResult.View.RenderAsync(viewContext);
            return sw.ToString();
        }

        private Recipe GetRecipe(string format) => format switch
        {
            "PDF" or "VIEW" => Recipe.ChromePdf,
            "HTML" => Recipe.Html,
            "EXCEL" or "XLSX" => Recipe.HtmlToXlsx,
            "DOCX" or "WORD" => Recipe.Docx,
            "PNG" => Recipe.ChromeImage,
            _ => Recipe.ChromePdf
        };

        private void ConfigureRecipeOptions(RenderRequest request, string format)
        {
            switch (format)
            {
                case "PDF":
                case "VIEW":
                    request.Template.Chrome = new Chrome
                    {
                        MarginTop = "1mm",
                        MarginBottom = "1mm",
                        MarginLeft = "5mm",
                        MarginRight = "5mm",
                        DisplayHeaderFooter = false,
                        PrintBackground = true,
                        Format = "A4",
                        Landscape = false
                    };
                    break;

                case "EXCEL":
                case "XLSX":
                    request.Template.HtmlToXlsx =
                        new HtmlToXlsx { HtmlEngine = "chrome" };
                    break;
            }
        }
    }
}