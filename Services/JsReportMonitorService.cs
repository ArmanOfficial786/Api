using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace NexgenCosysReport.Services
{
    public class JsReportMonitorService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<JsReportMonitorService> _logger;
        private readonly TimeSpan _interval = TimeSpan.FromSeconds(30);
        private HealthStatus? _lastStatus;

        public JsReportMonitorService(
            IServiceScopeFactory scopeFactory,
            ILogger<JsReportMonitorService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var healthCheckService = scope.ServiceProvider.GetRequiredService<HealthCheckService>();

                    var report = await healthCheckService.CheckHealthAsync(
                        r => r.Name == "jsreport", stoppingToken);

                    if (_lastStatus != report.Status)
                    {
                        if (report.Status == HealthStatus.Healthy)
                            _logger.LogInformation("jsreport status changed: {Previous} -> Healthy", _lastStatus?.ToString() ?? "Unknown");
                        else
                            _logger.LogWarning("jsreport status changed: {Previous} -> {Current}", _lastStatus?.ToString() ?? "Unknown", report.Status);

                        _lastStatus = report.Status;
                    }
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "jsreport monitor failed");
                }

                await Task.Delay(_interval, stoppingToken);
            }
        }
    }
}
