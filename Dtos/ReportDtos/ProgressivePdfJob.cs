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
    /// Tracks the full lifecycle of one progressive-PDF render job.
    ///
    /// Three-level pipeline state
    /// ──────────────────────────
    ///   PendingChunks  — compressed per-chunk PDFs awaiting level-1 merge.
    ///   PendingSlabs   — level-1 slab PDFs awaiting level-2 accumulation.
    ///   BasePdfPath    — the rolling accumulated base; served for snapshots.
    ///
    /// Thread-safety
    /// ─────────────
    ///   PendingChunks / PendingSlabs: ConcurrentDictionary (lock-free adds
    ///     from render tasks; drains from the single flush consumer).
    ///   FlushLock: SemaphoreSlim(1,1) serialises all writes to base.pdf and
    ///     the two pending collections.  Snapshot readers hold it only for the
    ///     few microseconds needed to copy the path lists.
    ///   Scalar progress fields (PagesReady etc.) written only by the flush
    ///     consumer (single writer) — no lock needed for reads.
    /// </summary>
    public sealed class ProgressivePdfJob : IDisposable
    {
        // ── Identity ─────────────────────────────────────────────────────────
        public string JobId { get; } = Guid.NewGuid().ToString("N");

        // ── Paths ─────────────────────────────────────────────────────────────

        /// <summary>Scratch directory for all temp files for this job.</summary>
        public string TempDir { get; init; } = string.Empty;

        /// <summary>
        /// First rendered chunk (with report header / column headings).
        /// Written synchronously in StartAsync — always valid before StartAsync returns.
        /// Also seeded into BasePdfPath so snapshots are available immediately.
        /// </summary>
        public string HeaderChunkPath { get; init; } = string.Empty;

        /// <summary>
        /// The rolling merged PDF.
        /// Initially a copy of the header chunk.
        /// Updated atomically (write-then-rename) by level-2 accumulations.
        /// Snapshots read: BasePdfPath + PendingSlabs + PendingChunks (tail only).
        /// </summary>
        public string BasePdfPath { get; init; } = string.Empty;

        // ── Progress ─────────────────────────────────────────────────────────

        public int TotalRows { get; init; }
        public int TotalChunks { get; init; }
        public int EstimatedTotalPages { get; init; }

        private int _completedChunks;
        public int CompletedChunks => _completedChunks;
        public void IncrementCompletedChunks() => Interlocked.Increment(ref _completedChunks);

        /// <summary>Pages in base.pdf after the most recent accumulation.</summary>
        public int PagesReady { get; set; }
        public long CurrentSizeBytes { get; set; }
        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;

        /// <summary>How many level-2 accumulations have been completed.</summary>
        public int AccumulationCount { get; set; }

        // ── Completion / error ────────────────────────────────────────────────

        public bool IsComplete { get; set; }
        public bool HasError { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;

        // ── Pipeline state (concurrency-safe) ────────────────────────────────

        /// <summary>
        /// Level-0 → Level-1 queue.
        /// Compressed chunk PDFs awaiting the next slab flush.
        /// Key = global chunk index (1, 2, 3 …).
        /// Written by render tasks; drained by the flush consumer.
        /// </summary>
        public ConcurrentDictionary<int, string> PendingChunks { get; } = new();

        /// <summary>
        /// Level-1 → Level-2 queue.
        /// Slab PDFs (each = BatchFlushSize compressed chunks) awaiting
        /// the next base.pdf accumulation.
        /// Key = monotonic slab sequence number.
        /// Written and drained exclusively by the flush consumer (under FlushLock).
        /// </summary>
        public ConcurrentDictionary<int, string> PendingSlabs { get; } = new();

        /// <summary>
        /// Serialises ALL writes to base.pdf and drains of PendingChunks /
        /// PendingSlabs.  Also held briefly by snapshot readers to copy
        /// the tail path lists.
        ///
        /// Acquiring order: always FlushLock before any file I/O.
        /// </summary>
        public SemaphoreSlim FlushLock { get; } = new(1, 1);

        /// <summary>
        /// Allows external callers (e.g. DELETE /jobs/{id}) to cancel the
        /// background render without shutting down the host.
        /// </summary>
        public CancellationTokenSource CancellationTokenSource { get; } = new();

        // ── IDisposable ───────────────────────────────────────────────────────

        private bool _disposed;

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            FlushLock.Dispose();
            CancellationTokenSource.Cancel();
            CancellationTokenSource.Dispose();
        }
    }
}