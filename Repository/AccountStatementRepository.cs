using Dapper;
using JsSampleReport.Dtos.RequestDtos;
using JsSampleReport.Inteface.ServiceInterface;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace JsSampleReport.Repository
{
    public class AccountStatementRepository : IAccountStatement
    {
        private readonly AppDbContext _context;
        private readonly IDateConverterService _dateConverter;

        public AccountStatementRepository(AppDbContext context, IDateConverterService dateConverter)
        {
            _context = context;
            _dateConverter = dateConverter;
        }

        // ══════════════════════════════════════════════════════════════
        // @SqlFilterExp
        // Appended inside SP WHERE vp.IsActive=1 + @SqlFilterExp
        // Uses v.VoucherOn (NOT v.VoucherDate)
        // ══════════════════════════════════════════════════════════════
        private async Task<string> BuildSqlFilterExp(AccountStatementRequest request)
        {
            var filter = string.Empty;

            if (!string.IsNullOrEmpty(request.FromDate) && !string.IsNullOrEmpty(request.ToDate) && request.FromDate != "-1" && request.ToDate != "-1")
            {
                string fromDateAd = await _dateConverter.BsToAdStringAsync(request.FromDate);
                string toDateAd = await _dateConverter.BsToAdStringAsync(request.ToDate);

                if (!string.IsNullOrEmpty(fromDateAd) && !string.IsNullOrEmpty(toDateAd))
                {
                    filter += $" AND v.VoucherOn BETWEEN '{fromDateAd}' AND '{toDateAd}'";
                }
            }

            if (!string.IsNullOrEmpty(request.BranchSelected) &&
              request.BranchSelected != "-1" &&
              request.BranchSelected != "string")
            {
                filter += $" AND v.UsmOfficeId IN ({request.BranchSelected})";
            }

            return filter;
        }

        // ══════════════════════════════════════════════════════════════
        // @SqlFilterExpOrderBy
        // Column names must match the SP final SELECT aliases
        // ══════════════════════════════════════════════════════════════
        private string BuildSqlFilterExpOrderBy(AccountStatementRequest request)
        {
            if (string.IsNullOrEmpty(request.OrderBy) ||
                request.OrderBy == "-1" ||
                request.OrderBy == "string")
            {
                return " ORDER BY SubLedger";  // default — matches legacy BLL
            }

            return request.OrderBy switch
            {
                "Ledger Name" => " ORDER BY SubLedger",
                "Debit Amount" => " ORDER BY DebitAmount  DESC",
                "Credit Amount" => " ORDER BY CreditAmount DESC",
                "Balance" => " ORDER BY Balance       DESC",
                _ => " ORDER BY SubLedger"
            };
        }

        // ══════════════════════════════════════════════════════════════
        // @SqlFilterExpType
        // SP accepts: Cash | Bank | CashBank | NonCash | anything else = All
        // ══════════════════════════════════════════════════════════════
        private string BuildSqlFilterExpType(AccountStatementRequest request)
        {
            if (string.IsNullOrEmpty(request.TransactionType) ||
                request.TransactionType == "string")
                return "All";

            return request.TransactionType; // SP handles the logic internally
        }

        // ══════════════════════════════════════════════════════════════
        // @SqlFilterExpToday  — period filter for opening/closing SP
        // v.VoucherOn between fromDate and toDate
        // ══════════════════════════════════════════════════════════════
        private string BuildSqlFilterExpToday(AccountStatementRequest request)
        {
            var filter = string.Empty;

            if (!string.IsNullOrEmpty(request.FromDate) &&
                !string.IsNullOrEmpty(request.ToDate) &&
                request.FromDate != "-1" &&
                request.ToDate != "-1")
            {
                filter += $" AND v.VoucherOn >= '{request.FromDate}'" +
                          $" AND v.VoucherOn <= '{request.ToDate}'";
            }

            if (!string.IsNullOrEmpty(request.BranchSelected) &&
                request.BranchSelected != "-1" &&
                request.BranchSelected != "string")
            {
                filter += $" AND v.UsmOfficeId IN ({request.BranchSelected})";
            }

            return filter;
        }

        // ══════════════════════════════════════════════════════════════
        // @SqlFilterExpPrevious — all transactions BEFORE fromDate
        // Used for opening balance calculation
        // ══════════════════════════════════════════════════════════════
        private string BuildSqlFilterExpPrevious(AccountStatementRequest request)
        {
            var filter = string.Empty;

            if (!string.IsNullOrEmpty(request.FromDate) &&
                request.FromDate != "-1")
            {
                filter += $" AND v.VoucherOn < '{request.FromDate}'";
            }

            if (!string.IsNullOrEmpty(request.BranchSelected) &&
                request.BranchSelected != "-1" &&
                request.BranchSelected != "string")
            {
                filter += $" AND v.UsmOfficeId IN ({request.BranchSelected})";
            }

            return filter;
        }

        // ── Main account statement ────────────────────────────────────
        public async Task<List<AccountStatementModelResponse>>
            GetAccountStatementTypeAsync(AccountStatementRequest request)
        {
            var sqlFilterExp = await BuildSqlFilterExp(request);
            var sqlFilterExpOrderBy = BuildSqlFilterExpOrderBy(request);
            var sqlFilterExpType = BuildSqlFilterExpType(request);

            var connectionString = _context.Database.GetConnectionString();
            await using var connection = new SqlConnection(connectionString);

            var parameters = new DynamicParameters();
            parameters.Add("@SqlFilterExp", sqlFilterExp);
            parameters.Add("@SqlFilterExpOrderBy", sqlFilterExpOrderBy);
            parameters.Add("@SqlFilterExpType", sqlFilterExpType);

            var result = await connection.QueryAsync<AccountStatementModelResponse>(
                "sp_6_56_GetAccountStatementType",
                parameters,
                commandType: CommandType.StoredProcedure,
                commandTimeout: 120
            );

            return result.ToList();
        }

        // ── Cash & Bank balance (opening / closing) ───────────────────
        public async Task<List<CashBankBalanceModelResponse>>
            GetCashAndBankBalanceOpeningClosingAsync(AccountStatementRequest request)
        {
            var sqlFilterExpToday = BuildSqlFilterExpToday(request);
            var sqlFilterExpPrevious = BuildSqlFilterExpPrevious(request);

            var connectionString = _context.Database.GetConnectionString();
            await using var connection = new SqlConnection(connectionString);

            var parameters = new DynamicParameters();
            parameters.Add("@SqlFilterExpToday", sqlFilterExpToday);
            parameters.Add("@SqlFilterExpPrevious", sqlFilterExpPrevious);

            var result = await connection.QueryAsync<CashBankBalanceModelResponse>(
                "sp_6_56_GetCashAndBankBalanceBankOpeningClosing",
                parameters,
                commandType: CommandType.StoredProcedure,
                commandTimeout: 120
            );

            return result.ToList();
        }
    }
}



//using Dapper;
//using JsSampleReport.Dtos.RequestDtos;
//using JsSampleReport.Inteface.ServiceInterface;
//using Microsoft.Data.SqlClient;
//using Microsoft.EntityFrameworkCore;
//using System.Data;

//namespace JsSampleReport.Repository
//{
//    public class AccountStatementRepository : IAccountStatement
//    {
//        private readonly AppDbContext _context;
//        private readonly IDateConverterService _dateConverter;

//        public AccountStatementRepository(AppDbContext context, IDateConverterService dateConverter)
//        {
//            _context = context;
//            _dateConverter = dateConverter;
//        }

//        // ── Helper: safely convert a BS date string to AD; returns empty on failure ──
//        private async Task<string> ToAdAsync(string? bsDate)
//        {
//            if (string.IsNullOrEmpty(bsDate) || bsDate == "-1")
//                return string.Empty;

//            return await _dateConverter.BsToAdStringAsync(bsDate) ?? string.Empty;
//        }

//        // ── Helper: true when BranchSelected represents a real set of branch IDs ──
//        private static bool HasBranchFilter(string? branchSelected)
//            => !string.IsNullOrEmpty(branchSelected)
//               && branchSelected != "-1"
//               && branchSelected != "string";

//        // ══════════════════════════════════════════════════════════════════════
//        // @SqlFilterExp
//        // Appended inside the SP's WHERE clause (IsActive = 1 + @SqlFilterExp).
//        // Uses v.VoucherOn (AD date column).
//        // Branch filter is only applied when SameCompanyName == false.
//        // ══════════════════════════════════════════════════════════════════════
//        private async Task<string> BuildSqlFilterExpAsync(AccountStatementRequest request)
//        {
//            var filter = string.Empty;

//            // ── Date range (BS → AD conversion) ─────────────────────────────
//            var fromAd = await ToAdAsync(request.FromDate);
//            var toAd = await ToAdAsync(request.ToDate);

//            if (!string.IsNullOrEmpty(fromAd) && !string.IsNullOrEmpty(toAd))
//                filter += $" AND v.VoucherOn BETWEEN '{fromAd}' AND '{toAd}'";

//            // ── Branch filter (only when NOT using same company name) ─────────
//            // SameCompanyName = true  → company-wide view, no branch filter
//            // SameCompanyName = false → filter to the selected branch(es)
//            if (!request.SameCompanyName && HasBranchFilter(request.BranchSelected))
//                filter += $" AND v.UsmOfficeId IN ({request.BranchSelected})";

//            return filter;
//        }

//        // ══════════════════════════════════════════════════════════════════════
//        // @SqlFilterExpOrderBy
//        // Column names must match the SP's final SELECT aliases.
//        // ══════════════════════════════════════════════════════════════════════
//        private static string BuildSqlFilterExpOrderBy(AccountStatementRequest request)
//        {
//            if (string.IsNullOrEmpty(request.OrderBy)
//                || request.OrderBy == "-1"
//                || request.OrderBy == "string")
//            {
//                return " ORDER BY SubLedger";   // default — matches legacy BLL behaviour
//            }

//            return request.OrderBy switch
//            {
//                "Ledger Name" => " ORDER BY SubLedger",
//                "Debit Amount" => " ORDER BY DebitAmount  DESC",
//                "Credit Amount" => " ORDER BY CreditAmount DESC",
//                "Balance" => " ORDER BY Balance      DESC",
//                _ => " ORDER BY SubLedger"
//            };
//        }

//        // ══════════════════════════════════════════════════════════════════════
//        // @SqlFilterExpType
//        // SP accepts: Cash | Bank | CashBank | NonCash | anything else = All
//        // ══════════════════════════════════════════════════════════════════════
//        private static string BuildSqlFilterExpType(AccountStatementRequest request)
//        {
//            return string.IsNullOrEmpty(request.TransactionType)
//                   || request.TransactionType == "string"
//                ? "All"
//                : request.TransactionType;   // SP handles the branching internally
//        }

//        // ══════════════════════════════════════════════════════════════════════
//        // @SqlFilterExpToday
//        // Period filter for the opening/closing SP:
//        //   v.VoucherOn BETWEEN fromDate AND toDate  (AD dates)
//        // Branch filter matches the same SameCompanyName logic as the main filter.
//        // ══════════════════════════════════════════════════════════════════════
//        private async Task<string> BuildSqlFilterExpTodayAsync(AccountStatementRequest request)
//        {
//            var filter = string.Empty;

//            var fromAd = await ToAdAsync(request.FromDate);
//            var toAd = await ToAdAsync(request.ToDate);

//            if (!string.IsNullOrEmpty(fromAd) && !string.IsNullOrEmpty(toAd))
//                filter += $" AND v.VoucherOn >= '{fromAd}' AND v.VoucherOn <= '{toAd}'";

//            if (!request.SameCompanyName && HasBranchFilter(request.BranchSelected))
//                filter += $" AND v.UsmOfficeId IN ({request.BranchSelected})";

//            return filter;
//        }

//        // ══════════════════════════════════════════════════════════════════════
//        // @SqlFilterExpPrevious
//        // All transactions BEFORE fromDate — used for opening balance calculation.
//        // Branch filter matches the same SameCompanyName logic.
//        // ══════════════════════════════════════════════════════════════════════
//        private async Task<string> BuildSqlFilterExpPreviousAsync(AccountStatementRequest request)
//        {
//            var filter = string.Empty;

//            var fromAd = await ToAdAsync(request.FromDate);

//            if (!string.IsNullOrEmpty(fromAd))
//                filter += $" AND v.VoucherOn < '{fromAd}'";

//            if (!request.SameCompanyName && HasBranchFilter(request.BranchSelected))
//                filter += $" AND v.UsmOfficeId IN ({request.BranchSelected})";

//            return filter;
//        }

//        // ── Main account statement ────────────────────────────────────────────
//        public async Task<List<AccountStatementModelResponse>>
//            GetAccountStatementTypeAsync(AccountStatementRequest request)
//        {
//            var sqlFilterExp = await BuildSqlFilterExpAsync(request);
//            var sqlFilterExpOrderBy = BuildSqlFilterExpOrderBy(request);
//            var sqlFilterExpType = BuildSqlFilterExpType(request);

//            var connectionString = _context.Database.GetConnectionString();
//            await using var connection = new SqlConnection(connectionString);

//            var parameters = new DynamicParameters();
//            parameters.Add("@SqlFilterExp", sqlFilterExp);
//            parameters.Add("@SqlFilterExpOrderBy", sqlFilterExpOrderBy);
//            parameters.Add("@SqlFilterExpType", sqlFilterExpType);

//            var result = await connection.QueryAsync<AccountStatementModelResponse>(
//                "sp_6_56_GetAccountStatementType",
//                parameters,
//                commandType: CommandType.StoredProcedure,
//                commandTimeout: 120
//            );

//            return result.ToList();
//        }

//        // ── Cash & Bank balance (opening / closing) ───────────────────────────
//        public async Task<List<CashBankBalanceModelResponse>>
//            GetCashAndBankBalanceOpeningClosingAsync(AccountStatementRequest request)
//        {
//            // Both date-conversion tasks can run in parallel
//            var todayTask = BuildSqlFilterExpTodayAsync(request);
//            var previousTask = BuildSqlFilterExpPreviousAsync(request);
//            await Task.WhenAll(todayTask, previousTask);

//            var sqlFilterExpToday = await todayTask;
//            var sqlFilterExpPrevious = await previousTask;

//            var connectionString = _context.Database.GetConnectionString();
//            await using var connection = new SqlConnection(connectionString);

//            var parameters = new DynamicParameters();
//            parameters.Add("@SqlFilterExpToday", sqlFilterExpToday);
//            parameters.Add("@SqlFilterExpPrevious", sqlFilterExpPrevious);

//            var result = await connection.QueryAsync<CashBankBalanceModelResponse>(
//                "sp_6_56_GetCashAndBankBalanceBankOpeningClosing",
//                parameters,
//                commandType: CommandType.StoredProcedure,
//                commandTimeout: 120
//            );

//            return result.ToList();
//        }
//    }
//}

