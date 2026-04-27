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

//using DocumentFormat.OpenXml;
//using DocumentFormat.OpenXml.Packaging;
//using DocumentFormat.OpenXml;
//using DocumentFormat.OpenXml.Packaging;
//using DocumentFormat.OpenXml.Wordprocessing;
//using HtmlToOpenXml;
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
//using Microsoft.Extensions.Caching.Memory;

//namespace JsSampleReport.Services.ReportService
//{
//    public class JsReportService : IJsReportService
//    {
//        private readonly ILogger<JsReportService> _logger;
//        private readonly IJsReportMVCService _jsReportMVCService;
//        private readonly IServiceProvider _serviceProvider;
//        private readonly IMemoryCache _cache;

//        public JsReportService(
//            ILogger<JsReportService> logger,
//            IJsReportMVCService jsReportMVCService,
//            IServiceProvider serviceProvider,
//            IMemoryCache cache)
//        {
//            _logger = logger;
//            _jsReportMVCService = jsReportMVCService;
//            _serviceProvider = serviceProvider;
//            _cache = cache;
//        }

//        // ── Check if key exists in cache ──────────────────────────────────────
//        public bool IsCached(string reportKey)
//            => _cache.TryGetValue(reportKey, out _);

//        // ── Pull HTML string from cache ───────────────────────────────────────
//        public string? GetFromCache(string reportKey)
//            => _cache.TryGetValue(reportKey, out string? html) ? html : null;

//        // ── Original — direct generate (used if needed) ───────────────────────
//        public byte[] GenerateReport(string reportPath, object data, string format)
//        {
//            try
//            {
//                _logger.LogInformation($"GenerateReport: {reportPath}, Format={format}");
//                var html = RenderViewAsync(reportPath, data).GetAwaiter().GetResult();
//                return GenerateReportFromHtml(html, format);
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, $"GenerateReport error: {ex.Message}");
//                throw new Exception($"Report rendering failed: {ex.Message}", ex);
//            }
//        }

//        // ── Render .cshtml → HTML string, cache it, return HTML ───────────────
//        public string RenderAndCacheReport(string reportKey,
//                                           string reportPath,
//                                           object data)
//        {
//            try
//            {
//                // Return from cache if already rendered
//                if (_cache.TryGetValue(reportKey, out string? cachedHtml))
//                {
//                    _logger.LogInformation($"✅ Cache HIT : {reportKey}");
//                    return cachedHtml!;
//                }

//                _logger.LogInformation($"🔄 Cache MISS — Rendering: {reportKey}");

//                var html = RenderViewAsync(reportPath, data).GetAwaiter().GetResult();

//                // Cache for 30 min sliding, max 2 hours absolute
//                _cache.Set(reportKey, html, new MemoryCacheEntryOptions()
//                    .SetSlidingExpiration(TimeSpan.FromMinutes(30))
//                    .SetAbsoluteExpiration(TimeSpan.FromHours(2)));

//                _logger.LogInformation($"✅ Cached: {reportKey}");
//                return html;
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, $"RenderAndCacheReport error: {ex.Message}");
//                throw new Exception($"Render and cache failed: {ex.Message}", ex);
//            }
//        }

//        // ── Convert HTML → any format via jsreport ────────────────────────────
//        public byte[] GenerateReportFromHtml(string htmlContent, string format)
//        {
//            try
//            {
//                _logger.LogInformation($"GenerateReportFromHtml: Format={format}");
//                // ✅ Bypass jsreport for DOCX — Word can open HTML natively
//                // jsreport has no HTML→DOCX recipe without a .docx template file
//                if (format.ToUpper() is "DOCX" or "WORD")
//                {
//                    _logger.LogInformation("✅ DOCX: Returning HTML bytes with Word MIME");
//                    //return System.Text.Encoding.UTF8.GetBytes(htmlContent);
//                    return ConvertHtmlToDocx(htmlContent);
//                }

//                var renderRequest = new RenderRequest()
//                {
//                    Template = new Template()
//                    {
//                        Content = htmlContent,
//                        Engine = Engine.None,
//                        Recipe = GetRecipe(format.ToUpper())
//                    }
//                };

//                ConfigureRecipeOptions(renderRequest, format.ToUpper());

//                var result = _jsReportMVCService
//                                 .RenderAsync(renderRequest)
//                                 .GetAwaiter().GetResult();

//                using var ms = new MemoryStream();
//                result.Content.CopyTo(ms);
//                var bytes = ms.ToArray();

//                _logger.LogInformation($"✅ Generated {format}: {bytes.Length} bytes");
//                return bytes;
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, $"GenerateReportFromHtml error: {ex.Message}");
//                throw new Exception($"Generation from HTML failed: {ex.Message}", ex);
//            }
//        }

//        // ✅ Fixed ConvertHtmlToDocx — proper async handling
//        private byte[] ConvertHtmlToDocx(string htmlContent)
//        {
//            try
//            {
//                using var ms = new MemoryStream();

//                using (var wordDoc = WordprocessingDocument.Create(
//                           ms, WordprocessingDocumentType.Document, true))
//                {
//                    var mainPart = wordDoc.AddMainDocumentPart();

//                    // ✅ Must initialize Document with Body first
//                    mainPart.Document = new Document();
//                    var body = mainPart.Document.AppendChild(new Body());

//                    // ✅ HtmlConverter — ParseHtml is async in newer versions
//                    var converter = new HtmlConverter(mainPart);
//                    var task = converter.ParseHtml(htmlContent);
//                    task.GetAwaiter().GetResult(); // ✅ Wait for async ParseHtml

//                    mainPart.Document.Save();
//                }

//                var bytes = ms.ToArray();
//                _logger.LogInformation($"✅ DOCX generated: {bytes.Length} bytes");
//                return bytes;
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, "❌ ConvertHtmlToDocx failed");
//                throw new Exception($"DOCX conversion failed: {ex.Message}", ex);
//            }
//        }

//        // ── Render Razor .cshtml → raw HTML string ────────────────────────────
//        // ✅ Uses CreateScope() — safe for Singleton registration
//        private async Task<string> RenderViewAsync(string viewName, object model)
//        {
//            // ✅ Create a new scope per render call
//            // This prevents captive dependency issues when JsReportService
//            // is registered as Singleton but Razor services are Scoped
//            using var scope = _serviceProvider.CreateScope();
//            var scopedProvider = scope.ServiceProvider;

//            var httpContext = new DefaultHttpContext
//            { RequestServices = scopedProvider };

//            var actionContext = new ActionContext(
//                httpContext,
//                new RouteData(),
//                new ActionDescriptor());

//            var viewEngine = scopedProvider.GetRequiredService<IRazorViewEngine>();
//            var tempDataProvider = scopedProvider.GetRequiredService<ITempDataProvider>();

//            // Try GetView first (absolute path), then FindView (by name)
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
//                new HtmlHelperOptions());

//            await viewResult.View.RenderAsync(viewContext);
//            return sw.ToString();
//        }

//        private Recipe GetRecipe(string format) => format switch
//        {
//            "PDF" or "VIEW" => Recipe.ChromePdf,
//            "HTML" => Recipe.Html,
//            "EXCEL" or "XLSX" => Recipe.HtmlToXlsx,
//            //"DOCX" or "WORD" => Recipe.Html,
//            "PNG" => Recipe.ChromeImage,
//            _ => Recipe.ChromePdf
//        };

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
//                    request.Template.HtmlToXlsx =
//                        new HtmlToXlsx { HtmlEngine = "chrome" };
//                    break;
//            }
//        }
//    }
//}







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

        // ══════════════════════════════════════════════════════════════════
        // Cache helpers
        // ══════════════════════════════════════════════════════════════════
        public bool IsCached(string reportKey)
            => _cache.TryGetValue(reportKey, out _);

        public string? GetFromCache(string reportKey)
            => _cache.TryGetValue(reportKey, out string? html) ? html : null;

        // ══════════════════════════════════════════════════════════════════
        // Render .cshtml → HTML → cache — ASYNC
        // ══════════════════════════════════════════════════════════════════
        public async Task<string> RenderAndCacheReportAsync(
            string reportKey,
            string reportPath,
            object data)
        {
            try
            {
                if (_cache.TryGetValue(reportKey, out string? cachedHtml))
                {
                    _logger.LogInformation("✅ Cache HIT: {Key}", reportKey);
                    return cachedHtml!;
                }

                _logger.LogInformation("🔄 Cache MISS — Rendering: {Key}", reportKey);

                var html = await RenderViewAsync(reportPath, data);

                _cache.Set(reportKey, html, new MemoryCacheEntryOptions()
                    .SetSlidingExpiration(TimeSpan.FromMinutes(30))
                    .SetAbsoluteExpiration(TimeSpan.FromHours(2)));

                _logger.LogInformation("✅ Cached: {Key}", reportKey);
                return html;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ RenderAndCacheReportAsync error: {Msg}", ex.Message);
                throw new Exception($"Render and cache failed: {ex.Message}", ex);
            }
        }

        // ══════════════════════════════════════════════════════════════════
        // HTML → any format — ASYNC
        // ══════════════════════════════════════════════════════════════════
        public async Task<byte[]> GenerateReportFromHtmlAsync(
            string htmlContent,
            string format)
        {
            try
            {
                _logger.LogInformation(
                    "GenerateReportFromHtmlAsync: Format={Format}", format);

                var upperFormat = format.ToUpper();

                // ✅ DOCX — PDF → LibreOffice → DOCX
                if (upperFormat is "DOCX" or "WORD")
                {
                    _logger.LogInformation("✅ DOCX: PDF → LibreOffice → DOCX");
                    var pdfBytes = await GeneratePdfBytesAsync(htmlContent);
                    return await ConvertPdfToDocxAsync(pdfBytes);
                }

                // ✅ All other formats — jsreport
                var renderRequest = new RenderRequest
                {
                    Template = new Template
                    {
                        Content = htmlContent,
                        Engine = Engine.None,
                        Recipe = GetRecipe(upperFormat)
                    }
                };

                ConfigureRecipeOptions(renderRequest, upperFormat);

                var result = await _jsReportMVCService.RenderAsync(renderRequest);

                using var ms = new MemoryStream();
                await result.Content.CopyToAsync(ms);
                var bytes = ms.ToArray();

                _logger.LogInformation(
                    "✅ Generated {Format}: {Bytes} bytes", format, bytes.Length);
                return bytes;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "❌ GenerateReportFromHtmlAsync error: {Msg}", ex.Message);
                throw new Exception($"Generation from HTML failed: {ex.Message}", ex);
            }
        }

        // ══════════════════════════════════════════════════════════════════
        // PDF bytes — ASYNC (internal — used for DOCX conversion)
        // ══════════════════════════════════════════════════════════════════
        private async Task<byte[]> GeneratePdfBytesAsync(string htmlContent)
        {
            var renderRequest = new RenderRequest
            {
                Template = new Template
                {
                    Content = htmlContent,
                    Engine = Engine.None,
                    Recipe = Recipe.ChromePdf,
                    Chrome = new Chrome
                    {
                        MarginTop = "1mm",
                        MarginBottom = "1mm",
                        MarginLeft = "5mm",
                        MarginRight = "5mm",
                        DisplayHeaderFooter = false,
                        PrintBackground = true,
                        Format = "A4",
                        Landscape = false
                    }
                }
            };

            var result = await _jsReportMVCService.RenderAsync(renderRequest);
            using var ms = new MemoryStream();
            await result.Content.CopyToAsync(ms);
            return ms.ToArray();
        }

        // ══════════════════════════════════════════════════════════════════
        // PDF → DOCX via LibreOffice — ASYNC
        // ══════════════════════════════════════════════════════════════════
        private async Task<byte[]> ConvertPdfToDocxAsync(byte[] pdfBytes)
        {
            var tempDir = Path.Combine(Path.GetTempPath(), $"report_{Guid.NewGuid()}");
            var pdfPath = Path.Combine(tempDir, "report.pdf");
            var docxPath = Path.Combine(tempDir, "report.docx");

            Directory.CreateDirectory(tempDir);

            try
            {
                await File.WriteAllBytesAsync(pdfPath, pdfBytes);

                var libreOfficePath = GetLibreOfficePath();

                using var process = new System.Diagnostics.Process
                {
                    StartInfo = new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = libreOfficePath,
                        Arguments = $"--headless --convert-to docx \"{pdfPath}\" --outdir \"{tempDir}\"",
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };

                process.Start();

                // ✅ Async read — prevents deadlock
                var outputTask = process.StandardOutput.ReadToEndAsync();
                var errorTask = process.StandardError.ReadToEndAsync();

                await Task.WhenAll(outputTask, errorTask);
                await process.WaitForExitAsync();

                _logger.LogInformation("LibreOffice output: {Out}", outputTask.Result);

                if (!File.Exists(docxPath))
                {
                    _logger.LogError("❌ LibreOffice failed: {Err}", errorTask.Result);
                    throw new Exception($"LibreOffice conversion failed: {errorTask.Result}");
                }

                var docxBytes = await File.ReadAllBytesAsync(docxPath);
                _logger.LogInformation("✅ PDF→DOCX: {Bytes} bytes", docxBytes.Length);
                return docxBytes;
            }
            finally
            {
                if (Directory.Exists(tempDir))
                    Directory.Delete(tempDir, recursive: true);
            }
        }

        // ══════════════════════════════════════════════════════════════════
        // Resolve LibreOffice path
        // ══════════════════════════════════════════════════════════════════
        private string GetLibreOfficePath()
        {
            var paths = new[]
            {
                @"C:\Program Files\LibreOffice\program\soffice.exe",
                @"C:\Program Files (x86)\LibreOffice\program\soffice.exe",
                "/usr/bin/libreoffice",
                "/usr/bin/soffice"
            };

            var found = paths.FirstOrDefault(File.Exists)
                ?? throw new FileNotFoundException(
                       "LibreOffice not found. Install from https://www.libreoffice.org");

            _logger.LogInformation("✅ LibreOffice found: {Path}", found);
            return found;
        }

        // ══════════════════════════════════════════════════════════════════
        // Razor .cshtml → HTML — TRUE ASYNC
        // ══════════════════════════════════════════════════════════════════
        private async Task<string> RenderViewAsync(string viewName, object model)
        {
            using var scope = _serviceProvider.CreateScope();
            var scopedProvider = scope.ServiceProvider;
            var httpContext = new DefaultHttpContext { RequestServices = scopedProvider };
            var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());
            var viewEngine = scopedProvider.GetRequiredService<IRazorViewEngine>();
            var tempDataProvider = scopedProvider.GetRequiredService<ITempDataProvider>();

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

        // ══════════════════════════════════════════════════════════════════
        // Helpers
        // ══════════════════════════════════════════════════════════════════
        private static Recipe GetRecipe(string format) => format switch
        {
            "PDF" or "VIEW" => Recipe.ChromePdf,
            "HTML" => Recipe.Html,
            "EXCEL" or "XLSX" => Recipe.HtmlToXlsx,
            "PNG" => Recipe.ChromeImage,
            _ => Recipe.ChromePdf
        };

        private  void ConfigureRecipeOptions(RenderRequest request, string format)
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

                    // ✅ Only use properties that exist in your jsreport version
                  

                    break;
            }
        }
    }
}



