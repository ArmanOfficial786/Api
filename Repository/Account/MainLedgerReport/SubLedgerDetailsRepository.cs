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
        // Root-cause fix: previously this switched purely on request.ReportType,
        // and any unrecognized value (e.g. "0", the placeholder Swagger fills in
        // by default) fell into the deepest "4th level" case — requiring
        // SubLedger1..4 to equal empty string. The SP never returns empty
        // string for those columns (it returns '-' when there's no posting
        // detail), so that filter could never match a row, silently producing
        // "No data found" even with valid data and a valid MainLedger name.
        //
        // Fixed by deriving the actual drill-down depth from how many non-empty
        // entries LedgerHead genuinely contains — this is what the legacy
        // webform encoded implicitly via which dropdown box had a selection —
        // rather than trusting an arbitrary ReportType string. A caller that
        // only sends ledgerHead[0] ("OFFICE EXPENSE") now correctly gets a
        // MainLedger-only filter regardless of what ReportType was set to.
        // --------------------------------------------------------------
        private static string BuildLedgerFilter(SubLedgerDetailsRequestDto request)
        {
            var suppliedHead = request.LedgerHead ?? new List<string>();

            // How many levels the caller actually supplied a real value for.
            var suppliedDepth = suppliedHead.Count(x => !string.IsNullOrWhiteSpace(x));

            // Prefer an explicit, recognized ReportType if given — but never let
            // an unrecognized value push us deeper than what was actually supplied.
            var requestedDepth = request.ReportType?.Trim() switch
            {
                "LedgerDetailsReport" => 1,
                "1stLedgerDetailsReport" => 2,
                "2ndLedgerDetailsReport" => 3,
                "3rdLedgerDetailsReport" => 4,
                "4thLedgerDetailsReport" => 5,
                _ => suppliedDepth
            };

            var depth = Math.Clamp(Math.Min(requestedDepth, Math.Max(suppliedDepth, 1)), 1, 5);

            var mainLedger = suppliedHead.Count > 0 ? suppliedHead[0] : string.Empty;
            var filter = new StringBuilder($"And MainLedger = '{mainLedger}'");

            for (int level = 1; level < depth; level++)
            {
                var value = suppliedHead.Count > level ? suppliedHead[level] : string.Empty;
                filter.Append($" And SubLedger{level} = N'{value}'");
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
                parameters.Add("@openingBalance", dbType: DbType.Decimal, direction: ParameterDirection.Output);
                parameters.Add("@closingBalance", dbType: DbType.Decimal, direction: ParameterDirection.Output);
                parameters.Add("@SqlFilterExpAccountType", dbType: DbType.String, size: -1, direction: ParameterDirection.Output);
                parameters.Add("@SqlFilterMainLedger", sqlFilterMainLedger, DbType.String, size: -1);

                var rows = (await connection.QueryAsync<SubLedgerDetailsRowDto>(
                    "sp_6_56_GetLedgerDetails",
                    parameters,
                    commandType: CommandType.StoredProcedure,
                    commandTimeout: 120
                )).AsList();

                var openingBalance = parameters.Get<decimal?>("@openingBalance") ?? 0m;
                var closingBalance = parameters.Get<decimal?>("@closingBalance") ?? 0m;
                var accountType = parameters.Get<string?>("@SqlFilterExpAccountType")
                                   ?? rows.FirstOrDefault()?.AccountType;

                var totalDebit = rows.Sum(r => r.DebitAmount ?? 0);
                var totalCredit = rows.Sum(r => r.CreditAmount ?? 0);

                var branchName = await ResolveBranchNameAsync(connection, request.BranchId);

                return new SubLedgerDetailsData
                {
                    Rows = rows,
                    TotalRecords = rows.Count,
                    TotalDebitAmount = totalDebit,
                    TotalCreditAmount = totalCredit,
                    OpeningBalance = openingBalance,
                    ClosingBalance = closingBalance,
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

        private static async Task<string> ResolveBranchNameAsync(SqlConnection connection, string? branchId)
        {
            if (string.IsNullOrEmpty(branchId) || branchId == "-1" || branchId == "string" ||
                !long.TryParse(branchId, out var id))
            {
                return "All";
            }

            var name = await connection.QueryFirstOrDefaultAsync<string>(
                "SELECT OfficeName FROM UsmOffice WHERE UsmOfficeId = @Id", new { Id = id });

            return string.IsNullOrEmpty(name) ? "All" : name;
        }
    }
}