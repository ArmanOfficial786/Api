namespace NexgenCosysReport.Services.ReportService
{
    using Microsoft.Extensions.Hosting;
    using Microsoft.Extensions.Logging;
    using NexgenCosysReport.Inteface.ReportInterface;

    /// <summary>
    /// Hosted background service that periodically deletes orphaned progressive
    /// PDF temp folders left behind by crashed processes or expired cache entries.
    ///
    /// Registration in Program.cs / Startup.cs:
    ///   builder.Services.AddHostedService&lt;ProgressiveTempCleanupService&gt;();
    /// </summary>
    public sealed class ProgressiveTempCleanupService : BackgroundService
    {
        private readonly ILogger<ProgressiveTempCleanupService> _logger;
        private readonly IServiceProvider _services;

        // Run cleanup every 10 minutes; delete folders older than 15 minutes.
        // Jobs are cached for 30 minutes max, so this is safely conservative.
        private static readonly TimeSpan CleanupInterval = TimeSpan.FromMinutes(10);
        private static readonly TimeSpan FolderMaxAge = TimeSpan.FromMinutes(15);

        public ProgressiveTempCleanupService(
            ILogger<ProgressiveTempCleanupService> logger,
            IServiceProvider services)
        {
            _logger = logger;
            _services = services;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation(
                "🗑  ProgressiveTempCleanupService started " +
                "(interval={Interval}, maxAge={Age})",
                CleanupInterval, FolderMaxAge);

            // Delay the first run so startup is not affected.
            await Task.Delay(CleanupInterval, stoppingToken).ConfigureAwait(false);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _services.CreateScope();
                    var pdfService = scope.ServiceProvider
                        .GetRequiredService<IProgressivePdfService>();

                    pdfService.CleanupOrphanedFolders(FolderMaxAge);
                }
                catch (OperationCanceledException) { break; }
                catch (Exception ex)
                {
                    // Log and continue — never crash the background service.
                    _logger.LogError(ex,
                        "❌ ProgressiveTempCleanupService encountered an error");
                }

                await Task.Delay(CleanupInterval, stoppingToken).ConfigureAwait(false);
            }

            _logger.LogInformation("🗑  ProgressiveTempCleanupService stopped");
        }
    }
}