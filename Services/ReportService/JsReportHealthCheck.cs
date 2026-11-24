//using Microsoft.Extensions.Diagnostics.HealthChecks;

//namespace NexgenCosysReport.Services.ReportService
//{
//    public class JsReportHealthCheck : IHealthCheck
//    {
//        public async Task<HealthCheckResult> CheckHealthAsync(
//            HealthCheckContext context,
//            CancellationToken cancellationToken = default)
//        {
//            try
//            {
//                using var client = new HttpClient
//                {
//                    Timeout = TimeSpan.FromSeconds(5)
//                };

//                using var response = await client.GetAsync(
//                    "http://127.0.0.1:5488/api/ping",
//                    cancellationToken);

//                return response.IsSuccessStatusCode
//                    ? HealthCheckResult.Healthy("jsreport is running.")
//                    : HealthCheckResult.Unhealthy(
//                        $"jsreport returned HTTP {(int)response.StatusCode}.");
//            }
//            catch (Exception ex)
//            {
//                return HealthCheckResult.Unhealthy(
//                    $"jsreport is unavailable: {ex.Message}", ex);
//            }
//        }
//    }
//}
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace NexgenCosysReport.Services.ReportService
{
    public class JsReportHealthCheck : IHealthCheck
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<JsReportHealthCheck> _logger;
        private readonly string _baseUrl;

        public JsReportHealthCheck(
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration,
            ILogger<JsReportHealthCheck> logger)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
            _baseUrl = (configuration["jsreport:url"] ?? "http://127.0.0.1:5488").TrimEnd('/');
        }

        public async Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var client = _httpClientFactory.CreateClient();
                client.Timeout = TimeSpan.FromSeconds(5);

                using var response = await client.GetAsync($"{_baseUrl}/api/ping", cancellationToken);

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("jsreport is RUNNING at {Url}", _baseUrl);
                    return HealthCheckResult.Healthy("jsreport is running.");
                }

                _logger.LogError("jsreport is NOT healthy at {Url}. HTTP {StatusCode}", _baseUrl, (int)response.StatusCode);
                return HealthCheckResult.Unhealthy($"jsreport returned HTTP {(int)response.StatusCode}.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "jsreport is NOT running or unreachable at {Url}", _baseUrl);
                return HealthCheckResult.Unhealthy($"jsreport is unavailable: {ex.Message}", ex);
            }
        }
    }
}