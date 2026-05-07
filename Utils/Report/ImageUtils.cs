//using SixLabors.ImageSharp;
//using SixLabors.ImageSharp.Formats.Jpeg;
//using SixLabors.ImageSharp.Processing;

//namespace NexgenCosysReport.Utils.Report
//{
//    public static class ImageUtils
//    {
//        // -- Resize + compress image bytes ? JPEG at target quality ---------------
//        // MemberPhoto: 200×200px, 70% quality  ? ~15KB instead of ~3MB
//        // Signature  : 300×100px, 80% quality  ? ~8KB
//        // Logo       : 400×150px, 80% quality  ? ~20KB
//        public static async Task<byte[]> CompressImageAsync(
//            byte[] imageBytes,
//            int maxWidth,
//            int maxHeight,
//            int quality = 75)
//        {
//            if (imageBytes == null || imageBytes.Length == 0)
//                return imageBytes ?? [];

//            try
//            {
//                using var image = Image.Load(imageBytes);
//                using var output = new MemoryStream();

//                // ? Only downscale — never upscale small images
//                if (image.Width > maxWidth || image.Height > maxHeight)
//                {
//                    image.Mutate(x => x.Resize(new ResizeOptions
//                    {
//                        Size = new Size(maxWidth, maxHeight),
//                        Mode = ResizeMode.Max,   // preserve aspect ratio
//                    }));
//                }

//                await image.SaveAsJpegAsync(output, new JpegEncoder
//                {
//                    Quality = quality,
//                });

//                return output.ToArray();
//            }
//            catch
//            {
//                // If compression fails, return original — never crash report
//                return imageBytes;
//            }
//        }

//        // -- Compress + return base64 ----------------------------------------------
//        public static async Task<string> CompressImageToBase64Async(
//            byte[] imageBytes,
//            int maxWidth,
//            int maxHeight,
//            int quality = 75)
//        {
//            var compressed = await CompressImageAsync(imageBytes, maxWidth, maxHeight, quality);
//            return Convert.ToBase64String(compressed);
//        }
//    }
//}




using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Processing;

namespace NexgenCosysReport.Utils.Report
{
    public static class ImageUtils
    {
        // -- Resize + compress image bytes ? JPEG ---------------------------------
        public static async Task<byte[]> CompressImageAsync(
            byte[] imageBytes,
            int maxWidth,
            int maxHeight,
            int quality = 75)
        {
            if (imageBytes == null || imageBytes.Length == 0)
                return imageBytes ?? [];

            try
            {
                using var image = Image.Load(imageBytes);
                using var output = new MemoryStream();

                // ? Only downscale — never upscale small images
                if (image.Width > maxWidth || image.Height > maxHeight)
                {
                    image.Mutate(x => x.Resize(new ResizeOptions
                    {
                        Size = new Size(maxWidth, maxHeight),
                        Mode = ResizeMode.Max,  // preserve aspect ratio
                    }));
                }

                await image.SaveAsJpegAsync(output, new JpegEncoder
                {
                    Quality = quality,
                });

                return output.ToArray();
            }
            catch
            {
                // Compression failed — return original bytes, never crash report
                return imageBytes;
            }
        }

        // -- Compress + return FULL data URL (with MIME prefix) --------------------
        // ? Always returns "data:image/jpeg;base64,..." 
        //    <img src="..."> in Razor/HTML requires this exact format
        //    jsreport converts the HTML to PDF — images must be data URLs
        public static async Task<string> CompressImageToBase64Async(
            byte[] imageBytes,
            int maxWidth,
            int maxHeight,
            int quality = 75)
        {
            var compressed = await CompressImageAsync(imageBytes, maxWidth, maxHeight, quality);

            // ? Always jpeg after compression — prefix must match
            return $"data:image/jpeg;base64,{Convert.ToBase64String(compressed)}";
        }

        // -- Keep original format — compress + return data URL with correct MIME ---
        // Use this when you cannot convert to JPEG (e.g. PNG with transparency)
        public static async Task<string> CompressImageToBase64WithMimeAsync(
            byte[] imageBytes,
            string originalExtension,
            int maxWidth,
            int maxHeight,
            int quality = 75)
        {
            // ? Detect original mime before compression changes format
            var mimeType = originalExtension.ToLower().TrimStart('.') switch
            {
                "jpg" or "jpeg" => "image/jpeg",
                "gif" => "image/gif",
                "bmp" => "image/bmp",
                "webp" => "image/webp",
                _ => "image/png",
            };

            var compressed = await CompressImageAsync(imageBytes, maxWidth, maxHeight, quality);

            // ? After compression output is always JPEG — override mime
            return $"data:image/jpeg;base64,{Convert.ToBase64String(compressed)}";
        }
    }
}