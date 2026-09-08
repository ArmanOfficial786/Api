// Repositories/Implementations/Account/SubLedgerDetailsReport/SubLedgerDetailsRepository.cs
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using NexgenCosysReport.DbContext;
using NexgenCosysReport.Dtos.RequestDtos.Account.SubLedgerDetailsReport;
using NexgenCosysReport.Inteface.ServiceInterface.Account.SubLedgerDetailsReport;
using NexgenCosysReport.Inteface.ServiceInterface.Common;
using System.Data;
using System.Text;

namespace NexgenCosysReport.Repository.Account.SubLedgerDetailsReport
{
    public class SubLedgerDetailsRepository : ISubLedgerDetailsRepository
    {
        private readonly AppDbContext _context;
        private readonly IDateConverterService _dateConverter;
        private readonly ILogger<SubLedgerDetailsRepository> _logger;

        // Fiscal-year-closing lookup tables (AcoAccountYearClosing / AcoFiscalYear) are
        // queried directly here rather than through their own repositories, mirroring
        // the legacy BLL's inline instantiation of CAcoAccountYearClosing/CAcoFiscalYear.
        public SubLedgerDetailsRepository(
            AppDbContext context,
            IDateConverterService dateConverter,
            ILogger<SubLedgerDetailsRepository> logger)
        {
            _context = context;
            _dateConverter = dateConverter;
            _logger = logger;
        }

        private record DateFilterSet(string SqlFilterExp, string SqlFilterExpOpening, string SqlFilterExpClosing);

        // --------------------------------------------------------------
        // Mirrors the legacy fiscal-year-aware opening/closing date logic:
        // for account types 3/4 (Income/Expense-like types, per legacy
        // convention), the opening balance window is bounded by the most
        // recent closed fiscal year rather than a flat "before fromDate".
        // --------------------------------------------------------------
        private async Task<DateFilterSet> BuildDateFiltersAsync(
            SqlConnection connection, SubLedgerDetailsRequestDto request)
        {
            if (string.IsNullOrEmpty(request.FromDateBs) || string.IsNullOrEmpty(request.ToDateBs)
                || request.FromDateBs == "-1" || request.ToDateBs == "-1")
            {
                return new DateFilterSet(string.Empty, string.Empty, string.Empty);
            }

            var fromDateAd = await _dateConverter.NepaliToEnglishAsync(request.FromDateBs);
            var toDateAd = await _dateConverter.NepaliToEnglishAsync(request.ToDateBs);
            var dateFrom = fromDateAd.ToString("yyyy-MM-dd");
            var dateTo = toDateAd.ToString("yyyy-MM-dd");

            if (request.SelectedAccountType != 3 && request.SelectedAccountType != 4)
            {
                return new DateFilterSet(
                    $" And v.VoucherOn between '{dateFrom}' And '{dateTo}' ",
                    $" And v.VoucherOn < '{dateFrom}'",
                    $" And v.VoucherOn <= '{dateTo}'");
            }

            // ---- Account types 3/4: fiscal-year-closing-aware branch ----
            var maxFiscalYearId = await connection.QueryFirstOrDefaultAsync<int?>(
                "SELECT MAX(AcoFiscalYearId) FROM AcoAccountYearClosing");

            if (!maxFiscalYearId.HasValue || maxFiscalYearId.Value == 0)
            {
                return new DateFilterSet(
                    $" And v.VoucherOn between '{dateFrom}' And '{dateTo}' ",
                    $" And v.VoucherOn < '{dateFrom}'",
                    $" And v.VoucherOn <= '{dateTo}'");
            }

            var fiscalYearToOn = await connection.QueryFirstOrDefaultAsync<DateTime?>(
                "SELECT FiscalYearToOn FROM AcoFiscalYear WHERE AcoFiscalYearId = @Id",
                new { Id = maxFiscalYearId.Value });

            if (fiscalYearToOn.HasValue && fiscalYearToOn.Value < fromDateAd.AddDays(-1))
            {
                var fiscalYearDateNext = fiscalYearToOn.Value.AddDays(1).ToString("yyyy-MM-dd");
                return new DateFilterSet(
                    $" And v.VoucherOn between '{dateFrom}' And '{dateTo}' ",
                    $" And v.VoucherOn between '{fiscalYearDateNext}' And '{dateFrom}' ",
                    $" And v.VoucherOn between '{fiscalYearDateNext}' And '{dateTo}' ");
            }

            return new DateFilterSet(
                $" And v.VoucherOn between '{dateFrom}' And '{dateTo}' ",
                " And v.VoucherOn ='1900-01-01'",
                $" And v.VoucherOn between '{dateFrom}' And '{dateTo}' ");
        }

        private static string BuildSqlOrderBy(SubLedgerDetailsRequestDto request)
        {
            var orderBy = new StringBuilder(" order by VoucherOn asc");

            switch (request.OrderBy?.Trim())
            {
                case "Main Ledger": orderBy.Append(" , MainLedger"); break;
                case "Sub Ledger": orderBy.Append(" , SubLedger1"); break;
                case "Debit Amount": orderBy.Append(" , DebitAmount DESC"); break;
                case "Credit Amount": orderBy.Append(" , CreditAmount DESC"); break;
                case "Balance": orderBy.Append(" , BalanceAmount DESC"); break;
            }

            return orderBy.ToString();
        }

        // --------------------------------------------------------------
        // SqlFilterMainLedger — builds up progressively based on ReportType,
        // matching the legacy if/else-if chain exactly. ledgerHead must
        // have 5 entries: [0]=MainLedger .. [4]=SubLedger4.
        // --------------------------------------------------------------
        private static string BuildLedgerFilter(SubLedgerDetailsRequestDto request)
        {
            var ledgerHead = request.LedgerHead;
            while (ledgerHead.Count < 5) ledgerHead.Add(string.Empty); // guard against short lists

            var filter = new StringBuilder($"And MainLedger = '{ledgerHead[0]}'");

            switch (request.ReportType)
            {
                case "LedgerDetailsReport":
                    break; // MainLedger only
                case "1stLedgerDetailsReport":
                    filter.Append($" And SubLedger1 = N'{ledgerHead[1]}'");
                    break;
                case "2ndLedgerDetailsReport":
                    filter.Append($" And SubLedger1 = N'{ledgerHead[1]}' And SubLedger2 = N'{ledgerHead[2]}'");
                    break;
                case "3rdLedgerDetailsReport":
                    filter.Append($" And SubLedger1 = N'{ledgerHead[1]}' And SubLedger2 = N'{ledgerHead[2]}' And SubLedger3 = N'{ledgerHead[3]}'");
                    break;
                case "4thLedgerDetailsReport":
                default:
                    filter.Append($" And SubLedger1 = N'{ledgerHead[1]}' And SubLedger2 = N'{ledgerHead[2]}' And SubLedger3 = N'{ledgerHead[3]}' And SubLedger4 = N'{ledgerHead[4]}'");
                    break;
            }

            return filter.ToString();
        }

        public async Task<SubLedgerDetailsData> GetReportDataAsync(SubLedgerDetailsRequestDto request)
        {
            try
            {
                var connectionString = _context.Database.GetConnectionString();
                using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();

                var dateFilters = await BuildDateFiltersAsync(connection, request);

                var sqlFilterExp = new StringBuilder(dateFilters.SqlFilterExp);
                var sqlFilterExpOpening = new StringBuilder(dateFilters.SqlFilterExpOpening);
                var sqlFilterExpClosing = new StringBuilder(dateFilters.SqlFilterExpClosing);

                if (!string.IsNullOrEmpty(request.BranchId) &&
                    request.BranchId != "-1" &&
                    request.BranchId != "string" &&
                    long.TryParse(request.BranchId, out var branchId))
                {
                    sqlFilterExp.Append($" And v.UsmOfficeId = {branchId}");
                    sqlFilterExpOpening.Append($" And v.UsmOfficeId = {branchId}");
                    sqlFilterExpClosing.Append($" And v.UsmOfficeId = {branchId}");
                }

                if (request.VoucherType == "Auto")
                {
                    sqlFilterExp.Append(" And vp.IsAutomatic = 1");
                    sqlFilterExpOpening.Append(" And vp.IsAutomatic = 1");
                    sqlFilterExpClosing.Append(" And vp.IsAutomatic = 1");
                }
                else if (request.VoucherType == "Manual")
                {
                    sqlFilterExp.Append(" And vp.IsAutomatic = 0");
                    sqlFilterExpOpening.Append(" And vp.IsAutomatic = 0");
                    sqlFilterExpClosing.Append(" And vp.IsAutomatic = 0");
                }

                var sqlFilterExpOrderBy = BuildSqlOrderBy(request);
                var sqlFilterMainLedger = BuildLedgerFilter(request);

                var parameters = new DynamicParameters();
                parameters.Add("@SqlFilterExp", sqlFilterExp.ToString(), DbType.String, size: -1);
                parameters.Add("@SqlFilterExpOrderBy", sqlFilterExpOrderBy, DbType.String, size: -1);
                parameters.Add("@SqlFilterExpOpening", sqlFilterExpOpening.ToString(), DbType.String, size: -1);
                parameters.Add("@SqlFilterExpClosing", sqlFilterExpClosing.ToString(), DbType.String, size: -1);
                parameters.Add("@SqlFilterMainLedger", sqlFilterMainLedger, DbType.String, size: -1);
                parameters.Add("@ShowOpeningBalance", request.ShowOpeningBalance);
                parameters.Add("@openingBalance", dbType: DbType.Double, direction: ParameterDirection.Output);
                parameters.Add("@closingBalance", dbType: DbType.Double, direction: ParameterDirection.Output);
                parameters.Add("@SqlFilterExpAccountType", dbType: DbType.String, size: 15, direction: ParameterDirection.Output);

                var rows = (await connection.QueryAsync<SubLedgerDetailsRowDto>(
                    "sp_6_56_GetLedgerDetails",
                    parameters,
                    commandType: CommandType.StoredProcedure,
                    commandTimeout: 120
                )).AsList();

                var openingBalance = parameters.Get<double?>("@openingBalance") ?? 0;
                var closingBalance = parameters.Get<double?>("@closingBalance") ?? 0;
                var accountType = parameters.Get<string?>("@SqlFilterExpAccountType");

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

                return new SubLedgerDetailsData
                {
                    Rows = rows,
                    TotalRecords = rows.Count,
                    OpeningBalance = (decimal)openingBalance,
                    ClosingBalance = (decimal)closingBalance,
                    AccountType = accountType,
                    FromDateBs = request.FromDateBs,
                    ToDateBs = request.ToDateBs,
                    BranchName = branchName,
                    VoucherType = request.VoucherType,
                    OrderBy = request.OrderBy,
                    LedgerHead = request.LedgerHead,
                    ShowOpeningBalance = request.ShowOpeningBalance,
                    IsSummary = request.IsSummary,
                    ReportType = request.ReportType
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