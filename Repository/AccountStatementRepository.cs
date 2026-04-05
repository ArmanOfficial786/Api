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

        public AccountStatementRepository(AppDbContext context)
        {
            _context = context;
        }

        // ══════════════════════════════════════════════════════════════
        // @SqlFilterExp
        // Appended inside SP WHERE vp.IsActive=1 + @SqlFilterExp
        // Uses v.VoucherOn (NOT v.VoucherDate)
        // ══════════════════════════════════════════════════════════════
        private string BuildSqlFilterExp(AccountStatementRequest request)
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
        public async Task<List<AccountStatementModel>>
            GetAccountStatementTypeAsync(AccountStatementRequest request)
        {
            var sqlFilterExp = BuildSqlFilterExp(request);
            var sqlFilterExpOrderBy = BuildSqlFilterExpOrderBy(request);
            var sqlFilterExpType = BuildSqlFilterExpType(request);

            var connectionString = _context.Database.GetConnectionString();
            await using var connection = new SqlConnection(connectionString);

            var parameters = new DynamicParameters();
            parameters.Add("@SqlFilterExp", sqlFilterExp);
            parameters.Add("@SqlFilterExpOrderBy", sqlFilterExpOrderBy);
            parameters.Add("@SqlFilterExpType", sqlFilterExpType);

            var result = await connection.QueryAsync<AccountStatementModel>(
                "sp_6_56_GetAccountStatementType",
                parameters,
                commandType: CommandType.StoredProcedure,
                commandTimeout: 120
            );

            return result.ToList();
        }

        // ── Cash & Bank balance (opening / closing) ───────────────────
        public async Task<List<CashBankBalanceModel>>
            GetCashAndBankBalanceOpeningClosingAsync(AccountStatementRequest request)
        {
            var sqlFilterExpToday = BuildSqlFilterExpToday(request);
            var sqlFilterExpPrevious = BuildSqlFilterExpPrevious(request);

            var connectionString = _context.Database.GetConnectionString();
            await using var connection = new SqlConnection(connectionString);

            var parameters = new DynamicParameters();
            parameters.Add("@SqlFilterExpToday", sqlFilterExpToday);
            parameters.Add("@SqlFilterExpPrevious", sqlFilterExpPrevious);

            var result = await connection.QueryAsync<CashBankBalanceModel>(
                "sp_6_56_GetCashAndBankBalanceBankOpeningClosing",
                parameters,
                commandType: CommandType.StoredProcedure,
                commandTimeout: 120
            );

            return result.ToList();
        }
    }
}