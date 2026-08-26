using Dapper;
using NexgenCosysReport.DbContext;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using NexgenCosysReport.Dtos.RequestDtos.Account;
using NexgenCosysReport.Dtos.RequestDtos.Account.NexgenCosysReport.Dtos.RequestDtos.AccountOperation;
using NexgenCosysReport.Inteface.ServiceInterface.Common;
using System.Data;
using System.Text.RegularExpressions;
using NexgenCosysReport.Inteface.ServiceInterface.Account.AccountingReport;

namespace NexgenCosysReport.Repository.Account.AccountingReport
{
    public class BalanceSheetRepository : IBalanceSheet
    {
        // ============================================================================
        // IMPORTANT - read before touching the date format below.
        //
        // sp_6_56_GetBalanceSheet[NoOpening][Summary]ForReport all extract the till-date
        // out of @SqlFilterExpCurrent using raw character math:
        //
        //   substring(@SqlFilterExpCurrent, CHARINDEX('/', @SqlFilterExpCurrent) - 2, 10)
        //
        // This ONLY produces a valid 10-character "MM/dd/yyyy" substring. Any other
        // date format (yyyy-MM-dd, yyyyMMdd, dd/MM/yyyy, etc.) breaks this extraction
        // and cascades into "Conversion failed when converting date..." or garbled
        // dynamic SQL further down in the same proc. Do not change this format unless
        // the stored procedures are also changed.
        // ============================================================================
        private const string SqlDateLiteralFormat = "MM/dd/yyyy";

        // Only digits and commas are ever legitimate here - @branchId is concatenated
        // directly into dynamic SQL inside every one of these procs with zero escaping
        // (e.g. "... And v.UsmOfficeId in (' + @branchId + ')"). This guards against
        // SQL injection via that parameter since the proc itself does not.
        private static readonly Regex BranchIdsPattern = new(@"^-?\d+(,\d+)*$", RegexOptions.Compiled);

        private readonly AppDbContext _context;
        private readonly IDateConverterService _dateConverter;
        private readonly ILogger<BalanceSheetRepository> _logger;

        public BalanceSheetRepository(
            AppDbContext context,
            IDateConverterService dateConverter,
            ILogger<BalanceSheetRepository> logger)
        {
            _context = context;
            _dateConverter = dateConverter;
            _logger = logger;
        }

        public async Task<BalanceSheetReportData> GetBalanceSheetReport(BalanceSheetRequest request)
        {
            var connectionString = _context.Database.GetConnectionString();
            using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();

            // 1. Fiscal year (BS) + its AD end date - used as the "previous balance" cutoff.
            var fiscalYear = GetFiscalYearFromNepaliDate(request.TillDate);
            var fiscalYearToOn = await GetFiscalYearToDate(connection, fiscalYear);

            // 2. Convert till date (BS -> AD) and format EXACTLY as the proc expects.
            var tillDateAd = await _dateConverter.NepaliToEnglishAsync(request.TillDate);
            string tillDateStr = tillDateAd.ToString(SqlDateLiteralFormat);
            string preDateStr = fiscalYearToOn.ToString(SqlDateLiteralFormat);

            string sqlFilterExpCurrent = $" And v.VoucherOn <= '{tillDateStr}'";
            string sqlFilterExpPrevious = $" And v.VoucherOn <= '{preDateStr}'";

            // 3. Resolve and validate the branch/office id list.
            //    NOTE: unlike what the "-1 = all" convention suggests, none of these
            //    stored procedures accept -1 as an "all offices" sentinel - they always
            //    do a literal "IN (@branchId)". The original WebForms code never sent
            //    "-1" here either; it always sent the actual comma list of checked
            //    office ids. Callers of this API must supply the real office id list.
            if (string.IsNullOrWhiteSpace(request.BranchIds) || !BranchIdsPattern.IsMatch(request.BranchIds))
            {
                throw new ArgumentException(
                    "BranchIds must be a comma-separated list of office ids (e.g. \"1,2,3\"). " +
                    "\"-1\" / \"all\" is not supported by the underlying stored procedures.");
            }

            string branchIdCsv = request.BranchIds;

            // 4. Order-by clause. All four #final result sets share the same columns
            //    (LedgerNo, SubLedger, CurrentAmount, PreviousAmount), so any of these
            //    mappings is safe against every variant.
            string sqlFilterExpOrderBy = BuildOrderByClause(request.OrderBy);

            // 5. This is the crux of the fix: replicate the original WebForms branching
            //    (btnViewReport_Click: "if (ledrbal.Count() > 0) use *ForReport else use plain").
            //    The *ForReport procs have a latent bug where an empty
            //    LedgerFinalBalanceCalcDateWiseForOffice table leaves @preCalcdate empty,
            //    which corrupts their dynamic SQL (this is exactly what produced both the
            //    "Incorrect syntax near 'GROUP'" and "Incorrect syntax near 'Insert'" errors).
            //    Only call the *ForReport variants when that cache table actually has data.
            bool useCachedForReportVariant = await LedgerBalanceCacheHasData(connection);

            var spName = GetStoredProcedureName(
                request.IncludePreviousYearBalance,
                request.ReportType,
                useCachedForReportVariant);

            var parameters = new DynamicParameters();

            if (useCachedForReportVariant)
            {
                // *ForReport signature: (@SqlFilterExpCurrent, @SqlFilterExpPrevious, @branchId, @SqlFilterExpOrderBy)
                parameters.Add("@SqlFilterExpCurrent", sqlFilterExpCurrent);
                parameters.Add("@SqlFilterExpPrevious", sqlFilterExpPrevious);
                parameters.Add("@branchId", branchIdCsv);
                parameters.Add("@SqlFilterExpOrderBy", sqlFilterExpOrderBy);
            }
            else
            {
                // Plain signature (matches the original BLL's GetBalanceSheet/GetBalanceSheetNoOpening):
                // (@SqlFilterExpCurrent, @SqlFilterExpPrevious, @SqlFilterExpOrderBy) - branch filter is
                // embedded into the current/previous filter fragments on the C# side instead.
                sqlFilterExpCurrent += $" And v.UsmOfficeId in ({branchIdCsv})";
                sqlFilterExpPrevious += $" And v.UsmOfficeId in ({branchIdCsv})";

                parameters.Add("@SqlFilterExpCurrent", sqlFilterExpCurrent);
                parameters.Add("@SqlFilterExpPrevious", sqlFilterExpPrevious);
                parameters.Add("@SqlFilterExpOrderBy", sqlFilterExpOrderBy);
            }

            try
            {
                var rows = await connection.QueryAsync<BalanceSheetDto>(
                    spName,
                    parameters,
                    commandType: CommandType.StoredProcedure,
                    commandTimeout: 120
                );

                var rowList = rows.AsList();

                return new BalanceSheetReportData
                {
                    Rows = rowList,
                    TotalDebit = rowList.Sum(r => r.DebitAmount),
                    TotalCredit = rowList.Sum(r => r.CreditAmount),
                    FiscalYearFrom = fiscalYearToOn.AddYears(-1).AddDays(1),
                    FiscalYearTo = fiscalYearToOn,
                    FiscalYearLabel = fiscalYear
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "BalanceSheet SP failed. SP={SpName} UsedCachedVariant={UsedCache} Current='{Current}' Previous='{Previous}' Branch='{Branch}' OrderBy='{OrderBy}'",
                    spName, useCachedForReportVariant, sqlFilterExpCurrent, sqlFilterExpPrevious, branchIdCsv, sqlFilterExpOrderBy);
                throw;
            }
        }

        // ---- Helper methods ----

        private static string GetFiscalYearFromNepaliDate(string nepaliDate)
        {
            if (string.IsNullOrEmpty(nepaliDate))
                return string.Empty;

            var parts = nepaliDate.Split('/');
            if (parts.Length < 2)
                return string.Empty;

            int year = int.Parse(parts[0]);
            int month = int.Parse(parts[1]);

            // Matches the ORIGINAL WebForms logic exactly (aspx.cs btnViewReport_Click):
            //   if (month > 3)  fiscalYear = (year-1) + "/" + year.Substring(2,2)
            //   else            fiscalYear = (year-2) + "/" + (year-1).Substring(2,2)
            return month > 3
                ? $"{year - 1}/{year.ToString().Substring(2, 2)}"
                : $"{year - 2}/{(year - 1).ToString().Substring(2, 2)}";
        }

        private async Task<DateTime> GetFiscalYearToDate(SqlConnection connection, string fiscalYear)
        {
            const string sql = "SELECT FiscalYearToOn FROM AcoFiscalYear WHERE FiscalYear = @FiscalYear";
            var result = await connection.QueryFirstOrDefaultAsync<DateTime?>(sql, new { FiscalYear = fiscalYear });

            if (result.HasValue)
                return result.Value;

            _logger.LogWarning("Fiscal year {FiscalYear} not found. Using fallback (today).", fiscalYear);
            return DateTime.Now;
        }

        /// <summary>
        /// Mirrors the original WebForms check:
        ///   var ledrbal = cLedgerFinalBalanceCalcDateWiseForOffice.GetByDate(tilDate);
        ///   if (ledrbal.Count() > 0) { use *ForReport } else { use the plain SPs }
        /// The stored procedures themselves check "does this table have ANY rows",
        /// so we check the same thing here to decide which SP family to call -
        /// avoiding the *ForReport procs' empty-@preCalcdate bug entirely.
        /// </summary>
        private static async Task<bool> LedgerBalanceCacheHasData(SqlConnection connection)
        {
            const string sql = "SELECT CASE WHEN EXISTS (SELECT 1 FROM LedgerFinalBalanceCalcDateWiseForOffice) THEN 1 ELSE 0 END";
            var result = await connection.ExecuteScalarAsync<int>(sql);
            return result == 1;
        }

        private static string GetStoredProcedureName(bool includePreviousBalance, string reportType, bool useForReportVariant)
        {
            bool isSummary = reportType == "Summary" || reportType == "SubLedger";

            string opening = includePreviousBalance ? "" : "NoOpening";
            string summary = isSummary ? "Summary" : "";
            string suffix = useForReportVariant ? "ForReport" : "";

            return $"sp_6_56_GetBalanceSheet{opening}{summary}{suffix}";
        }

        /// <summary>
        /// All four #final result sets (ForReport and plain) expose LedgerNo, SubLedger,
        /// CurrentAmount and PreviousAmount, so every one of these mappings is safe
        /// regardless of which specific stored procedure ends up being called.
        /// </summary>
        private static string BuildOrderByClause(string orderBy)
        {
            return orderBy switch
            {
                "Ledger No" => " order by LedgerNo",
                "Current Balance" => " order by CurrentAmount DESC",
                "Previous Balance" => " order by PreviousAmount DESC",
                _ => " order by SubLedger" // "Ledger Name" and any unrecognized value
            };
        }
    }
}
