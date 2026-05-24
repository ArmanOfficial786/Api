using NexgenCosysReport.Dtos.ReportDtos;

namespace NexgenCosysReport.Inteface.ReportInterface
{
    public interface IPdfChunkService
    {
        // <summary>
        /// Returns the cached merged-PDF path for this reportKey if it still exists on disk.
        /// </summary>
        bool TryGetChunkedPdfPath(string reportKey, out string? pdfPath);

        /// <summary>
        /// Renders rows in parallel chunks → merges → stamps page numbers → returns merged file path.
        /// </summary>
        Task<string> ExportChunkedPdfAsync(
            string reportKey,
            string reportPath,
            IDictionary<string, object> reportData,
            string rowsKey,
            int chunkSize,
            PageSizeSetting? pageSetting = null,
            int maxParallelism = 2,
            CancellationToken ct = default);
    }
}
