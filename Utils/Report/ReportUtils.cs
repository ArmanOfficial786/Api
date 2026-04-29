using JsSampleReport.Dtos.ReportDtos;
using Microsoft.Extensions.Options;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace JsSampleReport.Utils.Report
{
    public static class ReportUtils
    {
        // ── Deterministic cache key ───────────────────────────────────────────────
        public static string GenerateReportKey<TRequest>(
            TRequest request,
            string reportPrefix = "Report")
        {
            var json = JsonSerializer.Serialize(request,
                new JsonSerializerOptions { WriteIndented = false });
            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(json));
            var hash = Convert.ToHexString(bytes)[..16];
            return $"{reportPrefix}_{hash}";
        }

        // ── Resolve wwwroot path ──────────────────────────────────────────────────
        public static string GetWebRootPath(
            IWebHostEnvironment env,
            IOptions<ReportSettings> reportSettings,
            ILogger logger)
        {
            var configPath = reportSettings?.Value?.WebRootPath;
            if (!string.IsNullOrWhiteSpace(configPath) && Directory.Exists(configPath))
            {
                logger.LogInformation("✅ WebRootPath from appsettings: {Path}", configPath);
                return configPath;
            }

            if (!string.IsNullOrWhiteSpace(env?.WebRootPath) && Directory.Exists(env.WebRootPath))
            {
                logger.LogInformation("✅ WebRootPath from env: {Path}", env.WebRootPath);
                return env.WebRootPath;
            }

            var contentRoot = env?.ContentRootPath;
            if (!string.IsNullOrWhiteSpace(contentRoot))
            {
                var combined = Path.Combine(contentRoot, "wwwroot");
                if (Directory.Exists(combined))
                {
                    logger.LogInformation("✅ WebRootPath from ContentRoot: {Path}", combined);
                    return combined;
                }
            }

            logger.LogWarning("⚠️ WebRootPath not resolved. Check ReportSettings:WebRootPath");
            return @"C:\inetpub\wwwroot\Images";
        }

        // ── Read + compress shared image (logo, signatures) ───────────────────────
        // ✅ Returns full data URL: "data:image/jpeg;base64,..."
        //    Required by <img src="..."> in Razor HTML for jsreport PDF rendering
        public static async Task<string> ReadCommonImageAsBase64Async(
            string webRootPath,
            string relativePath,
            ILogger logger,
            int maxWidth = 400,
            int maxHeight = 200,
            int quality = 80)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(relativePath)) return string.Empty;

                var fullPath = Path.Combine(webRootPath,
                    relativePath.Replace('/', Path.DirectorySeparatorChar)
                                .TrimStart(Path.DirectorySeparatorChar));

                if (!File.Exists(fullPath))
                {
                    logger.LogWarning("❌ Image not found: {Path}", fullPath);
                    return string.Empty;
                }

                var bytes = await File.ReadAllBytesAsync(fullPath);
                var extension = Path.GetExtension(fullPath);

                // ✅ Returns "data:image/jpeg;base64,..." — <img src> ready
                return await ImageUtils.CompressImageToBase64WithMimeAsync(
                    bytes, extension, maxWidth, maxHeight, quality);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "❌ ReadCommonImageAsBase64Async failed: {File}", relativePath);
                return string.Empty;
            }
        }

        // ── Read + compress unique per-member images (MemberPhoto) ───────────────
        // ✅ Returns full data URL: "data:image/jpeg;base64,..."
        //    200×200px at 70% quality → ~10–20KB per photo
        public static async Task ConvertUniqueImagesToBase64Async<T>(
            IEnumerable<T> items,
            string propertyName,
            string webRoot,
            ILogger logger,
            int maxWidth = 200,
            int maxHeight = 200,
            int quality = 70)
        {
            var prop = typeof(T).GetProperty(propertyName);
            if (prop == null) return;

            var tasks = items.Select(async item =>
            {
                var relativePath = prop.GetValue(item) as string;
                if (string.IsNullOrWhiteSpace(relativePath)) return;

                try
                {
                    var fullPath = Path.Combine(webRoot,
                        relativePath.Replace('/', Path.DirectorySeparatorChar)
                                    .TrimStart(Path.DirectorySeparatorChar));

                    if (!File.Exists(fullPath))
                    {
                        logger.LogWarning("Member image not found: {Path}", fullPath);
                        prop.SetValue(item, string.Empty);
                        return;
                    }

                    var bytes = await File.ReadAllBytesAsync(fullPath);
                    var extension = Path.GetExtension(fullPath);

                    // ✅ Returns "data:image/jpeg;base64,..." — <img src> ready
                    var dataUrl = await ImageUtils.CompressImageToBase64WithMimeAsync(
                        bytes, extension, maxWidth, maxHeight, quality);

                    prop.SetValue(item, dataUrl);

                    logger.LogInformation(
                        "✅ Compressed member photo: {File} ({Bytes} bytes → data URL)",
                        Path.GetFileName(fullPath), bytes.Length);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "❌ Member image conversion failed: {Path}", relativePath);
                    prop.SetValue(item, string.Empty);
                }
            });

            await Task.WhenAll(tasks);
        }

        // ── Content-Type + extension ──────────────────────────────────────────────
        public static (string contentType, string extension)
            GetContentTypeAndExtension(string format) => format.ToUpper() switch
            {
                "VIEW" => ("application/pdf", "pdf"),
                "HTML" => ("text/html", "html"),
                "PDF" => ("application/pdf", "pdf"),
                "EXCEL" or "XLSX" => ("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "xlsx"),
                "WORD" or "DOCX" => ("application/vnd.openxmlformats-officedocument.wordprocessingml.document", "docx"),
                "PNG" => ("image/png", "png"),
                "CSV" => ("text/csv", "csv"),
                _ => ("application/pdf", "pdf"),
            };

        // ── Timestamped download filename ─────────────────────────────────────────
        public static string GetFileName(string reportName, string format)
        {
            var (_, ext) = GetContentTypeAndExtension(format);
            return $"{reportName}_{DateTime.Now:yyyyMMddHHmmss}.{ext}";
        }
    }
}


