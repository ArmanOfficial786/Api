//using Dapper;
//using Microsoft.Data.SqlClient;
//using Microsoft.EntityFrameworkCore;
//using NexgenCosysReport.Dtos.RequestDtos.Account;
//using NexgenCosysReport.Inteface.ServiceInterface.Account;
//using NexgenCosysReport.Inteface.ServiceInterface.Common;
//using System.Data;

//namespace NexgenCosysReport.Repository.AccountOperation
//{
//    public class CashFlowDetailsRepository : ICashFlowDetail
//    {
//        private readonly AppDbContext _context;
//        private readonly IDateConverterService _dateConverter;
//        private readonly ILogger<CashFlowDetailsRepository> _logger;

//        public CashFlowDetailsRepository(
//            AppDbContext context,
//            IDateConverterService dateConverter,
//            ILogger<CashFlowDetailsRepository> logger)
//        {
//            _context = context;
//            _dateConverter = dateConverter;
//            _logger = logger;
//        }

//        public async Task<CashFlowDetailsData> GetCashFlowDetails(CashFlowDetailsRequest request)
//        {
//            var connectionString = _context.Database.GetConnectionString();
//            using var connection = new SqlConnection(connectionString);
//            await connection.OpenAsync();

//            // 1. Fiscal year end
//            var fiscalYear = GetFiscalYearFromNepaliDate(request.TillDate);
//            var fiscalYearToOn = await GetFiscalYearToDate(connection, fiscalYear);

//            // 2. Convert dates
//            var tillDateAd = await _dateConverter.NepaliToEnglishAsync(request.TillDate);
//            string tillDateStr = tillDateAd.ToString("MM/dd/yyyy");
//            string preDateStr = fiscalYearToOn.ToString("MM/dd/yyyy");

//            // 3. Branch filter
//            string branchFilter = "";
//            if (!request.SameCompanyName && !string.IsNullOrEmpty(request.BranchIds) && request.BranchIds != "-1")
//            {
//                branchFilter = request.BranchIds;
//            }

//            // 4. Build filter strings (match BLL)
//            string sqlFilterExpCurrent = $" And v.VoucherOn <= '{tillDateStr}'";
//            string sqlFilterExpPrevious = $" And v.VoucherOn <= '{preDateStr}'";
//            string sqlFilterExpBetween = $" And v.VoucherOn <= '{tillDateStr}' And v.VoucherOn > '{preDateStr}'";

//            // 5. Prepare parameters for each SP
//            var opParams = new DynamicParameters();
//            opParams.Add("@SqlFilterExpCurrent", sqlFilterExpCurrent);
//            opParams.Add("@SqlFilterExpPrevious", sqlFilterExpPrevious);
//            opParams.Add("@SqlFilterExpBetween", sqlFilterExpBetween);
//            opParams.Add("@OperatingBalance", dbType: DbType.Decimal, direction: ParameterDirection.Output, precision: 18, scale: 2);

//            var invParams = new DynamicParameters();
//            invParams.Add("@SqlFilterExpCurrent", sqlFilterExpCurrent);
//            invParams.Add("@SqlFilterExpPrevious", sqlFilterExpPrevious);
//            invParams.Add("@InvestingBalance", dbType: DbType.Decimal, direction: ParameterDirection.Output, precision: 18, scale: 2);

//            var finParams = new DynamicParameters();
//            finParams.Add("@SqlFilterExpCurrent", sqlFilterExpCurrent);
//            finParams.Add("@SqlFilterExpPrevious", sqlFilterExpPrevious);
//            finParams.Add("@FinancingBalance", dbType: DbType.Decimal, direction: ParameterDirection.Output, precision: 18, scale: 2);

//            // 6. Execute
//            // Map results to CashFlowRowDto. The SPs return columns: MainLedger, SubLedger, Amount? Actually the BLL returns DataTable with columns from SP.
//            // The SPs might return different column names. We'll use dynamic and map.
//            var opRows = await connection.QueryAsync<dynamic>("sp_6_56_GetCashFlowOperating", opParams, commandType: CommandType.StoredProcedure);
//            var invRows = await connection.QueryAsync<dynamic>("sp_6_56_GetCashFlowInvesting", invParams, commandType: CommandType.StoredProcedure);
//            var finRows = await connection.QueryAsync<dynamic>("sp_6_56_GetCashFlowFinancing", finParams, commandType: CommandType.StoredProcedure);

//            // Map to DTO – assume columns: MainLedger, SubLedger, Amount (or maybe DebitAmount/CreditAmount)
//            var operating = opRows.Select(r => new CashFlowRowDto
//            {
//                MainLedger = r.MainLedger,
//                SubLedger = r.SubLedger,
//                Amount = r.Amount ?? r.DebitAmount ?? 0 // adjust based on actual column
//            }).ToList();

//            var investing = invRows.Select(r => new CashFlowRowDto
//            {
//                MainLedger = r.MainLedger,
//                SubLedger = r.SubLedger,
//                Amount = r.Amount ?? r.DebitAmount ?? 0
//            }).ToList();

//            var financing = finRows.Select(r => new CashFlowRowDto
//            {
//                MainLedger = r.MainLedger,
//                SubLedger = r.SubLedger,
//                Amount = r.Amount ?? r.DebitAmount ?? 0
//            }).ToList();

//            // Get output totals
//            decimal netOp = opParams.Get<decimal?>("@OperatingBalance") ?? 0;
//            decimal netInv = invParams.Get<decimal?>("@InvestingBalance") ?? 0;
//            decimal netFin = finParams.Get<decimal?>("@FinancingBalance") ?? 0;

//            // 7. Opening cash balance
//            var cashParams = new DynamicParameters();
//            cashParams.Add("@SqlFilterExpPrevious", sqlFilterExpPrevious);
//            cashParams.Add("@CashBalance", dbType: DbType.Decimal, direction: ParameterDirection.Output, precision: 18, scale: 2);
//            await connection.QueryAsync<dynamic>("sp_6_56_GetCashAndBankBalance", cashParams, commandType: CommandType.StoredProcedure);
//            decimal openingCash = cashParams.Get<decimal?>("@CashBalance") ?? 0;

//            return new CashFlowDetailsData
//            {
//                OperatingRows = operating,
//                InvestingRows = investing,
//                FinancingRows = financing,
//                NetOperating = netOp,
//                NetInvesting = netInv,
//                NetFinancing = netFin,
//                OpeningCashBalance = openingCash,
//                ClosingCashBalance = openingCash + netOp + netInv + netFin
//            };
//        }

//        private string GetFiscalYearFromNepaliDate(string nepaliDate)
//        {
//            if (string.IsNullOrEmpty(nepaliDate)) return string.Empty;
//            var parts = nepaliDate.Split('/');
//            if (parts.Length < 2) return string.Empty;
//            int year = int.Parse(parts[0]);
//            int month = int.Parse(parts[1]);
//            if (month > 3)
//                return $"{year - 1}/{year.ToString().Substring(2, 2)}";
//            else
//                return $"{year - 2}/{year - 1}";
//        }

//        private async Task<DateTime> GetFiscalYearToDate(SqlConnection connection, string fiscalYear)
//        {
//            var sql = "SELECT FiscalYearToOn FROM AcoFiscalYear WHERE FiscalYear = @FiscalYear";
//            var result = await connection.QueryFirstOrDefaultAsync<DateTime?>(sql, new { FiscalYear = fiscalYear });
//            if (result.HasValue)
//                return result.Value;
//            throw new Exception($"Fiscal year {fiscalYear} not found.");
//        }
//    }
//}








// File: NexgenCosysReport.Repository/AccountOperation/CashFlowDetailsRepository.cs
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using NexgenCosysReport.Dtos.RequestDtos.Account;
using NexgenCosysReport.Inteface.ServiceInterface.Account;
using NexgenCosysReport.Inteface.ServiceInterface.Common;
using System.Data;

namespace NexgenCosysReport.Repository.AccountOperation
{
    public class CashFlowDetailsRepository : ICashFlowDetail
    {
        private readonly AppDbContext _context;
        private readonly IDateConverterService _dateConverter;
        private readonly ILogger<CashFlowDetailsRepository> _logger;

        public CashFlowDetailsRepository(
            AppDbContext context,
            IDateConverterService dateConverter,
            ILogger<CashFlowDetailsRepository> logger)
        {
            _context = context;
            _dateConverter = dateConverter;
            _logger = logger;
        }

        public async Task<CashFlowDetailsData> GetCashFlowDetails(CashFlowDetailsRequest request)
        {
            var connectionString = _context.Database.GetConnectionString();
            using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();

            // 0. Normalize incoming Nepali date so both "2081/12/30" and "2081-12-30" work
            var normalizedTillDate = NormalizeNepaliDate(request.TillDate);

            // 1. Fiscal year end
            var fiscalYear = GetFiscalYearFromNepaliDate(normalizedTillDate);
            var fiscalYearToOn = await GetFiscalYearToDate(connection, fiscalYear);

            // 2. Convert dates
            var tillDateAd = await _dateConverter.NepaliToEnglishAsync(normalizedTillDate);
            string tillDateStr = tillDateAd.ToString("MM/dd/yyyy");
            string preDateStr = fiscalYearToOn.ToString("MM/dd/yyyy");

            // 3. Branch filter
            string branchFilter = "";
            if (!request.SameCompanyName && !string.IsNullOrEmpty(request.BranchIds) && request.BranchIds != "-1")
            {
                branchFilter = request.BranchIds;
            }

            // 4. Build filter strings (match BLL)
            string sqlFilterExpCurrent = $" And v.VoucherOn <= '{tillDateStr}'";
            string sqlFilterExpPrevious = $" And v.VoucherOn <= '{preDateStr}'";
            string sqlFilterExpBetween = $" And v.VoucherOn <= '{tillDateStr}' And v.VoucherOn > '{preDateStr}'";

            // 5. Prepare parameters for each SP
            var opParams = new DynamicParameters();
            opParams.Add("@SqlFilterExpCurrent", sqlFilterExpCurrent);
            opParams.Add("@SqlFilterExpPrevious", sqlFilterExpPrevious);
            opParams.Add("@SqlFilterExpBetween", sqlFilterExpBetween);
            opParams.Add("@OperatingBalance", dbType: DbType.Decimal, direction: ParameterDirection.Output, precision: 18, scale: 2);

            var invParams = new DynamicParameters();
            invParams.Add("@SqlFilterExpCurrent", sqlFilterExpCurrent);
            invParams.Add("@SqlFilterExpPrevious", sqlFilterExpPrevious);
            invParams.Add("@InvestingBalance", dbType: DbType.Decimal, direction: ParameterDirection.Output, precision: 18, scale: 2);

            var finParams = new DynamicParameters();
            finParams.Add("@SqlFilterExpCurrent", sqlFilterExpCurrent);
            finParams.Add("@SqlFilterExpPrevious", sqlFilterExpPrevious);
            finParams.Add("@FinancingBalance", dbType: DbType.Decimal, direction: ParameterDirection.Output, precision: 18, scale: 2);

            // 6. Execute
            var opRows = await connection.QueryAsync<dynamic>("sp_6_56_GetCashFlowOperating", opParams, commandType: CommandType.StoredProcedure);
            var invRows = await connection.QueryAsync<dynamic>("sp_6_56_GetCashFlowInvesting", invParams, commandType: CommandType.StoredProcedure);
            var finRows = await connection.QueryAsync<dynamic>("sp_6_56_GetCashFlowFinancing", finParams, commandType: CommandType.StoredProcedure);

            var operating = opRows.Select(r => new CashFlowRowDto
            {
                MainLedger = r.MainLedger,
                SubLedger = r.SubLedger,
                Amount = r.Amount ?? r.DebitAmount ?? 0 // adjust based on actual column
            }).ToList();

            var investing = invRows.Select(r => new CashFlowRowDto
            {
                MainLedger = r.MainLedger,
                SubLedger = r.SubLedger,
                Amount = r.Amount ?? r.DebitAmount ?? 0
            }).ToList();

            var financing = finRows.Select(r => new CashFlowRowDto
            {
                MainLedger = r.MainLedger,
                SubLedger = r.SubLedger,
                Amount = r.Amount ?? r.DebitAmount ?? 0
            }).ToList();

            // Get output totals
            decimal netOp = opParams.Get<decimal?>("@OperatingBalance") ?? 0;
            decimal netInv = invParams.Get<decimal?>("@InvestingBalance") ?? 0;
            decimal netFin = finParams.Get<decimal?>("@FinancingBalance") ?? 0;

            // 7. Opening cash balance
            var cashParams = new DynamicParameters();
            cashParams.Add("@SqlFilterExpPrevious", sqlFilterExpPrevious);
            cashParams.Add("@CashBalance", dbType: DbType.Decimal, direction: ParameterDirection.Output, precision: 18, scale: 2);
            await connection.QueryAsync<dynamic>("sp_6_56_GetCashAndBankBalance", cashParams, commandType: CommandType.StoredProcedure);
            decimal openingCash = cashParams.Get<decimal?>("@CashBalance") ?? 0;

            return new CashFlowDetailsData
            {
                OperatingRows = operating,
                InvestingRows = investing,
                FinancingRows = financing,
                NetOperating = netOp,
                NetInvesting = netInv,
                NetFinancing = netFin,
                OpeningCashBalance = openingCash,
                ClosingCashBalance = openingCash + netOp + netInv + netFin
            };
        }

        /// <summary>
        /// Accepts a Nepali (BS) date in either "yyyy/MM/dd" or "yyyy-MM-dd" form
        /// and returns it normalized to "yyyy/MM/dd", which is what the rest of the
        /// pipeline (fiscal-year lookup, IDateConverterService) expects.
        /// </summary>
        private static string NormalizeNepaliDate(string nepaliDate)
        {
            if (string.IsNullOrWhiteSpace(nepaliDate))
                throw new ArgumentException("TillDate is required.", nameof(nepaliDate));

            var (year, month, day) = ParseNepaliDateParts(nepaliDate);
            return $"{year:D4}/{month:D2}/{day:D2}";
        }

        /// <summary>
        /// Splits a Nepali date string on '/' or '-' and validates it has exactly
        /// year/month/day components.
        /// </summary>
        private static (int Year, int Month, int Day) ParseNepaliDateParts(string nepaliDate)
        {
            var parts = nepaliDate.Split(new[] { '/', '-' }, StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length != 3
                || !int.TryParse(parts[0], out var year)
                || !int.TryParse(parts[1], out var month)
                || !int.TryParse(parts[2], out var day))
            {
                throw new FormatException(
                    $"Invalid Nepali date '{nepaliDate}'. Expected format 'yyyy/MM/dd' or 'yyyy-MM-dd'.");
            }

            if (month < 1 || month > 12)
                throw new FormatException($"Invalid Nepali date '{nepaliDate}': month must be between 1 and 12.");

            return (year, month, day);
        }

        /// <summary>
        /// Maps a BS date to its fiscal year key as stored in AcoFiscalYear (e.g. "2082/83").
        /// A fiscal year "YYYY/YY" spans BS YYYY/04/01 through (YYYY+1)/03/32.
        /// - Month 4-12 (Shrawan..Chaitra) -> belongs to fiscal year starting in the same BS year.
        /// - Month 1-3  (Baisakh..Ashadh)  -> belongs to fiscal year that started the previous BS year.
        /// </summary>
        private string GetFiscalYearFromNepaliDate(string normalizedNepaliDate)
        {
            var (year, month, _) = ParseNepaliDateParts(normalizedNepaliDate);

            if (month > 3)
                return $"{year}/{(year + 1).ToString().Substring(2, 2)}";
            else
                return $"{year - 1}/{year.ToString().Substring(2, 2)}";
        }

        private async Task<DateTime> GetFiscalYearToDate(SqlConnection connection, string fiscalYear)
        {
            var sql = "SELECT FiscalYearToOn FROM AcoFiscalYear WHERE FiscalYear = @FiscalYear";
            var result = await connection.QueryFirstOrDefaultAsync<DateTime?>(sql, new { FiscalYear = fiscalYear });
            if (result.HasValue)
                return result.Value;
            throw new Exception($"Fiscal year {fiscalYear} not found.");
        }
    }
}