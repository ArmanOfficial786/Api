// Repositories/Implementations/Account/PaymentThroughSavingRepository.cs
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using NexgenCosysReport.DbContext;
using NexgenCosysReport.Dtos.RequestDtos.Account.OthersReport;
using NexgenCosysReport.Inteface.ServiceInterface.Account.OthersReport;
using NexgenCosysReport.Inteface.ServiceInterface.Common;
using System.Data;

namespace NexgenCosysReport.Repository.Account.OthersReport
{
    public class PaymentThroughSavingRepository : IPaymentThroughSavingRepository
    {
        private readonly AppDbContext _context;
        private readonly IDateConverterService _dateConverter;
        private readonly ILogger<PaymentThroughSavingRepository> _logger;

        public PaymentThroughSavingRepository(
            AppDbContext context,
            IDateConverterService dateConverter,
            ILogger<PaymentThroughSavingRepository> logger)
        {
            _context = context;
            _dateConverter = dateConverter;
            _logger = logger;
        }

        // --------------------------------------------------------------
        // @office
        // Appended inside SP as: WHERE 1=1 + @sqlfilter + @office + @orderBy
        // Uses v.UsmOfficeId (joined from UsmOffice aliased "v").
        // "Same Company Name" checked => office filter dropped entirely
        // (branchId = -1), matching legacy WebForm:
        // chkSameCompanyName.Checked == true -> branchId = -1
        // --------------------------------------------------------------
        private static string BuildOfficeFilter(PaymentThroughSavingRequestDto request)
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

        public async Task<PaymentThroughSavingData> GetReportDataAsync(PaymentThroughSavingRequestDto request)
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

                // The SP does its own CAST(@fromDate AS date) internally, so any
                // unambiguous date-parseable string works — ISO format is safest.
                var fromDateStr = fromDateAd.ToString("yyyy-MM-dd");
                var toDateStr = toDateAd.ToString("yyyy-MM-dd");

                var officeFilter = BuildOfficeFilter(request);

                var connectionString = _context.Database.GetConnectionString();
                using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();

                // ---- SP declares 5 separate parameters ----
                var parameters = new DynamicParameters();
                parameters.Add("@fromDate", fromDateStr);
                parameters.Add("@toDate", toDateStr);
                parameters.Add("@transactionType", request.TransactionType);
                parameters.Add("@office", officeFilter, DbType.String, size: -1);
                parameters.Add("@orderBy", request.OrderBy);

                var rows = await connection.QueryAsync<PaymentThroughSavingRowDto>(
                    "sp_6_56_GetPaymentThroughSavingReport",
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

                return new PaymentThroughSavingData
                {
                    Rows = resultList,
                    TotalRecords = resultList.Count,
                    TotalAmount = resultList.Sum(r => r.CashReceived ?? 0),
                    FromDateBs = request.FromDateBs,
                    ToDateBs = request.ToDateBs,
                    BranchNames = branchNames,
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