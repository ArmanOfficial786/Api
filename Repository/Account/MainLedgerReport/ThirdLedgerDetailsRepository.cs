// Repository/Account/ThirdLedgerDetailsReport/ThirdLedgerDetailsRepository.cs
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using NexgenCosysReport.DbContext;
using NexgenCosysReport.Dtos.RequestDtos.Account.ThirdLedgerDetailsReport;
using NexgenCosysReport.Inteface.ServiceInterface.Account.ThirdLedgerDetailsReport;
using NexgenCosysReport.Inteface.ServiceInterface.Common;
using System.Data;
using System.Text;

namespace NexgenCosysReport.Repository.Account.ThirdLedgerDetailsReport
{
    public class ThirdLedgerDetailsRepository : IThirdLedgerDetailsRepository
    {
        private const int IncomeAccountTypeId = 3;
        private const int ExpensesAccountTypeId = 4;

        private readonly AppDbContext _context;
        private readonly IDateConverterService _dateConverter;
        private readonly ILogger<ThirdLedgerDetailsRepository> _logger;

        public ThirdLedgerDetailsRepository(
            AppDbContext context,
            IDateConverterService dateConverter,
            ILogger<ThirdLedgerDetailsRepository> logger)
        {
            _context = context;
            _dateConverter = dateConverter;
            _logger = logger;
        }

        // --------------------------------------------------------------
        // @SqlFilterExp — date range, branch, voucher type, and account type
        // ONLY. Ledger-name filtering (MainLedger/SubLedger1/2/3) does NOT
        // belong here — those are the SP's own computed output-column
        // aliases, not real columns on l/vpd. Filtering vpd.SubLedger2 /
        // vpd.SubLedger3 directly (as this method previously did) throws
        // "Invalid column name" because AcoVoucherPostingDetail has no such
        // columns; the level-name filtering must go into @SqlFilterMainLedger
        // instead, applied by the SP against its own SELECT output.
        // --------------------------------------------------------------
        private async Task<string> BuildSqlFilterExpAsync(ThirdLedgerDetailsRequestDto request)
        {
            var filter = new StringBuilder();

            if (!string.IsNullOrEmpty(request.FromDate) && !string.IsNullOrEmpty(request.ToDate)
                && request.FromDate != "-1" && request.ToDate != "-1")
            {
                var fromDateAd = await _dateConverter.BsToAdStringAsync(request.FromDate);
                var toDateAd = await _dateConverter.BsToAdStringAsync(request.ToDate);

                if (!string.IsNullOrEmpty(fromDateAd) && !string.IsNullOrEmpty(toDateAd))
                {
                    filter.Append(" AND v.VoucherOn BETWEEN '").Append(fromDateAd)
                          .Append("' AND '").Append(toDateAd).Append('\'');
                }
            }

            AppendBranchFilter(filter, "v.UsmOfficeId", request.BranchIds);

            if (!string.IsNullOrEmpty(request.VoucherType) &&
                !string.Equals(request.VoucherType, "All", StringComparison.OrdinalIgnoreCase))
            {
                var isAuto = string.Equals(request.VoucherType, "Auto", StringComparison.OrdinalIgnoreCase);
                filter.Append(" AND vp.IsAutomatic = ").Append(isAuto ? '1' : '0');
            }

            if (request.LedgerHeadId > 0)
            {
                filter.Append(" AND l.AcoAccountTypeId = ").Append(request.LedgerHeadId);
            }

            return filter.ToString();
        }

        // --------------------------------------------------------------
        // @SqlFilterExpOpening / @SqlFilterExpClosing — same fiscal-year-aware
        // logic as First/Second Ledger Details.
        // --------------------------------------------------------------
        private async Task<(string opening, string closing)> BuildOpeningClosingFilterExpAsync(
            SqlConnection connection,
            ThirdLedgerDetailsRequestDto request,
            string dateFrom,
            string dateTo)
        {
            var opening = new StringBuilder();
            var closing = new StringBuilder();

            var isIncomeOrExpense = request.LedgerHeadId is IncomeAccountTypeId or ExpensesAccountTypeId;

            if (isIncomeOrExpense)
            {
                var lastClosedFiscalYearId = await connection.QueryFirstOrDefaultAsync<int?>(
                    "SELECT MAX(AcoFiscalYearId) FROM AcoAccountYearClosing") ?? 0;

                if (lastClosedFiscalYearId == 0)
                {
                    opening.Append(" And v.VoucherOn < '").Append(dateFrom).Append('\'');
                    closing.Append(" And v.VoucherOn <= '").Append(dateTo).Append('\'');
                }
                else
                {
                    var fiscalYearToOn = await connection.QueryFirstOrDefaultAsync<DateTime?>(
                        "SELECT FiscalYearToOn FROM AcoFiscalYear WHERE AcoFiscalYearId = @Id",
                        new { Id = lastClosedFiscalYearId });

                    var dateFromParsed = DateTime.Parse(dateFrom);

                    if (fiscalYearToOn.HasValue && fiscalYearToOn.Value < dateFromParsed.AddDays(-1))
                    {
                        var fiscalYearNext = fiscalYearToOn.Value.AddDays(1).ToString("yyyy-MM-dd");
                        opening.Append(" And v.VoucherOn between '").Append(fiscalYearNext)
                               .Append("' And '").Append(dateFrom).Append('\'');
                        closing.Append(" And v.VoucherOn between '").Append(fiscalYearNext)
                               .Append("' And '").Append(dateTo).Append('\'');
                    }
                    else
                    {
                        opening.Append(" And v.VoucherOn ='1900-01-01'");
                        closing.Append(" And v.VoucherOn between '").Append(dateFrom)
                               .Append("' And '").Append(dateTo).Append('\'');
                    }
                }
            }
            else
            {
                opening.Append(" And v.VoucherOn < '").Append(dateFrom).Append('\'');
                closing.Append(" And v.VoucherOn <= '").Append(dateTo).Append('\'');
            }

            AppendBranchFilter(opening, "v.UsmOfficeId", request.BranchIds);
            AppendBranchFilter(closing, "v.UsmOfficeId", request.BranchIds);

            return (opening.ToString(), closing.ToString());
        }

        // --------------------------------------------------------------
        // @SqlFilterMainLedger — this controller is the "3rd Ledger Details
        // Report": filters on MainLedger, SubLedger1, SubLedger2, AND
        // SubLedger3 — matches the webform's reportType ==
        // "3rdLedgerDetailsReport" branch exactly (SqlFilterMainLedger +=
        // "And MainLedger = ... And SubLedger1 = ... And SubLedger2 = ...
        // And SubLedger3 = ..."). This was previously always sent as an
        // empty string, which silently dropped all four levels of ledger
        // filtering from the query.
        // --------------------------------------------------------------
        private static string BuildSqlFilterMainLedger(ThirdLedgerDetailsRequestDto request)
        {
            var mainLedger = request.LedgerName ?? string.Empty;
            var subLedger1 = request.SubLedgerName ?? string.Empty;
            var subLedger2 = request.SecondSubLedgerName ?? string.Empty;
            var subLedger3 = request.ThirdSubLedgerName ?? string.Empty;

            return $"And MainLedger = '{mainLedger}' And SubLedger1 = N'{subLedger1}'" +
                   $" And SubLedger2 = N'{subLedger2}' And SubLedger3 = N'{subLedger3}'";
        }

        private static string BuildSqlOrderBy(ThirdLedgerDetailsRequestDto request)
        {
            const string baseOrderBy = " order by VoucherOn asc";

            if (string.IsNullOrEmpty(request.OrderBy) || request.OrderBy == "-1")
            {
                return baseOrderBy;
            }

            return request.OrderBy.Trim().ToLowerInvariant() switch
            {
                "voucher no" => baseOrderBy + " , VoucherNo",
                "main ledger" => baseOrderBy + " , MainLedger",
                "sub ledger" => baseOrderBy + " , SubLedger1",
                "debit amount" => baseOrderBy + " , DebitAmount DESC",
                "credit amount" => baseOrderBy + " , CreditAmount DESC",
                "balance" => baseOrderBy + " , BalanceAmount DESC",
                _ => baseOrderBy
            };
        }

        private static void AppendBranchFilter(StringBuilder filter, string column, string? branchIds)
        {
            if (!string.IsNullOrEmpty(branchIds) && branchIds != "-1" && branchIds != "string")
            {
                filter.Append(" AND ").Append(column).Append(" IN (").Append(branchIds).Append(')');
            }
        }

        public async Task<ThirdLedgerDetailsData> GetThirdLedgerDetailsDataAsync(ThirdLedgerDetailsRequestDto request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.FromDate) || string.IsNullOrEmpty(request.ToDate)
                    || request.FromDate == "-1" || request.ToDate == "-1")
                {
                    throw new ArgumentException("FromDate and ToDate are required.");
                }

                var connectionString = _context.Database.GetConnectionString();
                await using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();

                var dateFromAd = await _dateConverter.BsToAdStringAsync(request.FromDate);
                var dateToAd = await _dateConverter.BsToAdStringAsync(request.ToDate);

                var sqlFilterExp = await BuildSqlFilterExpAsync(request);
                var sqlOrderBy = BuildSqlOrderBy(request);
                var (sqlFilterExpOpening, sqlFilterExpClosing) =
                    await BuildOpeningClosingFilterExpAsync(connection, request, dateFromAd, dateToAd);
                var sqlFilterMainLedger = BuildSqlFilterMainLedger(request);

                string? accountTypeName = null;
                if (request.LedgerHeadId > 0)
                {
                    accountTypeName = await connection.QueryFirstOrDefaultAsync<string>(
                        "SELECT AccountType FROM AcoAccountType WHERE AcoAccountTypeId = @LedgerHeadId",
                        new { LedgerHeadId = request.LedgerHeadId });
                }

                var isSummary = string.Equals(request.ReportType, "Summary", StringComparison.OrdinalIgnoreCase);
                var spName = isSummary ? "sp_6_56_GetLedgerDetailsSummary" : "sp_6_56_GetLedgerDetails";

                var parameters = new DynamicParameters();
                parameters.Add("@SqlFilterExp", sqlFilterExp, DbType.String, size: -1);
                parameters.Add("@SqlFilterExpOrderBy", sqlOrderBy, DbType.String, size: -1);
                parameters.Add("@SqlFilterExpOpening", sqlFilterExpOpening, DbType.String, size: -1);
                parameters.Add("@SqlFilterExpClosing", sqlFilterExpClosing, DbType.String, size: -1);
                parameters.Add("@openingBalance", dbType: DbType.Decimal, direction: ParameterDirection.Output);
                parameters.Add("@closingBalance", dbType: DbType.Decimal, direction: ParameterDirection.Output);
                parameters.Add("@SqlFilterExpAccountType", dbType: DbType.String, direction: ParameterDirection.Output, size: -1);
                parameters.Add("@SqlFilterMainLedger", sqlFilterMainLedger, DbType.String, size: -1);

                var rows = (await connection.QueryAsync<ThirdLedgerDetailsRowDto>(
                    spName,
                    parameters,
                    commandType: CommandType.StoredProcedure,
                    commandTimeout: 120
                )).ToList();

                var openingBalanceOut = parameters.Get<decimal?>("@openingBalance") ?? 0m;
                var closingBalanceOut = parameters.Get<decimal?>("@closingBalance") ?? 0m;
                var accountTypeOut = parameters.Get<string?>("@SqlFilterExpAccountType") ?? accountTypeName;

                var totalDebit = rows.Sum(r => r.DebitAmount ?? 0m);
                var totalCredit = rows.Sum(r => r.CreditAmount ?? 0m);
                var totalBalance = isSummary ? totalDebit - totalCredit : totalCredit - totalDebit;

                var branchNames = await ResolveBranchNamesAsync(connection, request.BranchIds);

                return new ThirdLedgerDetailsData
                {
                    Rows = rows,
                    TotalRecords = rows.Count,
                    TotalDebitAmount = totalDebit,
                    TotalCreditAmount = totalCredit,
                    TotalBalance = totalBalance,
                    OpeningBalance = openingBalanceOut,
                    ClosingBalance = closingBalanceOut,
                    AccountType = accountTypeOut,
                    FromDateBs = request.FromDate,
                    ToDateBs = request.ToDate,
                    LedgerName = request.LedgerName,
                    SubLedgerName = request.SubLedgerName,
                    SecondSubLedgerName = request.SecondSubLedgerName,
                    ThirdSubLedgerName = request.ThirdSubLedgerName,
                    VoucherType = request.VoucherType,
                    ReportType = request.ReportType,
                    ShowOpeningBalance = request.ShowOpeningBalance,
                    OrderBy = request.OrderBy,
                    LedgerHeadName = accountTypeName,
                    BranchNames = branchNames
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetThirdLedgerDetailsDataAsync");
                throw;
            }
        }

        // Root-cause fix: STRING_AGG needs compat level 140 (SQL Server 2017+),
        // unavailable on this database — same fix as every other repository
        // in this thread.
        private static async Task<string> ResolveBranchNamesAsync(SqlConnection connection, string? branchIds)
        {
            if (string.IsNullOrEmpty(branchIds) || branchIds == "-1" || branchIds == "string")
            {
                return "All Branches";
            }

            var branchIdList = branchIds
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(id => long.TryParse(id, out var parsed) ? (long?)parsed : null)
                .Where(id => id.HasValue)
                .Select(id => id!.Value)
                .Distinct()
                .ToList();

            if (branchIdList.Count == 0)
            {
                return "All Branches";
            }

            const string sql = @"
                SELECT OfficeName
                FROM UsmOffice
                WHERE UsmOfficeId IN @Ids
                ORDER BY OfficeName";

            var names = (await connection.QueryAsync<string>(sql, new { Ids = branchIdList })).ToList();
            return names.Count > 0 ? string.Join(", ", names) : "All Branches";
        }
    }
}