using JsSampleReport.Dtos.ReportDtos;
using Microsoft.Extensions.Options;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace JsSampleReport.Utils.Report
{
    public static class ReportUtils
    {
        // ══════════════════════════════════════════════════════════════════
        // Generates deterministic cache key from ANY request object
        // ══════════════════════════════════════════════════════════════════
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

        // ══════════════════════════════════════════════════════════════════
        // Resolve WebRootPath from appsettings.json → ReportSettings
        // Falls back to IWebHostEnvironment → then hardcoded path
        // ══════════════════════════════════════════════════════════════════
        public static string GetWebRootPath(
            IWebHostEnvironment env,
            IOptions<ReportSettings> reportSettings,
            ILogger logger)
        {
            // 1️⃣ First priority — appsettings.json ReportSettings:WebRootPath
            var configPath = reportSettings?.Value?.WebRootPath;
            if (!string.IsNullOrWhiteSpace(configPath) && Directory.Exists(configPath))
            {
                logger.LogInformation($"✅ WebRootPath from appsettings: {configPath}");
                return configPath;
            }

            // 2️⃣ Second priority — IWebHostEnvironment.WebRootPath
            if (!string.IsNullOrWhiteSpace(env?.WebRootPath) && Directory.Exists(env.WebRootPath))
            {
                logger.LogInformation($"✅ WebRootPath from env: {env.WebRootPath}");
                return env.WebRootPath;
            }

            // 3️⃣ Third priority — ContentRootPath/wwwroot
            var contentRoot = env?.ContentRootPath;
            if (!string.IsNullOrWhiteSpace(contentRoot))
            {
                var combined = Path.Combine(contentRoot, "wwwroot");
                if (Directory.Exists(combined))
                {
                    logger.LogInformation($"✅ WebRootPath from ContentRoot: {combined}");
                    return combined;
                }
            }

            // 4️⃣ Last fallback — log warning
            logger.LogWarning("⚠️ WebRootPath not resolved. Check ReportSettings:WebRootPath in appsettings.json");
            return @"C:\inetpub\wwwroot";
        }

        // ══════════════════════════════════════════════════════════════════
        // Convert logo relative path → base64 data URL
        // ══════════════════════════════════════════════════════════════════

        public static void ConvertImagesToBase64<T>(
    IEnumerable<T> items,
    string propertyName,
    string webRootPath,
    ILogger logger)
        {
            if (items == null) return;

            var property = typeof(T).GetProperty(propertyName)
                ?? throw new ArgumentException($"Property '{propertyName}' not found on {typeof(T).Name}");

            foreach (var item in items)
            {
                var imagePath = property.GetValue(item) as string;
                if (string.IsNullOrWhiteSpace(imagePath)) continue;

                try
                {
                    var fullPath = Path.Combine(webRootPath,
                        imagePath.Replace('/', Path.DirectorySeparatorChar)
                                 .TrimStart(Path.DirectorySeparatorChar));

                    if (!File.Exists(fullPath))
                    {
                        logger.LogWarning($"❌ Image not found: {fullPath}");
                        property.SetValue(item, string.Empty);
                        continue;
                    }

                    var ext = Path.GetExtension(fullPath).TrimStart('.').ToLower();
                    var mimeType = ext switch
                    {
                        "jpg" or "jpeg" => "image/jpeg",
                        "gif" => "image/gif",
                        "bmp" => "image/bmp",
                        "webp" => "image/webp",
                        _ => "image/png"
                    };

                    var base64 = Convert.ToBase64String(File.ReadAllBytes(fullPath));
                    property.SetValue(item, $"data:{mimeType};base64,{base64}");
                    logger.LogInformation("✅ Image converted to base64.");
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, $"Image conversion failed: {imagePath}");
                    property.SetValue(item, string.Empty);
                }
            }
        }

        //public static void ConvertLogoToBase64(
        //    IEnumerable<CommonHeader> headers,
        //    string webRootPath,
        //    ILogger logger)
        //{
        //    if (headers == null) return;

        //    foreach (var header in headers)
        //    {
        //        if (string.IsNullOrWhiteSpace(header.CompanyLogo)) continue;

        //        try
        //        {
        //            var relativePath = header.CompanyLogo
        //                .Replace('/', Path.DirectorySeparatorChar)
        //                .Replace('\\', Path.DirectorySeparatorChar)
        //                .TrimStart(Path.DirectorySeparatorChar);

        //            var fullPath = Path.Combine(webRootPath, relativePath);

        //            logger.LogInformation($"Logo resolved path: {fullPath}");

        //            if (!File.Exists(fullPath))
        //            {
        //                logger.LogWarning($"❌ Logo not found: {fullPath}");
        //                header.CompanyLogo = string.Empty;
        //                continue;
        //            }

        //            var imgBytes = File.ReadAllBytes(fullPath);
        //            var base64 = Convert.ToBase64String(imgBytes);
        //            var ext = Path.GetExtension(fullPath).TrimStart('.').ToLower();

        //            var mimeType = ext switch
        //            {
        //                "png" => "image/png",
        //                "jpg" or "jpeg" => "image/jpeg",
        //                "gif" => "image/gif",
        //                "bmp" => "image/bmp",
        //                "webp" => "image/webp",
        //                _ => "image/png"
        //            };

        //            header.CompanyLogo = $"data:{mimeType};base64,{base64}";
        //            logger.LogInformation("✅ Logo converted to base64 successfully.");
        //        }
        //        catch (Exception ex)
        //        {
        //            logger.LogError(ex, $"Logo conversion failed: {header.CompanyLogo}");
        //            header.CompanyLogo = string.Empty;
        //        }
        //    }
        //}

        // ══════════════════════════════════════════════════════════════════
        // Content type + extension for any format
        // ══════════════════════════════════════════════════════════════════
        public static (string contentType, string extension)
            GetContentTypeAndExtension(string format) => format switch
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

        // ══════════════════════════════════════════════════════════════════
        // Timestamped filename for any report download
        // ══════════════════════════════════════════════════════════════════
        public static string GetFileName(string reportName, string format)
        {
            var (_, ext) = GetContentTypeAndExtension(format);
            return $"{reportName}_{DateTime.Now:yyyyMMddHHmmss}.{ext}";
        }
    }
}
