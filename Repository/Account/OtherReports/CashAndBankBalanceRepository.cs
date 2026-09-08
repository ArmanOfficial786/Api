// Repositories/Implementations/AccountOperation/CashAndBankBalanceRepository.cs
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
    public class CashAndBankBalanceRepository : ICashAndBankBalanceRepository
    {
        private readonly AppDbContext _context;
        private readonly IDateConverterService _dateConverter;
        private readonly ILogger<CashAndBankBalanceRepository> _logger;

        public CashAndBankBalanceRepository(
            AppDbContext context,
            IDateConverterService dateConverter,
            ILogger<CashAndBankBalanceRepository> logger)
        {
            _context = context;
            _dateConverter = dateConverter;
            _logger = logger;
        }

        // --------------------------------------------------------------
        // Filter set matching the legacy CAccountOperationReports.GetCashAndBankBalance
        // exactly:
        //   SqlFilterExp         -> v.VoucherOn <= tillDate  (+ branch)
        //   SqlFilterExpPrevious -> v.VoucherOn <  tillDate  (+ branch)
        //   SqlFilterExpToday    -> v.VoucherOn =  tillDate  (+ branch)
        //   SqlFilterExpTeller   -> t.TransectionOn = tillDate (+ branch, on t alias)
        //   SqlFilterExpOrderBy  -> Main Ledger / Sub Ledger / Debit / Credit / Balance
        //   SqlFilterExpTellerOrderBy -> always empty (commented out in legacy code)
        // --------------------------------------------------------------
        private record FilterSet(
            string SqlFilterExp,
            string SqlFilterExpPrevious,
            string SqlFilterExpToday,
            string SqlFilterExpTeller,
            string SqlFilterExpOrderBy,
            string SqlFilterExpTellerOrderBy);

        private async Task<FilterSet> BuildFiltersAsync(CashAndBankBalanceRequestDto request)
        {
            var sqlFilterExp = string.Empty;
            var sqlFilterExpPrevious = string.Empty;
            var sqlFilterExpToday = string.Empty;
            var sqlFilterExpTeller = string.Empty;

            if (!string.IsNullOrEmpty(request.TillDateBs) && request.TillDateBs != "-1")
            {
                var tillDateAd = await _dateConverter.NepaliToEnglishAsync(request.TillDateBs);
                var tillDateStr = tillDateAd.ToString("yyyy-MM-dd");

                sqlFilterExp += $" And v.VoucherOn <= '{tillDateStr}'";
                sqlFilterExpPrevious += $" And v.VoucherOn < '{tillDateStr}'";
                sqlFilterExpToday += $" And v.VoucherOn = '{tillDateStr}'";
                sqlFilterExpTeller += $" And t.TransectionOn = '{tillDateStr}'";
            }

            if (!string.IsNullOrEmpty(request.BranchId) &&
                request.BranchId != "-1" &&
                request.BranchId != "string" &&
                long.TryParse(request.BranchId, out var branchId))
            {
                sqlFilterExp += $" And v.UsmOfficeId = {branchId}";
                sqlFilterExpPrevious += $" And v.UsmOfficeId = {branchId}";
                sqlFilterExpToday += $" And v.UsmOfficeId = {branchId}";
                sqlFilterExpTeller += $" And t.UsmOfficeId = {branchId}";
            }

            var sqlFilterExpOrderBy = request.OrderBy?.Trim() switch
            {
                "Main Ledger" => " order by MainLedger",
                "Sub Ledger" => " order by SubLedger",
                "Debit Amount" => " order by DebitAmount DESC",
                "Credit Amount" => " order by CreditAmount DESC",
                "Balance" => " order by Balance DESC",
                _ => string.Empty
            };

            // Legacy code always leaves this empty (line is commented out)
            var sqlFilterExpTellerOrderBy = string.Empty;

            return new FilterSet(
                sqlFilterExp, sqlFilterExpPrevious, sqlFilterExpToday,
                sqlFilterExpTeller, sqlFilterExpOrderBy, sqlFilterExpTellerOrderBy);
        }

        public async Task<CashAndBankBalanceData> GetReportDataAsync(CashAndBankBalanceRequestDto request)
        {
            try
            {
                var filters = await BuildFiltersAsync(request);

                var connectionString = _context.Database.GetConnectionString();
                using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();

                // ---- sp_6_56_GetNepaliCashAndBankBalanceBank (output: @bankBalance) ----
                var bankParams = new DynamicParameters();
                bankParams.Add("@SqlFilterExp", filters.SqlFilterExp);
                bankParams.Add("@SqlFilterExpToday", filters.SqlFilterExpToday);
                bankParams.Add("@SqlFilterExpOrderBy", filters.SqlFilterExpOrderBy);
                bankParams.Add("@SqlFilterExpPrevious", filters.SqlFilterExpPrevious);
                bankParams.Add("@bankBalance", dbType: DbType.Double, direction: ParameterDirection.Output);

                var bankRows = (await connection.QueryAsync<CashAndBankBalanceBankRowDto>(
                    "sp_6_56_GetNepaliCashAndBankBalanceBank",
                    bankParams,
                    commandType: CommandType.StoredProcedure,
                    commandTimeout: 120
                )).AsList();

                var bankBalanceOutput = bankParams.Get<double?>("@bankBalance") ?? 0;

                // ---- sp_6_56_GetCashAndBankBalanceTeller (no output params) ----
                var tellerParams = new DynamicParameters();
                tellerParams.Add("@SqlFilterExpTeller", filters.SqlFilterExpTeller);
                tellerParams.Add("@SqlFilterExpTellerOrderBy", filters.SqlFilterExpTellerOrderBy);

                var tellerRows = (await connection.QueryAsync<CashAndBankBalanceTellerRowDto>(
                    "sp_6_56_GetCashAndBankBalanceTeller",
                    tellerParams,
                    commandType: CommandType.StoredProcedure,
                    commandTimeout: 120
                )).AsList();

                // ---- sp_6_56_GetCashAndBankBalanceCash (output: @cashBalance) ----
                var cashParams = new DynamicParameters();
                cashParams.Add("@SqlFilterExp", filters.SqlFilterExp);
                cashParams.Add("@SqlFilterExpToday", filters.SqlFilterExpToday);
                cashParams.Add("@SqlFilterExpOrderBy", filters.SqlFilterExpOrderBy);
                cashParams.Add("@SqlFilterExpPrevious", filters.SqlFilterExpPrevious);
                cashParams.Add("@cashBalance", dbType: DbType.Double, direction: ParameterDirection.Output);

                var cashRows = (await connection.QueryAsync<CashAndBankBalanceCashRowDto>(
                    "sp_6_56_GetCashAndBankBalanceCash",
                    cashParams,
                    commandType: CommandType.StoredProcedure,
                    commandTimeout: 120
                )).AsList();

                var cashBalanceOutput = cashParams.Get<double?>("@cashBalance") ?? 0;

                // --------------------------------------------------------------
                // GetCashAndBankBalanceDetails logic, ported exactly:
                //   TellerBalance = SUM(dt.CashBalance) over the teller table
                //   xrTableCellBank = TotalBank
                //   xrTableCellCash = TotalCash + TellerBalance
                // --------------------------------------------------------------
                decimal tellerBalance = tellerRows.Sum(r => r.CashBalance ?? 0);

                string branchName = "All";
                if (!string.IsNullOrEmpty(request.BranchId) &&
                    request.BranchId != "-1" &&
                    request.BranchId != "string" &&
                    long.TryParse(request.BranchId, out var branchIdForName))
                {
                    var name = await connection.QueryFirstOrDefaultAsync<string>(
                        "SELECT OfficeName FROM UsmOffice WHERE UsmOfficeId = @BranchId",
                        new { BranchId = branchIdForName });
                    branchName = string.IsNullOrEmpty(name) ? "All" : name;
                }

                return new CashAndBankBalanceData
                {
                    BankRows = bankRows,
                    TellerRows = tellerRows,
                    CashRows = cashRows,
                    BankBalanceOutput = (decimal)bankBalanceOutput,
                    CashBalanceOutput = (decimal)cashBalanceOutput,
                    TellerBalance = tellerBalance,
                    TillDateBs = request.TillDateBs,
                    BranchName = branchName,
                    OrderBy = request.OrderBy,
                    NepaliReport = request.NepaliReport
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