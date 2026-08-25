// Repository/AccountOperation/CashFlowRepository.cs
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
    public class CashFlowRepository : ICashFlow
    {
        private readonly AppDbContext _context;
        private readonly IDateConverterService _dateConverter;
        private readonly ILogger<CashFlowRepository> _logger;

        public CashFlowRepository(
            AppDbContext context,
            IDateConverterService dateConverter,
            ILogger<CashFlowRepository> logger)
        {
            _context = context;
            _dateConverter = dateConverter;
            _logger = logger;
        }

        public async Task<CashFlowData> GetCashFlow(CashFlowRequest request)
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

            string orderByClause = MapOrderBy(request.OrderBy);
            string sqlFilterExpOrderBy = $" order by {orderByClause}";

            // Inflow
            var inflowParams = new DynamicParameters();
            inflowParams.Add("@SqlFilterExp", sqlFilterExp);
            inflowParams.Add("@SqlFilterExpOrderBy", sqlFilterExpOrderBy);
            inflowParams.Add("@inFlowBalance", dbType: DbType.Decimal, direction: ParameterDirection.Output, precision: 18, scale: 2);

            var inflowRows = await connection.QueryAsync<CashFlowSpDto>(
                "sp_6_56_GetCashInflow",
                inflowParams,
                commandType: CommandType.StoredProcedure
            );
            decimal totalInflow = inflowParams.Get<decimal?>("@inFlowBalance") ?? 0;

            // Outflow
            var outflowParams = new DynamicParameters();
            outflowParams.Add("@SqlFilterExp", sqlFilterExp);
            outflowParams.Add("@SqlFilterExpOrderBy", sqlFilterExpOrderBy);
            outflowParams.Add("@outFlowBalance", dbType: DbType.Decimal, direction: ParameterDirection.Output, precision: 18, scale: 2);

            var outflowRows = await connection.QueryAsync<CashFlowSpDto>(
                "sp_6_56_GetCashOutFlow",
                outflowParams,
                commandType: CommandType.StoredProcedure
            );
            decimal totalOutflow = outflowParams.Get<decimal?>("@outFlowBalance") ?? 0;

            // Opening Cash Balance
            string sqlFilterExpPrevious = "";
            if (!string.IsNullOrEmpty(request.FromDate))
            {
                var fromDateAdForOpening = await _dateConverter.NepaliToEnglishAsync(request.FromDate);
                string prevDateStr = fromDateAdForOpening.ToString("MM/dd/yyyy");
                sqlFilterExpPrevious = $" And v.VoucherOn < '{prevDateStr}' ";
                if (!string.IsNullOrEmpty(branchFilter))
                {
                    sqlFilterExpPrevious += $" And v.UsmOfficeId in ({branchFilter})";
                }
            }

            var openingParams = new DynamicParameters();
            openingParams.Add("@SqlFilterExpPrevious", sqlFilterExpPrevious);
            openingParams.Add("@cashBalance", dbType: DbType.Decimal, direction: ParameterDirection.Output, precision: 18, scale: 2);

            await connection.QueryAsync<dynamic>(
                "sp_6_56_GetCashAndBankBalanceCashOpening",
                openingParams,
                commandType: CommandType.StoredProcedure
            );
            decimal openingCash = openingParams.Get<decimal?>("@cashBalance") ?? 0;

            return new CashFlowData
            {
                InflowRows = inflowRows.AsList(),
                OutflowRows = outflowRows.AsList(),
                TotalInflow = totalInflow,
                TotalOutflow = totalOutflow,
                OpeningCashBalance = openingCash
            };
        }

        private string MapOrderBy(string orderBy)
        {
            return orderBy switch
            {
                "Voucher No" => "VoucherNo",
                "Narration" => "Narration",
                "Amount" => "Amount DESC",
                _ => "VoucherDate"
            };
        }
    }
}
