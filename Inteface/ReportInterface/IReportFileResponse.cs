using Microsoft.AspNetCore.Mvc;
using NexgenCosysReport.Services.ReportService;

namespace NexgenCosysReport.Inteface.ReportInterface
{
    public interface IReportFileResponse
    {
        FileContentResult BuildPdfResponse(byte[] pdfBytes, int totalRecords);
        FileStreamResult BuildPdfStreamResponse(string pdfPath, int totalRecords);

        static int CountPdfPagesFromFile(string pdfPath) =>
            ReportFileResponse.CountPdfPagesFromFile(pdfPath);
    }
}