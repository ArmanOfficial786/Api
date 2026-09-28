using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using NexgenCosysReport.DbContext;
using NexgenCosysReport.Dtos.RequestDtos.Remit;
using NexgenCosysReport.Inteface.ServiceInterface.Common;
using NexgenCosysReport.Inteface.ServiceInterface.Remit;
using System.Data;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace NexgenCosysReport.Repository.Remit
{
    public class RemitReconcileReportRepository : IRemitReconcileReportRepository
    {
        private readonly AppDbContext _context;
        private readonly IDateConverterService _dateConverter;
        private readonly ICryptorService _cryptorService;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly ILogger<RemitReconcileReportRepository> _logger;

        public RemitReconcileReportRepository(
            AppDbContext context,
            IDateConverterService dateConverter,
            ICryptorService cryptorService,
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration,
            ILogger<RemitReconcileReportRepository> logger)
        {
            _context = context;
            _dateConverter = dateConverter;
            _cryptorService = cryptorService;
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<RemitReconcileReportData> GetReportDataAsync(RemitReconcileReportRequestDto request)
        {
            try
            {
                var connectionString = _context.Database.GetConnectionString();
                using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();

                var credential = await connection.QueryFirstOrDefaultAsync<dynamic>(
                    "SELECT AccessCode, UserId, Password FROM RemRemittanceList WHERE RemRemittanceGroupId = @Id",
                    new { Id = request.RemittanceGroupId });

                if (credential == null)
                {
                    throw new InvalidOperationException("Record Not Found Currently");
                }

                var remittanceGroupName = await connection.QueryFirstOrDefaultAsync<string>(
                    "SELECT ProviderName FROM RemRemittanceGroup WHERE RemRemittanceGroupId = @Id",
                    new { Id = request.RemittanceGroupId });

                string accessCode = credential.AccessCode ?? string.Empty;
                string userId = credential.UserId ?? string.Empty;
                string password = _cryptorService.Decrypt((string)credential.Password, true);

                var fromDateAd = (await _dateConverter.NepaliToEnglishAsync(request.FromDateBs)).ToString("yyyy-MM-dd");
                var toDateAd = (await _dateConverter.NepaliToEnglishAsync(request.ToDateBs)).ToString("yyyy-MM-dd");
                var fromTime = TimeSpan.Zero.ToString();
                var toTime = DateTime.Now.TimeOfDay.ToString();
                var reportType = string.IsNullOrEmpty(request.ReportType) ? "A" : request.ReportType;
                var sessionId = string.IsNullOrEmpty(request.SessionId) ? Guid.NewGuid().ToString("N") : request.SessionId;

                var signatureSource = accessCode + userId + sessionId + fromDateAd + fromTime + toDateAd + toTime + reportType + password;
                var signature = _cryptorService.GetSha256Hash(signatureSource);

                var middlewareRequest = new RemitReconcileMiddlewareRequestDto
                {
                    accessCode = accessCode,
                    userName = userId,
                    sessionId = sessionId,
                    fromDate = fromDateAd,
                    fromTime = fromTime,
                    toDate = toDateAd,
                    toTime = toTime,
                    reportType = reportType,
                    signature = signature
                };

                var url = _configuration["Remit:ReconcileReportUrl"];
                var smsUser = _configuration["Remit:SmsManagementUserName"];
                var smsPass = _configuration["Remit:SmsManagementPassword"];

                var httpClient = _httpClientFactory.CreateClient();
                var authHeader = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{smsUser}:{smsPass}"));
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", authHeader);

                var jsonOptions = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                var payloadJson = JsonSerializer.Serialize(middlewareRequest, jsonOptions);
                var content = new StringContent(payloadJson, Encoding.UTF8, "application/json");

                var httpResponse = await httpClient.PostAsync(url, content);
                var responseJson = await httpResponse.Content.ReadAsStringAsync();

                RemitReconcileMiddlewareWrapperDto? wrapper = null;
                try
                {
                    wrapper = JsonSerializer.Deserialize<RemitReconcileMiddlewareWrapperDto>(responseJson, jsonOptions);
                }
                catch (JsonException ex)
                {
                    _logger.LogError(ex, "Failed to deserialize middleware reconcile response");
                }

                var rows = wrapper?.Properties?.transactionReports ?? [];

                string branchName = "All";
                var offices = await connection.QueryAsync<string>(
                    "SELECT OfficeName FROM UsmOffice WHERE IsActive = 1");
                var officeList = offices.Where(o => !string.IsNullOrWhiteSpace(o)).ToList();
                if (officeList.Count > 0)
                {
                    branchName = string.Join(", ", officeList);
                }

                var totalPaid = rows.Count(r => string.Equals(r.Status, "PAID", StringComparison.OrdinalIgnoreCase));
                var totalCancelled = rows.Count(r => string.Equals(r.Status, "CANCEL", StringComparison.OrdinalIgnoreCase) || string.Equals(r.Status, "CANCELLED", StringComparison.OrdinalIgnoreCase));

                decimal totalPayoutAmount = 0;
                decimal totalPayoutCommission = 0;
                foreach (var row in rows)
                {
                    if (decimal.TryParse(row.PayoutAmount, out var amt)) totalPayoutAmount += amt;
                    if (decimal.TryParse(row.PayoutCommission, out var com)) totalPayoutCommission += com;
                }

                return new RemitReconcileReportData
                {
                    Rows = rows,
                    TotalRecords = rows.Count,
                    TotalPaid = totalPaid,
                    TotalCancelled = totalCancelled,
                    TotalPayoutAmount = totalPayoutAmount,
                    TotalPayoutCommission = totalPayoutCommission,
                    FromDateBs = request.FromDateBs,
                    ToDateBs = request.ToDateBs,
                    BranchName = branchName,
                    RemittanceGroupName = remittanceGroupName ?? "All",
                    ReportType = reportType
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetReportDataAsync");
                throw;
            }
        }
    }
}