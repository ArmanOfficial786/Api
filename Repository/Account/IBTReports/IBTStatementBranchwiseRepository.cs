// Repositories/Implementations/Account/IBTReports/IBTStatementBranchwiseRepository.cs
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using NexgenCosysReport.DbContext;
using NexgenCosysReport.Dtos.RequestDtos.Account.IBTReports;
using NexgenCosysReport.Inteface.ServiceInterface.Account.IBTReports;
using NexgenCosysReport.Inteface.ServiceInterface.Common;
using System.Data;

namespace NexgenCosysReport.Repository.Account.IBTReports
{
    public class IBTStatementBranchwiseRepository : IIBTStatementBranchwiseRepository
    {
        private readonly AppDbContext _context;
        private readonly IDateConverterService _dateConverter;
        private readonly ILogger<IBTStatementBranchwiseRepository> _logger;

        private const string DefaultReportType = "Detail";
        private static readonly HashSet<string> ValidReportTypes = new(StringComparer.OrdinalIgnoreCase)
        {
            "Detail", "SubLedger", "InterestCalculation"
        };

        public IBTStatementBranchwiseRepository(
            AppDbContext context,
            IDateConverterService dateConverter,
            ILogger<IBTStatementBranchwiseRepository> logger)
        {
            _context = context;
            _dateConverter = dateConverter;
            _logger = logger;
        }

        // --------------------------------------------------------------
        // Resolves ReportType with a safe fallback to "Detail" — logs a
        // warning rather than throwing, same pattern as TellerCashVault.
        // --------------------------------------------------------------
        private string ResolveReportType(string? reportType)
        {
            if (!string.IsNullOrWhiteSpace(reportType) && ValidReportTypes.Contains(reportType))
                return reportType;

            _logger.LogWarning(
                "IBTStatementBranchwise: Unrecognized report type '{ReportType}' — falling back to '{Default}'.",
                reportType, DefaultReportType);

            return DefaultReportType;
        }

        public async Task<IBTStatementBranchwiseData> GetReportDataAsync(IBTStatementBranchwiseRequestDto request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.FromDateBs) || string.IsNullOrEmpty(request.ToDateBs)
                    || request.FromDateBs == "-1" || request.ToDateBs == "-1")
                {
                    throw new ArgumentException("FromDateBs and ToDateBs are required.");
                }

                if (string.IsNullOrEmpty(request.PayableBranchId) ||
                    request.PayableBranchId == "-1" ||
                    !long.TryParse(request.PayableBranchId, out var payableBranchId))
                {
                    // Mirrors legacy RequiredFieldValidator on ddlPaybleBranchName
                    throw new ArgumentException("PayableBranchId is required.");
                }

                if (string.IsNullOrEmpty(request.OfficeId) || !long.TryParse(request.OfficeId, out var officeId))
                {
                    throw new ArgumentException("OfficeId is required.");
                }

                var fromDateAd = await _dateConverter.NepaliToEnglishAsync(request.FromDateBs);
                var toDateAd = await _dateConverter.NepaliToEnglishAsync(request.ToDateBs);
                var fromDateStr = fromDateAd.ToString("yyyy-MM-dd");
                var toDateStr = toDateAd.ToString("yyyy-MM-dd");

                var connectionString = _context.Database.GetConnectionString();
                using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();

                var loginBranchName = await connection.QueryFirstOrDefaultAsync<string>(
                    "SELECT OfficeName FROM UsmOffice WHERE UsmOfficeId = @OfficeId",
                    new { OfficeId = officeId }) ?? "";

                var payableBranchName = await connection.QueryFirstOrDefaultAsync<string>(
                    "SELECT OfficeName FROM UsmOffice WHERE UsmOfficeId = @PayableBranchId",
                    new { PayableBranchId = payableBranchId }) ?? "";

                var resolvedReportType = ResolveReportType(request.ReportType);

                var data = new IBTStatementBranchwiseData
                {
                    FromDateBs = request.FromDateBs,
                    ToDateBs = request.ToDateBs,
                    LoginBranchName = loginBranchName,
                    PayableBranchName = payableBranchName,
                    ReportType = resolvedReportType,
                    InterestRate = request.InterestRate,
                    MinimumClosingBalance = request.MinimumClosingBalance
                };

                switch (resolvedReportType)
                {
                    case "SubLedger":
                        {
                            var parameters = new DynamicParameters();
                            parameters.Add("@UsmOfficeId", officeId);
                            parameters.Add("@PayableBranchName", payableBranchName);
                            parameters.Add("@FromDate", fromDateStr);
                            parameters.Add("@Todate", toDateStr);

                            var rows = await connection.QueryAsync<IBTStatementBranchwiseLedgerGroupRowDto>(
                                "sp_5_43_GetIBTStatementBranchwiseLedgerGroup",
                                parameters,
                                commandType: CommandType.StoredProcedure,
                                commandTimeout: 120
                            );
                            data.LedgerGroupRows = rows.AsList();
                            break;
                        }

                    case "InterestCalculation":
                        {
                            var parameters = new DynamicParameters();
                            parameters.Add("@UsmOfficeId", officeId);
                            parameters.Add("@PayableBranchName", payableBranchName);
                            parameters.Add("@FromDate", fromDateStr);
                            parameters.Add("@Todate", toDateStr);
                            parameters.Add("@InterestRate", request.InterestRate);
                            parameters.Add("@MinimumClosingBalance", request.MinimumClosingBalance);

                            var rows = await connection.QueryAsync<IBTStatementBranchwiseInterestRowDto>(
                                "sp_5_43_GetIBTStatementBranchwiseForInterest",
                                parameters,
                                commandType: CommandType.StoredProcedure,
                                commandTimeout: 120
                            );
                            data.InterestRows = rows.AsList();
                            break;
                        }

                    case "Detail":
                    default:
                        {
                            var parameters = new DynamicParameters();
                            parameters.Add("@UsmOfficeId", officeId);
                            parameters.Add("@PayableBranchName", payableBranchName);
                            parameters.Add("@FromDate", fromDateStr);
                            parameters.Add("@Todate", toDateStr);

                            var rows = await connection.QueryAsync<IBTStatementBranchwiseRowDto>(
                                "sp_5_43_GetIBTStatementBranchwise",
                                parameters,
                                commandType: CommandType.StoredProcedure,
                                commandTimeout: 120
                            );
                            data.DetailRows = rows.AsList();
                            break;
                        }
                }

                return data;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetReportDataAsync");
                throw;
            }
        }
    }
}