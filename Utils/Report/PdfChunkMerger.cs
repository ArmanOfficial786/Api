// Utils/Report/PdfChunkMerger.cs
using iText.Kernel.Pdf;
using iText.Kernel.Utils;

namespace NexgenCosysReport.Utils.Report
{
    public static class PdfChunkMerger
    {
        /// <summary>
        /// Merge multiple PDF byte arrays into a single PDF using iText7.
        /// </summary>
        public static byte[] MergeChunks(IEnumerable<byte[]> pdfChunks, ILogger logger)
        {
            using var outputStream = new MemoryStream();
            using var writer = new PdfWriter(outputStream);
            using var mergedDoc = new PdfDocument(writer);
            var merger = new PdfMerger(mergedDoc);

            int chunkIndex = 0;
            foreach (var chunkBytes in pdfChunks)
            {
                try
                {
                    using var chunkStream = new MemoryStream(chunkBytes);
                    using var reader = new PdfReader(chunkStream);
                    using var chunkDoc = new PdfDocument(reader);

                    merger.Merge(chunkDoc, 1, chunkDoc.GetNumberOfPages());
                    logger.LogInformation("✅ Merged chunk {Index} ({Pages} pages)",
                        ++chunkIndex, chunkDoc.GetNumberOfPages());
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "❌ Failed to merge chunk {Index}", ++chunkIndex);
                    throw;
                }
            }

            mergedDoc.Close();
            return outputStream.ToArray();
        }
    }
}