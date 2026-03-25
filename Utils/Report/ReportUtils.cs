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
            return @"C:\inetpub\wwwroot\Images";
        }

        // ══════════════════════════════════════════════════════════════════
        // Convert logo relative path → base64 data URL
        // ══════════════════════════════════════════════════════════════════

        //    public static void ConvertImagesToBase64<T>(
        //IEnumerable<T> items,
        //string propertyName,
        //string webRootPath,
        //ILogger logger)
        //    {
        //        if (items == null) return;

        //        var property = typeof(T).GetProperty(propertyName)
        //            ?? throw new ArgumentException($"Property '{propertyName}' not found on {typeof(T).Name}");

        //        foreach (var item in items)
        //        {
        //            var imagePath = property.GetValue(item) as string;
        //            if (string.IsNullOrWhiteSpace(imagePath)) continue;

        //            try
        //            {
        //                var fullPath = Path.Combine(webRootPath,
        //                    imagePath.Replace('/', Path.DirectorySeparatorChar)
        //                             .TrimStart(Path.DirectorySeparatorChar));

        //                if (!File.Exists(fullPath))
        //                {
        //                    logger.LogWarning($"❌ Image not found: {fullPath}");
        //                    property.SetValue(item, string.Empty);
        //                    continue;
        //                }

        //                var ext = Path.GetExtension(fullPath).TrimStart('.').ToLower();
        //                var mimeType = ext switch
        //                {
        //                    "jpg" or "jpeg" => "image/jpeg",
        //                    "gif" => "image/gif",
        //                    "bmp" => "image/bmp",
        //                    "webp" => "image/webp",
        //                    _ => "image/png"
        //                };

        //                var base64 = Convert.ToBase64String(File.ReadAllBytes(fullPath));
        //                property.SetValue(item, $"data:{mimeType};base64,{base64}");
        //                logger.LogInformation("✅ Image converted to base64.");
        //            }
        //            catch (Exception ex)
        //            {
        //                logger.LogError(ex, $"Image conversion failed: {imagePath}");
        //                property.SetValue(item, string.Empty);
        //            }
        //        }
        //    }


        // ══════════════════════════════════════════════════════════════════
        // Read ONE static image — ASYNC
        // Use for signatures/logo loaded ONCE not per-member
        // ══════════════════════════════════════════════════════════════════
        public static async Task<string> ReadCommonImageAsBase64Async(
            string webRootPath,
            string relativePath,
            ILogger logger)
        {
            try
            {
                var fullPath = Path.Combine(webRootPath,
                    relativePath.Replace('/', Path.DirectorySeparatorChar)
                                .TrimStart(Path.DirectorySeparatorChar));

                if (!File.Exists(fullPath))
                {
                    logger.LogWarning("❌ Image not found: {Path}", fullPath);
                    return string.Empty;
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

                // ✅ True async file read
                var fileBytes = await File.ReadAllBytesAsync(fullPath);
                var base64 = Convert.ToBase64String(fileBytes);

                logger.LogInformation(
                    "✅ [Loaded] {File} ({Mime}) | {Kb}KB",
                    relativePath, mimeType, fileBytes.Length / 1024);

                return $"data:{mimeType};base64,{base64}";
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "❌ ReadCommonImageAsBase64Async failed: {File}", relativePath);
                return string.Empty;
            }
        }

        // ══════════════════════════════════════════════════════════════════
        // Convert image path → base64 data URL — TRUE ASYNC + PARALLEL per item
        // ══════════════════════════════════════════════════════════════════
        public static async Task ConvertUniqueImagesToBase64Async<T>(
            IEnumerable<T> items,
            string propertyName,
            string webRootPath,
            ILogger logger)
        {
            if (items == null) return;

            var property = typeof(T).GetProperty(propertyName)
                ?? throw new ArgumentException(
                       $"Property '{propertyName}' not found on {typeof(T).Name}");

            // ✅ All items processed concurrently — true async file reads
            var tasks = items.Select(async item =>
            {
                var imagePath = property.GetValue(item) as string;
                if (string.IsNullOrWhiteSpace(imagePath)) return;

                try
                {
                    var fullPath = Path.Combine(webRootPath,
                        imagePath.Replace('/', Path.DirectorySeparatorChar)
                                 .TrimStart(Path.DirectorySeparatorChar));

                    if (!File.Exists(fullPath))
                    {
                        logger.LogWarning("❌ Image not found: {Path}", fullPath);
                        property.SetValue(item, string.Empty);
                        return;
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

                    // ✅ True async file read — no thread blocked
                    var fileBytes = await File.ReadAllBytesAsync(fullPath);
                    var base64 = Convert.ToBase64String(fileBytes);

                    property.SetValue(item, $"data:{mimeType};base64,{base64}");

                    logger.LogInformation(
                        "✅ [Converted] {File} ({Mime})",
                        Path.GetFileName(fullPath), mimeType);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "❌ Image conversion failed: {Path}", imagePath);
                    property.SetValue(item, string.Empty);
                }
            });

            // ✅ All items converted concurrently
            await Task.WhenAll(tasks);
        }



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







//using JsSampleReport.Dtos.ReportDtos;
//using Microsoft.Extensions.Caching.Memory;
//using Microsoft.Extensions.Options;
//using System.Collections.Concurrent;
//using System.Security.Cryptography;
//using System.Text;
//using System.Text.Json;

//namespace JsSampleReport.Utils.Report
//{
//    public static class ReportUtils
//    {
//        // ══════════════════════════════════════════════════════════════════
//        // In-memory image cache — same file path = read once, reuse forever
//        // Company logo, Auth signature same for ALL members — read only once
//        // ══════════════════════════════════════════════════════════════════
//        private static readonly ConcurrentDictionary<string, string> _imageCache
//            = new(StringComparer.OrdinalIgnoreCase);

//        // ══════════════════════════════════════════════════════════════════
//        // Generates deterministic cache key from ANY request object
//        // ══════════════════════════════════════════════════════════════════
//        public static string GenerateReportKey<TRequest>(
//            TRequest request,
//            string reportPrefix = "Report")
//        {
//            var json = JsonSerializer.Serialize(request,
//                            new JsonSerializerOptions { WriteIndented = false });
//            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(json));
//            var hash = Convert.ToHexString(bytes)[..16];
//            return $"{reportPrefix}_{hash}";
//        }

//        // ══════════════════════════════════════════════════════════════════
//        // Resolve WebRootPath — appsettings first, no Directory.Exists check
//        // ══════════════════════════════════════════════════════════════════
//        public static string GetWebRootPath(
//            IWebHostEnvironment env,
//            IOptions<ReportSettings> reportSettings,
//            ILogger logger)
//        {
//            var configPath = reportSettings?.Value?.WebRootPath;
//            if (!string.IsNullOrWhiteSpace(configPath))
//            {
//                logger.LogInformation($"✅ WebRootPath from appsettings: {configPath}");
//                return configPath;
//            }

//            if (!string.IsNullOrWhiteSpace(env?.WebRootPath))
//            {
//                logger.LogInformation($"✅ WebRootPath from env: {env.WebRootPath}");
//                return env.WebRootPath;
//            }

//            var contentRoot = env?.ContentRootPath;
//            if (!string.IsNullOrWhiteSpace(contentRoot))
//            {
//                var combined = Path.Combine(contentRoot, "wwwroot");
//                logger.LogInformation($"✅ WebRootPath from ContentRoot: {combined}");
//                return combined;
//            }

//            logger.LogWarning("⚠️ WebRootPath not resolved — using fallback");
//            return @"C:\inetpub\wwwroot\Images";
//        }

//        // ══════════════════════════════════════════════════════════════════
//        // ✅ OPTIMIZED — Parallel image conversion with in-memory image cache
//        //
//        // Before: Sequential — 10 members × 150KB × file read = slow
//        // After:  Parallel  — all members processed simultaneously
//        //         + same image path reused from cache (logo, auth sig)
//        // ══════════════════════════════════════════════════════════════════
//        public static void ConvertImagesToBase64<T>(
//            IEnumerable<T> items,
//            string propertyName,
//            string webRootPath,
//            ILogger logger)
//        {
//            if (items == null) return;

//            var property = typeof(T).GetProperty(propertyName)
//                ?? throw new ArgumentException(
//                    $"Property '{propertyName}' not found on {typeof(T).Name}");

//            var itemList = items.ToList();

//            // ✅ Parallel.ForEach — all items processed at the same time
//            Parallel.ForEach(itemList, item =>
//            {
//                var imagePath = property.GetValue(item) as string;
//                if (string.IsNullOrWhiteSpace(imagePath)) return;
//                if (imagePath.StartsWith("data:")) return; // already base64

//                try
//                {
//                    var normalizedPath = imagePath
//                        .Replace('/', Path.DirectorySeparatorChar)
//                        .Replace('\\', Path.DirectorySeparatorChar)
//                        .TrimStart(Path.DirectorySeparatorChar);

//                    var fullPath = Path.Combine(webRootPath, normalizedPath);

//                    // ✅ Check in-memory image cache first
//                    // Same path (e.g. CompanyLogo.png) read only ONCE
//                    // All subsequent calls return cached base64 string
//                    if (_imageCache.TryGetValue(fullPath, out var cachedBase64))
//                    {
//                        property.SetValue(item, cachedBase64);
//                        logger.LogInformation(
//                            $"✅ [ImageCache HIT] {Path.GetFileName(fullPath)}");
//                        return;
//                    }

//                    if (!File.Exists(fullPath))
//                    {
//                        logger.LogWarning($"❌ Image not found: {fullPath}");
//                        property.SetValue(item, string.Empty);
//                        return;
//                    }

//                    // ✅ Read file and detect real MIME from magic bytes
//                    var fileBytes = File.ReadAllBytes(fullPath);
//                    var mimeType = GetRealMimeType(fileBytes,
//                                        Path.GetExtension(fullPath)
//                                            .TrimStart('.').ToLower(),
//                                        logger);

//                    var base64DataUrl = $"data:{mimeType};base64,"
//                                      + Convert.ToBase64String(fileBytes);

//                    // ✅ Store in image cache for reuse
//                    _imageCache[fullPath] = base64DataUrl;

//                    property.SetValue(item, base64DataUrl);
//                    logger.LogInformation(
//                        $"✅ [Converted] {Path.GetFileName(fullPath)} ({mimeType})");
//                }
//                catch (Exception ex)
//                {
//                    logger.LogError(ex, $"Image conversion failed: {imagePath}");
//                    property.SetValue(item, string.Empty);
//                }
//            });
//        }

//        // ══════════════════════════════════════════════════════════════════
//        // Detect real MIME type from magic bytes — not extension
//        // Handles renamed files (jpg renamed to png etc.)
//        // ══════════════════════════════════════════════════════════════════
//        private static string GetRealMimeType(
//            byte[] bytes, string fallbackExt, ILogger logger)
//        {
//            // JPEG — FF D8 FF
//            if (bytes.Length >= 3 &&
//                bytes[0] == 0xFF && bytes[1] == 0xD8 && bytes[2] == 0xFF)
//                return "image/jpeg";

//            // PNG — 89 50 4E 47 0D 0A 1A 0A
//            if (bytes.Length >= 8 &&
//                bytes[0] == 0x89 && bytes[1] == 0x50 &&
//                bytes[2] == 0x4E && bytes[3] == 0x47 &&
//                bytes[4] == 0x0D && bytes[5] == 0x0A &&
//                bytes[6] == 0x1A && bytes[7] == 0x0A)
//                return "image/png";

//            // GIF — 47 49 46
//            if (bytes.Length >= 3 &&
//                bytes[0] == 0x47 && bytes[1] == 0x49 && bytes[2] == 0x46)
//                return "image/gif";

//            // BMP — 42 4D
//            if (bytes.Length >= 2 &&
//                bytes[0] == 0x42 && bytes[1] == 0x4D)
//                return "image/bmp";

//            // WebP — RIFF....WEBP
//            if (bytes.Length >= 12 &&
//                bytes[0] == 0x52 && bytes[1] == 0x49 &&
//                bytes[2] == 0x46 && bytes[3] == 0x46 &&
//                bytes[8] == 0x57 && bytes[9] == 0x45 &&
//                bytes[10] == 0x42 && bytes[11] == 0x50)
//                return "image/webp";

//            // Fallback to extension
//            return fallbackExt switch
//            {
//                "jpg" or "jpeg" => "image/jpeg",
//                "png" => "image/png",
//                "gif" => "image/gif",
//                "bmp" => "image/bmp",
//                "webp" => "image/webp",
//                _ => "image/jpeg"
//            };
//        }

//        // ══════════════════════════════════════════════════════════════════
//        // Content type + extension for any format
//        // ══════════════════════════════════════════════════════════════════
//        public static (string contentType, string extension)
//            GetContentTypeAndExtension(string format) => format switch
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

//        public static string GetFileName(string reportName, string format)
//        {
//            var (_, ext) = GetContentTypeAndExtension(format);
//            return $"{reportName}_{DateTime.Now:yyyyMMddHHmmss}.{ext}";
//        }
//    }
//}