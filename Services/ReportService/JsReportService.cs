
using jsreport.AspNetCore;
using jsreport.Types;
using JsSampleReport.Dtos.ReportDtos;
using JsSampleReport.Inteface.ReportInterface;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace JsSampleReport.Services.ReportService
{
    /// <summary>
    /// Workflow:
    /// 1. RenderAndCacheReportAsync()      - Render Razor .cshtml to HTML and cache it
    /// 2. GenerateReportFromHtmlAsync()    - Convert cached HTML to PDF, Excel, Word, etc.
    /// 
    /// This prevents DB calls during export — render once, export many times from cache.
    /// 
    /// Method Sequence:
    /// ├─ CACHE HELPERS (IsCached, GetFromCache)
    /// ├─ RENDER PATH (RenderAndCacheReportAsync → RenderViewAsync)
    /// ├─ EXPORT PATH (GenerateReportFromHtmlAsync)
    /// ├─ JSREPORT ENGINE (RenderHtmlAsPdfAsync, GetRecipe, ApplyFormatOptions)
    /// ├─ LIBREOFFICE (ConvertPdfToDocxAsync, FindLibreOfficePath)
    /// └─ INTERNAL HELPERS
    /// </summary>
    public class JsReportService : IJsReportService
    {
        private readonly ILogger<JsReportService> _logger;
        private readonly IJsReportMVCService _jsReportMVCService;
        private readonly IServiceProvider _serviceProvider;
        private readonly IMemoryCache _cache;
        private const int LibreOfficeTimeoutMs = 30000; // 30 seconds

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

        // ╔════════════════════════════════════════════════════════════════════╗
        // ║ CACHE HELPERS — Used everywhere (before DB calls for exports)      ║
        // ╚════════════════════════════════════════════════════════════════════╝

        /// <summary>
        /// Check if report HTML is cached (e.g., for export check before DB call)
        /// </summary>
        public bool IsCached(string reportKey)
            => _cache.TryGetValue(reportKey, out _);

        /// <summary>
        /// Retrieve cached HTML without DB call (for exports)
        /// </summary>
        public string? GetFromCache(string reportKey)
            => _cache.TryGetValue(reportKey, out string? html) ? html : null;

        // ╔════════════════════════════════════════════════════════════════════╗
        // ║ RENDER PATH — Initial .cshtml rendering to HTML → Cache            ║
        // ║ Called ONCE per report (with DB query)                             ║
        // ╚════════════════════════════════════════════════════════════════════╝

        /// <summary>
        /// Render Razor view to HTML and cache for later exports
        /// Call once per report — then export multiple times from cache
        /// </summary>
        public async Task<string> RenderAndCacheReportAsync(
            string reportKey,
            string reportPath,
            object data)
        {
            try
            {
                // ✅ Return cached if already rendered
                if (_cache.TryGetValue(reportKey, out string? cachedHtml))
                {
                    _logger.LogInformation("✅ Cache HIT: {Key}", reportKey);
                    return cachedHtml!;
                }

                _logger.LogInformation("🔄 Cache MISS — Rendering: {Key}", reportKey);

                // ✅ Render Razor .cshtml to HTML string
                var html = await RenderViewAsync(reportPath, data);

                // ✅ Cache with sliding + absolute expiration
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

        // ╔════════════════════════════════════════════════════════════════════╗
        // ║ EXPORT PATH — Convert cached HTML to PDF, Excel, Word, PNG        ║
        // ║ Called MANY times (NO DB calls — uses cache)                       ║
        // ╚════════════════════════════════════════════════════════════════════╝

        /// <summary>
        /// Convert HTML to any format (PDF, Excel, Word, PNG)
        /// Always called with cached HTML — NO DB CALLS
        /// </summary>
        public async Task<byte[]> GenerateReportFromHtmlAsync(
            string htmlContent,
            string format)
        {
            try
            {
                var upperFormat = format.ToUpper();
                _logger.LogInformation("📄 Exporting to {Format}...", upperFormat);

                // ✅ Word: HTML → PDF → DOCX (via LibreOffice)
                if (upperFormat is "DOCX" or "WORD")
                {
                    var pdfBytes = await RenderHtmlAsPdfAsync(htmlContent);
                    return await ConvertPdfToDocxAsync(pdfBytes);
                }

                // ✅ All others: HTML → [format] via jsreport
                var renderRequest = new RenderRequest
                {
                    Template = new Template
                    {
                        Content = htmlContent,
                        Engine = Engine.None,
                        Recipe = GetRecipe(upperFormat)
                    }
                };

                ApplyFormatOptions(renderRequest, upperFormat);
                var result = await _jsReportMVCService.RenderAsync(renderRequest);

                using var ms = new MemoryStream();
                await result.Content.CopyToAsync(ms);
                var bytes = ms.ToArray();

                _logger.LogInformation("✅ Exported {Format}: {Bytes} bytes", upperFormat, bytes.Length);
                return bytes;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Export error ({Format}): {Msg}", format.ToUpper(), ex.Message);
                throw new Exception($"Export to {format} failed: {ex.Message}", ex);
            }
        }

        // ╔════════════════════════════════════════════════════════════════════╗
        // ║ JSREPORT ENGINE — HTML → PDF/Excel/PNG conversions                ║
        // ║ Internal methods called by GenerateReportFromHtmlAsync              ║
        // ╚════════════════════════════════════════════════════════════════════╝

        /// <summary>
        /// Render HTML as PDF via jsreport (used internally for DOCX conversion)
        /// </summary>
        private async Task<byte[]> RenderHtmlAsPdfAsync(string htmlContent)
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

        /// <summary>
        /// Map export format string to jsreport recipe enum
        /// </summary>
        private static Recipe GetRecipe(string format) => format switch
        {
            "PDF" or "VIEW" => Recipe.ChromePdf,
            "HTML" => Recipe.Html,
            "EXCEL" or "XLSX" => Recipe.HtmlToXlsx,
            "PNG" => Recipe.ChromeImage,
            _ => Recipe.ChromePdf
        };

        /// <summary>
        /// Apply format-specific options (margins, page size, etc.) to jsreport request
        /// </summary>
        private static void ApplyFormatOptions(RenderRequest request, string format)
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
                    request.Template.HtmlToXlsx = new HtmlToXlsx { HtmlEngine = "chrome" };
                    break;
            }
        }

        // ╔════════════════════════════════════════════════════════════════════╗
        // ║ LIBREOFFICE — PDF → DOCX conversion via LibreOffice              ║
        // ║ Only used for Word/DOCX exports                                    ║
        // ╚════════════════════════════════════════════════════════════════════╝

        /// <summary>
        /// Convert PDF to DOCX using LibreOffice
        /// </summary>
        private async Task<byte[]> ConvertPdfToDocxAsync(byte[] pdfBytes)
        {

            var tempDir = Path.Combine(Path.GetTempPath(), $"report_{Guid.NewGuid()}");
            var pdfPath = Path.Combine(tempDir, "report.pdf");
            var docxPath = Path.Combine(tempDir, "report.docx");

            Directory.CreateDirectory(tempDir);
            _logger.LogInformation("📁 Created temp directory: {TempDir}", tempDir);

            try
            {
                // ✅ Write PDF file
                await File.WriteAllBytesAsync(pdfPath, pdfBytes);
                _logger.LogInformation("📄 PDF written: {Path} ({Size} bytes)", pdfPath, pdfBytes.Length);

                // ✅ Verify PDF exists
                if (!File.Exists(pdfPath))
                {
                    throw new FileNotFoundException($"PDF file was not created at: {pdfPath}");
                }
                _logger.LogInformation("✅ PDF file verified to exist");

                var libreOfficePath = FindLibreOfficePath();
                _logger.LogInformation("🔍 LibreOffice path: {Path}", libreOfficePath);

                // ✅ Try multiple command formats
                var commands = new[]
                {
                    // Format 1: Standard (most common)
                    new { args = $"--headless --convert-to docx \"{pdfPath}\" --outdir \"{tempDir}\"", name = "Standard headless" },

                    // Format 2: With batch mode
                    new { args = $"--headless --invisible --convert-to docx:MS Word 2007 XML \"{pdfPath}\" --outdir \"{tempDir}\"", name = "With filter" },

                    // Format 3: Simpler version
                    new { args = $"--convert-to docx \"{pdfPath}\" --outdir \"{tempDir}\"", name = "No headless" }
                };

                Exception? lastException = null;

                foreach (var cmd in commands)
                {
                    try
                    {
                        _logger.LogInformation("🔄 Attempt: {Name}", cmd.name);
                        _logger.LogInformation("   Command: {Exe} {Args}", libreOfficePath, cmd.args);

                        // Clear any leftover docx
                        if (File.Exists(docxPath))
                        {
                            File.Delete(docxPath);
                            _logger.LogInformation("🗑️  Deleted existing docx file");
                        }

                        using var process = new System.Diagnostics.Process
                        {
                            StartInfo = new System.Diagnostics.ProcessStartInfo
                            {
                                FileName = libreOfficePath,
                                Arguments = cmd.args,
                                RedirectStandardOutput = true,
                                RedirectStandardError = true,
                                UseShellExecute = false,
                                CreateNoWindow = true,
                                WorkingDirectory = tempDir  // ✅ Set working directory
                            }
                        };

                        process.Start();
                        _logger.LogInformation("▶️  Process started (PID: {Pid})", process.Id);

                        // ✅ Async read to prevent deadlock
                        var outputTask = process.StandardOutput.ReadToEndAsync();
                        var errorTask = process.StandardError.ReadToEndAsync();

                        // ✅ Wait with timeout
                        var delayTask = Task.Delay(LibreOfficeTimeoutMs);
                        var outputComplete = await Task.WhenAny(outputTask, delayTask);
                        var errorComplete = await Task.WhenAny(errorTask, delayTask);

                        if (outputComplete == delayTask || errorComplete == delayTask)
                        {
                            process.Kill();
                            _logger.LogError("⏱️  LibreOffice timed out after {Timeout}ms", LibreOfficeTimeoutMs);
                            throw new TimeoutException($"LibreOffice conversion timed out after {LibreOfficeTimeoutMs}ms");
                        }

                        await process.WaitForExitAsync();

                        var output = outputTask.IsCompletedSuccessfully ? outputTask.Result : "";
                        var error = errorTask.IsCompletedSuccessfully ? errorTask.Result : "";
                        var exitCode = process.ExitCode;

                        _logger.LogInformation("📤 Output: {Output}", string.IsNullOrEmpty(output) ? "(empty)" : output);
                        if (!string.IsNullOrEmpty(error))
                            _logger.LogInformation("⚠️  Stderr: {Error}", error);
                        _logger.LogInformation("🔍 Exit code: {ExitCode}", exitCode);

                        // ✅ List all files in temp directory
                        var files = Directory.GetFiles(tempDir);
                        _logger.LogInformation("📂 Files in temp directory ({Count}):", files.Length);
                        foreach (var file in files)
                        {
                            var info = new FileInfo(file);
                            _logger.LogInformation("   - {FileName} ({Size} bytes)", info.Name, info.Length);
                        }

                        // ✅ Check if conversion succeeded
                        if (File.Exists(docxPath))
                        {
                            var docxBytes = await File.ReadAllBytesAsync(docxPath);
                            _logger.LogInformation("✅ PDF→DOCX successful: {Bytes} bytes", docxBytes.Length);
                            return docxBytes;
                        }

                        _logger.LogWarning("⚠️  DOCX not created with {Name} attempt", cmd.name);
                        lastException = new FileNotFoundException($"DOCX file not created at: {docxPath}");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "⚠️  {Name} failed, trying next format", cmd.name);
                        lastException = ex;
                    }
                }

                // ✅ All formats failed
                var errorMessage = lastException?.Message ?? "Unknown error";
                _logger.LogError("❌ All LibreOffice conversion attempts failed: {Error}", errorMessage);
                throw new Exception($"LibreOffice conversion failed after {commands.Length} attempts: {errorMessage}", lastException);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ ConvertPdfToDocxAsync failed");
                throw;
            }
            finally
            {
                try
                {
                    if (Directory.Exists(tempDir))
                    {
                        Directory.Delete(tempDir, recursive: true);
                        _logger.LogInformation("🗑️  Temp directory cleaned up: {TempDir}", tempDir);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "⚠️  Failed to cleanup temp directory: {TempDir}", tempDir);
                }
            }
        }

        /// <summary>
        /// Find LibreOffice installation path (Windows or Linux)
        /// Auto-detects common installation locations
        /// </summary>
        private string FindLibreOfficePath()
        {
            // ✅ Auto-detect common installation paths
            var paths = new[]
            {
                // Windows installations
                @"C:\Program Files\LibreOffice\program\soffice.exe",
                @"C:\Program Files (x86)\LibreOffice\program\soffice.exe",
                @"C:\Program Files\LibreOffice\soffice.exe",
                @"C:\Program Files (x86)\LibreOffice\soffice.exe",

                // Linux installations
                "/usr/bin/soffice",
                "/usr/bin/libreoffice",
                "/snap/bin/libreoffice",
                "/opt/libreoffice/program/soffice"
            };

            _logger.LogInformation("🔍 Searching for LibreOffice in {Count} paths", paths.Length);

            foreach (var path in paths)
            {
                _logger.LogDebug("  Checking: {Path}", path);
                if (File.Exists(path))
                {
                    _logger.LogInformation("✅ LibreOffice found: {Path}", path);
                    return path;
                }
            }

            // ✅ Not found — provide helpful error
            var error = "LibreOffice not found in common installation paths.\n" +
                       "Checked paths:\n" +
                       string.Join("\n", paths.Select(p => $"  - {p}")) +
                       "\n\nSolution:\n" +
                       "1. Install LibreOffice from https://www.libreoffice.org\n" +
                       "2. Or set CustomPath in appsettings.json LibreOfficeSettings.CustomPath";

            _logger.LogError("❌ {Error}", error);
            throw new FileNotFoundException(error);
        }

        // ╔════════════════════════════════════════════════════════════════════╗
        // ║ INTERNAL HELPERS — Razor rendering (called by RenderAndCacheAsync) ║
        // ╚════════════════════════════════════════════════════════════════════╝

        /// <summary>
        /// Render Razor .cshtml file to HTML string (called by RenderAndCacheReportAsync)
        /// </summary>
        private async Task<string> RenderViewAsync(string viewName, object model)
        {
            // ✅ Create scope to avoid captive dependency issues
            using var scope = _serviceProvider.CreateScope();
            var scopedProvider = scope.ServiceProvider;

            var httpContext = new DefaultHttpContext { RequestServices = scopedProvider };
            var actionContext = new ActionContext(
                httpContext,
                new RouteData(),
                new ActionDescriptor());

            var viewEngine = scopedProvider.GetRequiredService<IRazorViewEngine>();
            var tempDataProvider = scopedProvider.GetRequiredService<ITempDataProvider>();

            // ✅ Try absolute path first, then by name
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
    }
}