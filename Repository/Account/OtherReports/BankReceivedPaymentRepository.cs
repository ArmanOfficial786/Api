// Repositories/Implementations/AccountOperation/BankReceivedPaymentRepository.cs
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using NexgenCosysReport.DbContext;
using NexgenCosysReport.Dtos.RequestDtos.Account.OthersReport;
using NexgenCosysReport.Inteface.ServiceInterface.AccountOperation.OthersReport;
using NexgenCosysReport.Inteface.ServiceInterface.Common;
using System.Data;

namespace NexgenCosysReport.Repository.Account.OthersReport
{
    public class BankReceivedPaymentRepository : IBankReceivedPaymentRepository
    {
        private readonly AppDbContext _context;
        private readonly IDateConverterService _dateConverter;
        private readonly ILogger<BankReceivedPaymentRepository> _logger;

        public BankReceivedPaymentRepository(
            AppDbContext context,
            IDateConverterService dateConverter,
            ILogger<BankReceivedPaymentRepository> logger)
        {
            _context = context;
            _dateConverter = dateConverter;
            _logger = logger;
        }

        // --------------------------------------------------------------
        // @office
        // Appended inside SP as: WHERE ... + @office
        // Uses v.UsmOfficeId (matches the pattern seen in the sibling
        // sp_6_56_GetPaymentThroughSavingReport, which joins UsmOffice
        // aliased "v"). "Same Company Name" checked => office filter
        // dropped entirely (branchId = -1), matching legacy WebForm:
        // chkSameCompanyName.Checked == true -> branchId = -1
        // --------------------------------------------------------------
        private static string BuildOfficeFilter(BankReceivedPaymentRequestDto request)
        {
            if (request.SameCompanyName)
                return string.Empty;

            var branchIds = SanitizeBranchIds(request.BranchIds);
            if (branchIds == "-1")
                return string.Empty;

            return $" And v.UsmOfficeId in ({branchIds})";
        }

        private static string SanitizeBranchIds(string? branchIds)
        {
            if (string.IsNullOrWhiteSpace(branchIds) || branchIds == "-1" || branchIds == "string")
                return "-1";

            var validIds = branchIds
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Where(id => long.TryParse(id, out _));

            var joined = string.Join(",", validIds);
            return string.IsNullOrEmpty(joined) ? "-1" : joined;
        }

        public async Task<BankReceivedPaymentData> GetReportDataAsync(BankReceivedPaymentRequestDto request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.FromDateBs) || string.IsNullOrEmpty(request.ToDateBs)
                    || request.FromDateBs == "-1" || request.ToDateBs == "-1")
                {
                    throw new ArgumentException("FromDateBs and ToDateBs are required.");
                }

                var fromDateAd = await _dateConverter.NepaliToEnglishAsync(request.FromDateBs);
                var toDateAd = await _dateConverter.NepaliToEnglishAsync(request.ToDateBs);

                // ISO format avoids SQL Server regional/language ambiguity for string->date literals
                var fromDateStr = fromDateAd.ToString("yyyy-MM-dd");
                var toDateStr = toDateAd.ToString("yyyy-MM-dd");

                var officeFilter = BuildOfficeFilter(request);

                var connectionString = _context.Database.GetConnectionString();
                using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();

                // ---- SP declares 6 separate parameters (per the legacy DAL call) ----
                var parameters = new DynamicParameters();
                parameters.Add("@fromDate", fromDateStr);
                parameters.Add("@toDate", toDateStr);
                parameters.Add("@paymentType", request.PaymentType);
                parameters.Add("@transactionType", request.TransactionType);
                parameters.Add("@office", officeFilter, DbType.String, size: -1);
                parameters.Add("@orderBy", request.OrderBy);

                var rows = await connection.QueryAsync<BankReceivedPaymentRowDto>(
                    "sp_6_56_GetBankReceivedPaymentReport",
                    parameters,
                    commandType: CommandType.StoredProcedure,
                    commandTimeout: 120
                );

                var resultList = rows.AsList();

                string branchNames = "All";
                if (!request.SameCompanyName)
                {
                    var branchIds = SanitizeBranchIds(request.BranchIds);
                    if (branchIds != "-1")
                    {
                        var names = await connection.QueryAsync<string>(
                            $"SELECT OfficeName FROM UsmOffice WHERE UsmOfficeId IN ({branchIds})");
                        var nameList = names.Where(n => !string.IsNullOrWhiteSpace(n)).ToList();
                        branchNames = nameList.Count > 0 ? string.Join(", ", nameList) : "All";
                    }
                }

                return new BankReceivedPaymentData
                {
                    Rows = resultList,
                    TotalRecords = resultList.Count,
                    TotalAmount = resultList.Sum(r => r.CashReceived ?? 0),
                    FromDateBs = request.FromDateBs,
                    ToDateBs = request.ToDateBs,
                    BranchNames = branchNames,
                    PaymentType = request.PaymentType,
                    TransactionType = request.TransactionType,
                    OrderBy = request.OrderBy
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