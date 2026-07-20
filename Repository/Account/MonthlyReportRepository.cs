//// Repository/AccountOperation/MonthlyReportRepository.cs
//using Dapper;
//using Microsoft.Data.SqlClient;
//using Microsoft.EntityFrameworkCore;
//using NexgenCosysReport.Dtos.ReportDtos;
//using NexgenCosysReport.Dtos.RequestDtos.Account;
//using NexgenCosysReport.Inteface.ServiceInterface.Account;
//using NexgenCosysReport.Inteface.ServiceInterface.Common;
//using System.Data;

//namespace NexgenCosysReport.Repository.Account
//{
//    public class MonthlyReportRepository : IMonthlyReport
//    {
//        private readonly AppDbContext _context;
//        private readonly IDateConverterService _dateConverter;
//        private readonly ILogger<MonthlyReportRepository> _logger;

//        public MonthlyReportRepository(
//            AppDbContext context,
//            IDateConverterService dateConverter,
//            ILogger<MonthlyReportRepository> logger)
//        {
//            _context = context;
//            _dateConverter = dateConverter;
//            _logger = logger;
//        }

//        public async Task<MonthlyReportData> GetMonthlyReport(MonthlyReportRequest request)
//        {
//            var connectionString = _context.Database.GetConnectionString();
//            using var connection = new SqlConnection(connectionString);
//            await connection.OpenAsync();

//            // 1. Convert dates
//            var tillDateAd = await _dateConverter.NepaliToEnglishAsync(request.TillDate);
//            string tillDateEng = tillDateAd.ToString("MM/dd/yyyy");
//            var currentMonthStart = new DateTime(tillDateAd.Year, tillDateAd.Month, 1);
//            string fromDateEng = currentMonthStart.ToString("MM/dd/yyyy");

//            var previousMonthStart = currentMonthStart.AddMonths(-1);
//            string prevFromDateEng = previousMonthStart.ToString("MM/dd/yyyy");
//            string prevTillDateEng = currentMonthStart.AddDays(-1).ToString("MM/dd/yyyy");

//            // 2. Fiscal year ID
//            var fiscalYear = await GetFiscalYearByDate(connection, tillDateAd);
//            int fiscalYearId = fiscalYear?.AcoFiscalYearId ?? -1;

//            // 3. Branch filter
//            string branchFilter = "";
//            if (!request.SameCompanyName && !string.IsNullOrEmpty(request.BranchIds) && request.BranchIds != "-1")
//            {
//                branchFilter = request.BranchIds;
//            }
//            if (string.IsNullOrEmpty(branchFilter))
//                branchFilter = "-1";

//            // 4. Account types to fetch
//            var accountTypes = request.AccountTypeId == -1
//                ? new List<int> { 1, 2, 3, 4 }
//                : new List<int> { request.AccountTypeId };

//            // 5. SP name
//            string spName = request.IsNepali
//                ? (request.IsMonthWise ? "sp_6_56_GetMonthlyReportNepali" : "sp_6_56_GetMonthlyReportNepali")
//                : (request.IsMonthWise ? "sp_6_56_GetMonthlyReport" : "sp_6_56_GetMonthlyReport");
//            bool isMonthly = request.IsMonthWise;

//            // 6. Fetch data for each account type
//            var allRows = new List<MonthlyReportRowDto>();
//            foreach (int accType in accountTypes)
//            {
//                var parameters = new DynamicParameters();
//                parameters.Add("@SqlBranchId", branchFilter);
//                parameters.Add("@SqlAccountTypeId", accType);
//                parameters.Add("@SqlPrevFromDate", prevFromDateEng);
//                parameters.Add("@SqlPrevTillDate", prevTillDateEng);
//                parameters.Add("@SqlFromDate", fromDateEng);
//                parameters.Add("@SqlTillDate", tillDateEng);
//                parameters.Add("@SqlIsMonthly", isMonthly);
//                parameters.Add("@SqlFiscalYearId", fiscalYearId);

//                var rows = await connection.QueryAsync<MonthlyReportRowDto>(
//                    spName,
//                    parameters,
//                    commandType: CommandType.StoredProcedure
//                );
//                allRows.AddRange(rows);
//            }

//            // 7. Group by LedgerHead
//            var result = new MonthlyReportData
//            {
//                AssetsRows = allRows.Where(r => r.LedgerHead?.Equals("ASSETS", StringComparison.OrdinalIgnoreCase) == true).ToList(),
//                LiabilitiesRows = allRows.Where(r => r.LedgerHead?.Equals("LIABILITIES", StringComparison.OrdinalIgnoreCase) == true).ToList(),
//                IncomeRows = allRows.Where(r => r.LedgerHead?.Equals("INCOME", StringComparison.OrdinalIgnoreCase) == true).ToList(),
//                ExpensesRows = allRows.Where(r => r.LedgerHead?.Equals("EXPENSES", StringComparison.OrdinalIgnoreCase) == true).ToList()
//            };

//            result.TotalAssets = result.AssetsRows.Sum(r => r.CurrentAmount);
//            result.TotalLiabilities = result.LiabilitiesRows.Sum(r => r.CurrentAmount);
//            result.TotalIncome = result.IncomeRows.Sum(r => r.CurrentAmount);
//            result.TotalExpenses = result.ExpensesRows.Sum(r => r.CurrentAmount);

//            return result;
//        }

//        private async Task<dynamic> GetFiscalYearByDate(SqlConnection connection, DateTime date)
//        {
//            var sql = "SELECT AcoFiscalYearId, FiscalYearToOn FROM AcoFiscalYear WHERE FiscalYearFromOn <= @date AND FiscalYearToOn >= @date";
//            var result = await connection.QueryFirstOrDefaultAsync(sql, new { date });
//            return result;
//        }
//    }
//}






// Repository/AccountOperation/MonthlyReportRepository.cs
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using NexgenCosysReport.Dtos.ReportDtos;
using NexgenCosysReport.Dtos.RequestDtos.Account;
using NexgenCosysReport.Inteface.ServiceInterface.Account;
using NexgenCosysReport.Inteface.ServiceInterface.Common;
using System.Data;

namespace NexgenCosysReport.Repository.Account
{
    public class MonthlyReportRepository : IMonthlyReport
    {
        private readonly AppDbContext _context;
        private readonly IDateConverterService _dateConverter;
        private readonly ILogger<MonthlyReportRepository> _logger;

        public MonthlyReportRepository(
            AppDbContext context,
            IDateConverterService dateConverter,
            ILogger<MonthlyReportRepository> logger)
        {
            _context = context;
            _dateConverter = dateConverter;
            _logger = logger;
        }

        private const string IsoDateFormat = "yyyy-MM-dd";

        private static string NormalizeBsDate(string bsDate)
        {
            if (string.IsNullOrWhiteSpace(bsDate)) return bsDate;
            return bsDate.Replace('-', '/');
        }

        public async Task<MonthlyReportData> GetMonthlyReport(MonthlyReportRequest request)
        {
            var connectionString = _context.Database.GetConnectionString();
            using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();

            // 1. Convert dates — SP's own usage comments show 'yyyy-MM-dd'
            // (ISO). "MM/dd/yyyy" is locale-ambiguous for the SP's implicit
            // string->date casts and dynamic SQL string concatenation.
            var normalizedTillDate = NormalizeBsDate(request.TillDate);
            var tillDateAd = (await _dateConverter.NepaliToEnglishAsync(normalizedTillDate)).Date;
            string tillDateEng = tillDateAd.ToString(IsoDateFormat);

            var currentMonthStart = new DateTime(tillDateAd.Year, tillDateAd.Month, 1);
            string fromDateEng = currentMonthStart.ToString(IsoDateFormat);

            var previousMonthStart = currentMonthStart.AddMonths(-1);
            string prevFromDateEngDefault = previousMonthStart.ToString(IsoDateFormat);
            string prevTillDateEng = currentMonthStart.AddDays(-1).ToString(IsoDateFormat);

            // 2. Fiscal year ID
            var fiscalYear = await GetFiscalYearByDate(connection, tillDateAd);
            int fiscalYearId = fiscalYear?.AcoFiscalYearId ?? -1;

            // 3. Branch filter.
            //
            // sp_6_56_GetMonthlyReport(Nepali) has TWO different, inconsistent
            // treatments of @SqlBranchId:
            //   - Main balance query: gated -- `IF(@SqlBranchId <> '')`, so
            //     an empty string correctly means "no filter, all branches".
            //   - Budget subquery (used only when @SqlAccountTypeId is 3 or
            //     4): UNGATED -- always emits
            //     `and BE.UsmOfficeId in ('+@SqlBranchId+')` regardless of
            //     content. An empty string here produces the invalid
            //     "in ()" and throws "Incorrect syntax near ')'." This is a
            //     latent bug in the SP itself.
            //
            // WebForms never exposed this because chkSelectAll always built
            // branchSelected from every individual checked office ID --
            // "all branches" was never represented as an empty string or a
            // literal "-1" sent to the SP. We reproduce that behavior here:
            // when the caller means "all branches" ("-1" or blank), resolve
            // it to the actual comma-separated list of active office IDs
            // instead of passing through an empty string.
            string branchFilter;
            if (!string.IsNullOrEmpty(request.BranchId) && request.BranchId != "-1")
            {
                branchFilter = request.BranchId;
            }
            else
            {
                branchFilter = await GetAllActiveOfficeIdsCsv(connection);
            }

            // 4. Account types to fetch
            var accountTypes = request.AccountTypeId == -1
                ? new List<int> { 1, 2, 3, 4 }
                : new List<int> { request.AccountTypeId };

            string spName = request.IsNepali
                ? "sp_6_56_GetMonthlyReportNepali"
                : "sp_6_56_GetMonthlyReport";
            bool isMonthly = request.IsMonthWise;

            _logger.LogInformation(
                "MonthlyReport query: TillDate(AD)={TillDate}, FromDate(AD)={FromDate}, BranchId(raw)={RawBranchId}, BranchFilter(sent)={BranchFilter}, AccountTypes={AccountTypes}, IsMonthly={IsMonthly}, IsNepali={IsNepali}, FiscalYearId={FiscalYearId}",
                tillDateEng, fromDateEng, request.BranchId, branchFilter, string.Join(",", accountTypes), isMonthly, request.IsNepali, fiscalYearId);

            var assetsRows = new List<MonthlyReportRowDto>();
            var liabilitiesRows = new List<MonthlyReportRowDto>();
            var incomeRows = new List<MonthlyReportRowDto>();
            var expensesRows = new List<MonthlyReportRowDto>();

            foreach (int accType in accountTypes)
            {
                // Legacy WebForms special case: if the previous BS month is
                // Chaitra (month 3, fiscal year-end) and this is
                // Income/Expense (3/4), there's no meaningful "previous
                // period" inside the same fiscal year, so the prev-period
                // filter is disabled entirely via the "-1" sentinel (the SP
                // sets `@PrevDateSetting = ' and 1=2 '` in that case).
                string prevFromDateEng = prevFromDateEngDefault;
                var prevMonthBs = await _dateConverter.EnglishToNepaliAsync(previousMonthStart);
                int prevBsMonth = int.Parse(prevMonthBs.Split('/')[1]);
                if (prevBsMonth == 3 && (accType == 3 || accType == 4))
                {
                    prevFromDateEng = "-1";
                }

                var parameters = new DynamicParameters();
                parameters.Add("@SqlBranchId", branchFilter);
                parameters.Add("@SqlAccountTypeId", accType);
                parameters.Add("@SqlPrevFromDate", prevFromDateEng);
                parameters.Add("@SqlPrevTillDate", prevTillDateEng);
                parameters.Add("@SqlFromDate", fromDateEng);
                parameters.Add("@SqlTillDate", tillDateEng);
                parameters.Add("@SqlIsMonthly", isMonthly);
                parameters.Add("@SqlFiscalYearId", fiscalYearId);

                List<dynamic> rawRows;
                try
                {
                    rawRows = (await connection.QueryAsync(
                        spName,
                        parameters,
                        commandType: CommandType.StoredProcedure)).ToList();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "MonthlyReport: SP call failed for accountType={AccountType}, branchFilter={BranchFilter}",
                        accType, branchFilter);
                    throw;
                }

                if (rawRows.Count > 0)
                {
                    var cols = string.Join(", ", ((IDictionary<string, object>)rawRows[0]).Keys);
                    _logger.LogInformation(
                        "MonthlyReport: accountType={AccountType} returned {RowCount} rows. Columns: {Columns}",
                        accType, rawRows.Count, cols);
                }
                else
                {
                    _logger.LogWarning(
                        "MonthlyReport: accountType={AccountType} returned 0 rows.", accType);
                }

                foreach (var raw in rawRows)
                {
                    var dict = (IDictionary<string, object>)raw;
                    var dto = MapRow(dict, isMonthly);

                    switch (accType)
                    {
                        case 1: assetsRows.Add(dto); break;
                        case 2: liabilitiesRows.Add(dto); break;
                        case 3: incomeRows.Add(dto); break;
                        case 4: expensesRows.Add(dto); break;
                    }
                }
            }

            var result = new MonthlyReportData
            {
                AssetsRows = assetsRows,
                LiabilitiesRows = liabilitiesRows,
                IncomeRows = incomeRows,
                ExpensesRows = expensesRows
            };

            result.TotalAssets = result.AssetsRows.Sum(r => r.CurrentAmount);
            result.TotalLiabilities = result.LiabilitiesRows.Sum(r => r.CurrentAmount);
            result.TotalIncome = result.IncomeRows.Sum(r => r.CurrentAmount);
            result.TotalExpenses = result.ExpensesRows.Sum(r => r.CurrentAmount);

            return result;
        }

        /// <summary>
        /// Resolves "all branches" to the actual comma-separated list of
        /// active office IDs, matching what WebForms' chkSelectAll always
        /// produced. Adjust the table/column/IsActive filter below if your
        /// UsmOffice schema differs.
        /// </summary>
        private static async Task<string> GetAllActiveOfficeIdsCsv(SqlConnection connection)
        {
            var ids = (await connection.QueryAsync<long>(
                "SELECT UsmOfficeId FROM UsmOffice WHERE IsActive = 1")).ToList();

            return string.Join(",", ids);
        }

        /// <summary>
        /// Non-monthly (#tempLedgerFinalBalance2) columns: LedgerHead,
        /// MainLedger, SubLedger, SubLedger1, PrevDebitAmount,
        /// PrevCreditAmout, PrevBalance, DebitAmount, CreditAmount, Balance,
        /// TotalBalance, BudgetAmount(, SubBudgetAmount).
        ///
        /// Monthly (#tempLedgerBalanceMonthlyFinal2) columns: LedgerHead,
        /// MainLedger, SubLedger, SubLedger1, Opening, Srawan..Aashad,
        /// Closing, BudgetAmount(, SubBudgetAmount).
        /// </summary>
        private static MonthlyReportRowDto MapRow(IDictionary<string, object> dict, bool isMonthly)
        {
            string? Get(string key) => dict.TryGetValue(key, out var v) && v != null && v != DBNull.Value ? v.ToString() : null;
            decimal GetDec(string key) => dict.TryGetValue(key, out var v) && v != null && v != DBNull.Value ? Convert.ToDecimal(v) : 0m;

            var dto = new MonthlyReportRowDto
            {
                LedgerHead = Get("LedgerHead"),
                MainLedger = Get("MainLedger"),
                SubLedger = Get("SubLedger"),
                SubLedger1 = Get("SubLedger1"),
                BudgetAmount = GetDec("BudgetAmount")
            };

            if (isMonthly)
            {
                dto.CurrentAmount = GetDec("Closing");
                dto.PreviousAmount = GetDec("Opening");
            }
            else
            {
                dto.CurrentAmount = GetDec("Balance");
                dto.PreviousAmount = GetDec("PrevBalance");
            }

            return dto;
        }

        private async Task<dynamic> GetFiscalYearByDate(SqlConnection connection, DateTime date)
        {
            var sql = "SELECT AcoFiscalYearId, FiscalYearToOn FROM AcoFiscalYear WHERE FiscalYearFromOn <= @date AND FiscalYearToOn >= @date";
            var result = await connection.QueryFirstOrDefaultAsync(sql, new { date });
            return result;
        }
    }
}