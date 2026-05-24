using iText.Kernel.Pdf;
using Microsoft.AspNetCore.Mvc;
using NexgenCosysReport.Dtos.RequestDtos.Common;
using NexgenCosysReport.Inteface.ReportInterface;
using NexgenCosysReport.Utils.Report;
using System.Text.Json;

namespace NexgenCosysReport.Services.ReportService
{
    public class ReportFileResponse : IReportFileResponse
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly CustomHeaderResponse _headerResponse;
        private readonly ILogger<ReportFileResponse> _logger;

        // Always resolves the current request's HttpResponse
        private HttpResponse Response =>
            _httpContextAccessor.HttpContext?.Response
            ?? throw new InvalidOperationException("No active HTTP context.");

        public ReportFileResponse(
            IHttpContextAccessor httpContextAccessor,
            CustomHeaderResponse headerResponse,
            ILogger<ReportFileResponse> logger)
        {
            _httpContextAccessor = httpContextAccessor;
            _headerResponse = headerResponse;
            _logger = logger;
        }

        // ═══════════════════════════════════════════════════════════════════
        // PUBLIC — IReportFileResponse implementation
        // ═══════════════════════════════════════════════════════════════════

        /// <summary>
        /// Builds a FileContentResult from in-memory PDF bytes.
        /// Used for small datasets whose PDF fits in cache.
        /// </summary>
        public FileContentResult BuildPdfResponse(byte[] pdfBytes, int totalRecords)
        {
            var totalPages = JsReportService.CountPdfPages(pdfBytes);

            _logger.LogInformation("📄 PDF — {Pages} pages, {MB:F2} MB",
                totalPages, pdfBytes.Length / 1024.0 / 1024.0);

            AppendPaginationHeaders(totalPages, totalRecords);
            Response.Headers.Append("Content-Disposition",
                "inline; filename=\"SavingAcWiseBalance.pdf\"");

            return new FileContentResult(pdfBytes, "application/pdf");
        }

        /// <summary>
        /// Builds a FileStreamResult streamed directly from disk.
        /// Used for large chunked PDFs that must not be loaded into memory.
        /// </summary>
        public FileStreamResult BuildPdfStreamResponse(string pdfPath, int totalRecords)
        {
            var totalPages = CountPdfPagesFromFile(pdfPath);

            _logger.LogInformation("📄 Chunked PDF — {Pages} pages, path={Path}",
                totalPages, pdfPath);

            var fs = new FileStream(
                pdfPath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read,
                bufferSize: 81_920,
                options: FileOptions.Asynchronous | FileOptions.SequentialScan);

            AppendPaginationHeaders(totalPages, totalRecords);
            Response.Headers.Append("Content-Disposition",
                "inline; filename=\"SavingAcWiseBalance.pdf\"");

            return new FileStreamResult(fs, "application/pdf");
        }

        // ═══════════════════════════════════════════════════════════════════
        // PUBLIC STATIC — called by the interface default implementation
        // ═══════════════════════════════════════════════════════════════════

        /// <summary>
        /// Reads page count directly from a PDF file on disk using iText.
        /// Static so the interface default method can call it without a DI instance.
        /// Falls back to 1 if the file is unreadable.
        /// </summary>
        public static int CountPdfPagesFromFile(string pdfPath)
        {
            try
            {
                using var fs = new FileStream(pdfPath, FileMode.Open, FileAccess.Read, FileShare.Read);
                using var reader = new PdfReader(fs);
                using var doc = new PdfDocument(reader);
                return doc.GetNumberOfPages();
            }
            catch { return 1; }
        }

        // ═══════════════════════════════════════════════════════════════════
        // PRIVATE
        // ═══════════════════════════════════════════════════════════════════

        private void AppendPaginationHeaders(int totalPages, int totalRecords)
        {
            var pagination = new Pagination
            {
                currentPage = 1,
                totalPages = totalPages,
                totalRecord = totalRecords,
                pageSize = 1,
                hasNextPage = totalPages > 1,
                hasPreviousPage = false
            };

            _headerResponse.SetResponseHeaders(true, 200, "Report generated successfully.");
            Response.Headers.Append("X-Pagination", JsonSerializer.Serialize(pagination));
        }
    }
}