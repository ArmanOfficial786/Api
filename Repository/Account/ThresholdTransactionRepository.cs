// Repository/AccountOperation/ThresholdTransactionRepository.cs
using Dapper;
using NexgenCosysReport.DbContext;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using NexgenCosysReport.Dtos.RequestDtos.Account;
using NexgenCosysReport.Inteface.ServiceInterface.Account;
using NexgenCosysReport.Inteface.ServiceInterface.Common;
using System.Data;

namespace NexgenCosysReport.Repository.AccountOperation
{
    public class ThresholdTransactionRepository : IThresholdTransaction
    {
        private readonly AppDbContext _context;
        private readonly IDateConverterService _dateConverter;
        private readonly ILogger<ThresholdTransactionRepository> _logger;

        public ThresholdTransactionRepository(
            AppDbContext context,
            IDateConverterService dateConverter,
            ILogger<ThresholdTransactionRepository> logger)
        {
            _context = context;
            _dateConverter = dateConverter;
            _logger = logger;
        }

        public async Task<ThresholdTransactionData> GetThresholdTransaction(ThresholdTransactionRequest request)
        {
            var connectionString = _context.Database.GetConnectionString();
            using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();

            // Convert Nepali dates to English (MM/dd/yyyy) - SP expects this format
            string fromDateStr = "";
            string toDateStr = "";

            if (!string.IsNullOrEmpty(request.FromDate) && request.FromDate != "-1")
            {
                var fromDateAd = await _dateConverter.NepaliToEnglishAsync(request.FromDate);
                fromDateStr = fromDateAd.ToString("MM/dd/yyyy");
            }

            if (!string.IsNullOrEmpty(request.ToDate) && request.ToDate != "-1")
            {
                var toDateAd = await _dateConverter.NepaliToEnglishAsync(request.ToDate);
                toDateStr = toDateAd.ToString("MM/dd/yyyy");
            }

            // Build branch filter
            string branchFilter = "-1";
            if (!request.SameCompanyName && !string.IsNullOrEmpty(request.BranchId) && request.BranchId != "-1")
            {
                branchFilter = request.BranchId;
            }

            // Map order by to SP's expected value (use the value directly)
            string orderByClause = request.OrderBy;

            var parameters = new DynamicParameters();
            parameters.Add("@FromDate", fromDateStr);
            parameters.Add("@ToDate", toDateStr);
            parameters.Add("@TransactionNumber", string.IsNullOrEmpty(request.TransactionNumber) ? "" : request.TransactionNumber);
            parameters.Add("@MemberName", string.IsNullOrEmpty(request.MemberName) ? "" : request.MemberName);
            parameters.Add("@UsmOfficeId", branchFilter);
            parameters.Add("@orderBy", orderByClause);

            var rows = await connection.QueryAsync<ThresholdTransactionRowDto>(
                "sp_6_56_GetThresholdTransactionReport",
                parameters,
                commandType: CommandType.StoredProcedure
            );

            var list = rows.AsList();

            return new ThresholdTransactionData
            {
                Rows = list,
                TotalDepositAmount = list.Sum(r => r.AmountInvolvedInDeposit),
                TotalWithdrawAmount = list.Sum(r => r.AmountInvolvedInWithdraw),
                TotalRecords = list.Count
            };
        }
    }
}
