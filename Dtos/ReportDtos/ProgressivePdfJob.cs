namespace NexgenCosysReport.Dtos.ReportDtos
{
    public class ProgressivePdfJob
    {
        public string JobId { get; init; } = Guid.NewGuid().ToString("N");
        public string LivePdfPath { get; init; } = "";
        public string TempDir { get; init; } = "";

        public int TotalChunks { get; set; }
        public int CompletedChunks { get; set; }
        public int PagesReady { get; set; }
        public int EstimatedTotalPages { get; set; }
        public long CurrentSizeBytes { get; set; }

        public bool IsComplete => CompletedChunks >= TotalChunks && TotalChunks > 0;
        public bool HasError { get; set; }
        public string? ErrorMessage { get; set; }

        public DateTime StartedAt { get; } = DateTime.UtcNow;
        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;

        // Synchronizes file access between writer (background task) and reader (HTTP).
        public readonly SemaphoreSlim FileLock = new(1, 1);
    }
}
