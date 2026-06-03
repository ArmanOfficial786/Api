//namespace NexgenCosysReport.Dtos.ReportDtos
//{
//    public class ProgressivePdfJob
//    {
//        public string JobId { get; init; } = Guid.NewGuid().ToString("N");
//        public string LivePdfPath { get; init; } = "";
//        public string TempDir { get; init; } = "";

//        public int TotalChunks { get; set; }
//        public int CompletedChunks { get; set; }
//        public int PagesReady { get; set; }
//        public int EstimatedTotalPages { get; set; }
//        public long CurrentSizeBytes { get; set; }

//        public bool IsComplete => CompletedChunks >= TotalChunks && TotalChunks > 0;
//        public bool HasError { get; set; }
//        public string? ErrorMessage { get; set; }

//        public DateTime StartedAt { get; } = DateTime.UtcNow;
//        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;

//        // Synchronizes file access between writer (background task) and reader (HTTP).
//        public readonly SemaphoreSlim FileLock = new(1, 1);
//    }
//}





// ProgressivePdfJob.cs
//namespace NexgenCosysReport.Dtos.ReportDtos
//{
//    /// <summary>
//    /// Tracks the state of one progressive-PDF render job.
//    ///
//    /// LIFECYCLE
//    /// ─────────
//    /// 1. StartAsync creates the job and stores it in IMemoryCache (30 min sliding / 60 min absolute).
//    /// 2. Background task appends compressed chunks into <see cref="BasePdfPath"/> (unstamped).
//    /// 3. Each GET /progressive/{jobId} call stamps BasePdfPath into a one-shot snapshot,
//    ///    reads the bytes, deletes the snapshot, and returns the PDF with progress headers.
//    /// 4. When the cache entry is evicted the PostEvictionCallback deletes <see cref="TempDir"/>.
//    ///
//    /// THREAD SAFETY
//    /// ─────────────
//    /// All reads/writes to BasePdfPath must be done while holding <see cref="FileLock"/>.
//    /// Scalar properties (PagesReady, CompletedChunks, …) are set by the single background
//    /// task or by the first-chunk await, so they are effectively sequentially consistent for
//    /// polling clients that only need an approximate progress value.
//    /// </summary>
//    public class ProgressivePdfJob
//    {
//        /// <summary>Unique job identifier returned to the client.</summary>
//        public string JobId { get; } = Guid.NewGuid().ToString("N");

//        /// <summary>
//        /// Path to the UNSTAMPED, compressed accumulator PDF.
//        /// Populated by StartAsync; never handed directly to clients.
//        /// </summary>
//        public string BasePdfPath { get; set; } = string.Empty;

//        /// <summary>Working directory for all temp files belonging to this job.</summary>
//        public string TempDir { get; set; } = string.Empty;

//        // ── Progress ──────────────────────────────────────────────────────

//        /// <summary>Number of PDF pages accumulated so far.</summary>
//        public int PagesReady { get; set; }

//        /// <summary>Rough estimate of total pages (based on 25 rows/page).</summary>
//        public int EstimatedTotalPages { get; set; }

//        /// <summary>Total input rows (used for progress percentage).</summary>
//        public int TotalRows { get; set; }

//        /// <summary>Total number of chunks (known after first chunk is done).</summary>
//        public int TotalChunks { get; set; }

//        /// <summary>Number of chunks merged so far.</summary>
//        public int CompletedChunks { get; set; }

//        /// <summary>Compressed size of base.pdf after the last merge (bytes).</summary>
//        public long CurrentSizeBytes { get; set; }

//        /// <summary>
//        /// Compressed size of the most recently rendered chunk (bytes).
//        /// Used by CalcSubsequentRows to auto-calibrate chunk row count.
//        /// </summary>
//        public long ChunkBytes { get; set; }

//        /// <summary>UTC timestamp of the last chunk merge.</summary>
//        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;

//        // ── Terminal states ───────────────────────────────────────────────

//        /// <summary>True when all chunks have been merged.</summary>
//        public bool IsComplete { get; set; }

//        /// <summary>True if any background chunk failed.</summary>
//        public bool HasError { get; set; }

//        /// <summary>Error message from the failed background chunk (if any).</summary>
//        public string? ErrorMessage { get; set; }





//        // ── Synchronisation ───────────────────────────────────────────────

//        /// <summary>
//        /// Guards all reads/writes to <see cref="BasePdfPath"/>.
//        /// Acquire before touching base.pdf; release immediately after.
//        /// </summary>
//        public SemaphoreSlim FileLock { get; } = new SemaphoreSlim(1, 1);
//    }
//}



//best of all
using System.Collections.Concurrent;

namespace NexgenCosysReport.Dtos.ReportDtos
{
    /// <summary>
    /// Represents a running or completed progressive PDF build job.
    ///
    /// Lifecycle:
    ///   chunks rendered → compressed → flushed into slabs → slabs accumulated into base.pdf
    ///
    /// All dictionaries are ConcurrentDictionary so reads from GetSnapshotAsync
    /// never need a lock — only FlushLock is needed when writing base.pdf.
    /// </summary>
    public sealed class ProgressivePdfJob : IDisposable
    {
        // ── Identity ────────────────────────────────────────────────────────
        public string JobId { get; } = Guid.NewGuid().ToString("N");

        // ── File system ─────────────────────────────────────────────────────
        public string TempDir { get; init; } = string.Empty;
        public string HeaderChunkPath { get; init; } = string.Empty;
        public string BasePdfPath { get; init; } = string.Empty;

        /// <summary>Rendered + compressed chunk files awaiting slab flush. Key = chunk index.</summary>
        public ConcurrentDictionary<int, string> PendingChunks { get; } = new();

        /// <summary>Merged slab files awaiting base accumulation. Key = slab sequence number.</summary>
        public ConcurrentDictionary<int, string> PendingSlabs { get; } = new();

        // ── Progress ─────────────────────────────────────────────────────────
        public int TotalRows { get; init; }
        public int TotalChunks { get; set; }
        public int EstimatedTotalPages { get; init; }

        private int _completedChunks;
        public int CompletedChunks => Volatile.Read(ref _completedChunks);
        public void IncrementCompletedChunks() => Interlocked.Increment(ref _completedChunks);

        public int PagesReady { get; set; }
        public long CurrentSizeBytes { get; set; }
        public int AccumulationCount { get; set; }
        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;

        // ── Status ───────────────────────────────────────────────────────────
        public bool IsComplete { get; set; }
        public bool HasError { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;

        /// <summary>
        /// Set once by GetFinalAsync after compress+stamp completes.
        /// Null means the final file has not been prepared yet.
        /// Stays valid until cache eviction (60 min absolute).
        /// </summary>
        public string? FinalPdfPath { get; set; }

        // ── Synchronisation ──────────────────────────────────────────────────
        /// <summary>Serialises writes to base.pdf so snapshot reads never see a partial file.</summary>
        public SemaphoreSlim FlushLock { get; } = new(1, 1);

        /// <summary>Per-job cancel — cancelled by the background service on timeout or error.</summary>
        public CancellationTokenSource CancellationTokenSource { get; } = new();

        // ── Disposal ─────────────────────────────────────────────────────────
        private int _disposed;

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) != 0) return;
            FlushLock.Dispose();
            CancellationTokenSource.Dispose();
        }
    }
}