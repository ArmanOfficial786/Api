// Repository/AccountOperation/SummaryTrialBalanceRepository.cs
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using NexgenCosysReport.Dtos.RequestDtos.Account;
using NexgenCosysReport.Inteface.ServiceInterface.Account;
using NexgenCosysReport.Inteface.ServiceInterface.Common;
using System.Data;

namespace NexgenCosysReport.Repository.Account
{
    public class SummaryTrialBalanceRepository : ISummaryTrailBalance
    {
        private readonly AppDbContext _context;
        private readonly IDateConverterService _dateConverter;
        private readonly ILogger<SummaryTrialBalanceRepository> _logger;

        public SummaryTrialBalanceRepository(
            AppDbContext context,
            IDateConverterService dateConverter,
            ILogger<SummaryTrialBalanceRepository> logger)
        {
            _context = context;
            _dateConverter = dateConverter;
            _logger = logger;
        }

        public async Task<List<SummaryTrialBalanceRowDto>> GetSummaryTrialBalance(SummaryTrialBalanceRequest request)
        {
            var connectionString = _context.Database.GetConnectionString();
            using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();

            // Convert dates
            var fromDateAd = await _dateConverter.NepaliToEnglishAsync(request.FromDate);
            var toDateAd = await _dateConverter.NepaliToEnglishAsync(request.ToDate);
            string fromDateStr = fromDateAd.ToString("MM/dd/yyyy");
            string toDateStr = toDateAd.ToString("MM/dd/yyyy");

            // Build filter strings exactly as BLL
            string sqlFilterExp = string.Empty;
            string sqlFilterExpClosing = string.Empty;
            string sqlFilterExpOrderBy = string.Empty;

            // The BLL builds filters based on With/WithoutClosingBalance
            if (request.WithClosingBalance)
            {
                sqlFilterExp = $" And v.VoucherOn >= '{fromDateStr}' And v.VoucherOn <= '{toDateStr}' ";
                sqlFilterExpClosing = $" And v.VoucherOn < '{fromDateStr}' ";
                if (!request.SameCompanyName && !string.IsNullOrEmpty(request.BranchIds) && request.BranchIds != "-1")
                {
                    sqlFilterExp += $" And v.UsmOfficeId in ({request.BranchIds})";
                    sqlFilterExpClosing += $" And v.UsmOfficeId in ({request.BranchIds})";
                }
            }
            else
            {
                sqlFilterExpClosing = " AND 1 = 0 ";
                sqlFilterExp = $" And v.VoucherOn >= '{fromDateStr}' And v.VoucherOn <= '{toDateStr}' ";
                if (!request.SameCompanyName && !string.IsNullOrEmpty(request.BranchIds) && request.BranchIds != "-1")
                {
                    sqlFilterExp += $" And v.UsmOfficeId in ({request.BranchIds})";
                }
            }

            // Order by
            string orderByClause = MapOrderBy(request.OrderBy);
            sqlFilterExpOrderBy = $" order by {orderByClause}";

            // Determine SP
            string spName = request.IsSubLedger ? "sp_6_56_GetSummaryTrialBalanceSub" : "sp_6_56_GetSummaryTrialBalance";

            var parameters = new DynamicParameters();
            parameters.Add("@SqlFilterExp", sqlFilterExp);
            parameters.Add("@SqlFilterExpClosing", sqlFilterExpClosing);
            parameters.Add("@SqlFilterExpOrderBy", sqlFilterExpOrderBy);

            // For SubLedger version, we also need output parameters, but we can handle them in the controller if needed.
            // However, the BLL for SubLedger returns output parameters (AssetExpenses, LiabilitiesIncome) in a list.
            // We'll capture those as output parameters and return them.
            if (request.IsSubLedger)
            {
                parameters.Add("@OutputAssetExpenses", dbType: DbType.Decimal, direction: ParameterDirection.Output, precision: 18, scale: 2);
                parameters.Add("@OutputLiabilitiesIncome", dbType: DbType.Decimal, direction: ParameterDirection.Output, precision: 18, scale: 2);
            }

            var rows = await connection.QueryAsync<SummaryTrialBalanceRowDto>(
                spName,
                parameters,
                commandType: CommandType.StoredProcedure
            );

            // For SubLedger, we could attach the outputs to a property, but we'll just return the rows.
            // The controller can decide what to do with them.

            return rows.AsList();
        }

        private string MapOrderBy(string orderBy)
        {
            return orderBy switch
            {
                "Debit Amount" => "DebitAmount DESC",
                "Credit Amount" => "CreditAmount DESC",
                "Balance" => "Balance DESC",
                _ => "SubLedger"   // Ledger Name
            };
        }
    }
}