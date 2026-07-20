// Repository/AccountOperation/DetailTrialBalanceRepository.cs
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using NexgenCosysReport.Dtos.RequestDtos.Account;
using NexgenCosysReport.Inteface.ServiceInterface.Account;
using NexgenCosysReport.Inteface.ServiceInterface.Common;
using System.Data;

namespace NexgenCosysReport.Repository.Account
{
    public class DetailTrialBalanceRepository : IDetailTrailBalance
    {
        private readonly AppDbContext _context;
        private readonly IDateConverterService _dateConverter;
        private readonly ILogger<DetailTrialBalanceRepository> _logger;

        public DetailTrialBalanceRepository(
            AppDbContext context,
            IDateConverterService dateConverter,
            ILogger<DetailTrialBalanceRepository> logger)
        {
            _context = context;
            _dateConverter = dateConverter;
            _logger = logger;
        }

        public async Task<DetailTrialBalanceData> GetDetailTrialBalance(DetailTrialBalanceRequest request)
        {
            var connectionString = _context.Database.GetConnectionString();
            using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();

            // Convert Nepali dates to English (MM/dd/yyyy)
            var fromDateAd = await _dateConverter.NepaliToEnglishAsync(request.FromDate);
            var toDateAd = await _dateConverter.NepaliToEnglishAsync(request.ToDate);
            string fromDateStr = fromDateAd.ToString("MM/dd/yyyy");
            string toDateStr = toDateAd.ToString("MM/dd/yyyy");

            // Build branch filter
            string branchFilter = "";
            if (!request.SameCompanyName && !string.IsNullOrEmpty(request.BranchId) && request.BranchId != "-1")
            {
                branchFilter = request.BranchId;
            }

            // Build filter strings
            string sqlFilterExp = $" And v.VoucherOn between '{fromDateStr}' And '{toDateStr}' ";
            if (!string.IsNullOrEmpty(branchFilter))
            {
                sqlFilterExp += $" And v.UsmOfficeId in ({branchFilter})";
            }

            // Order by mapping
            string orderByClause = MapOrderBy(request.OrderBy);
            string sqlFilterExpOrderBy = $" order by {orderByClause}";

            // Prepare output parameters
            var parameters = new DynamicParameters();
            parameters.Add("@SqlFilterExp", sqlFilterExp);
            parameters.Add("@SqlFilterExpOrderBy", sqlFilterExpOrderBy);
            parameters.Add("@OutputAssetExpenses", dbType: DbType.Decimal, direction: ParameterDirection.Output, precision: 18, scale: 2);
            parameters.Add("@OutputLiabilitiesIncome", dbType: DbType.Decimal, direction: ParameterDirection.Output, precision: 18, scale: 2);

            // Execute SP
            var rows = await connection.QueryAsync<DetailTrialBalanceRowDto>(
                "sp_6_56_GetDetailTrailBalance",
                parameters,
                commandType: CommandType.StoredProcedure
            );

            decimal totalAssetExpenses = parameters.Get<decimal?>("@OutputAssetExpenses") ?? 0;
            decimal totalLiabilitiesIncome = parameters.Get<decimal?>("@OutputLiabilitiesIncome") ?? 0;

            return new DetailTrialBalanceData
            {
                Rows = rows.AsList(),
                TotalAssetExpenses = totalAssetExpenses,
                TotalLiabilitiesIncome = totalLiabilitiesIncome
            };
        }

        private string MapOrderBy(string orderBy)
        {
            return orderBy switch
            {
                "Debit Amount" => "DebitAmount DESC",
                "Credit Amount" => "CreditAmount DESC",
                "Balance" => "Balance DESC",
                _ => "SubLedger1"   // default: Sub Ledger
            };
        }
    }
}