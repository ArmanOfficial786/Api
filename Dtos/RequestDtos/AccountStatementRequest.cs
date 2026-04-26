using Microsoft.CodeAnalysis.Operations;

namespace JsSampleReport.Dtos.RequestDtos
{
    // ── Request ───────────────────────────────────────────────────
    public class AccountStatementRequest
    {
        // Dates in English format: "yyyy-MM-dd"
        // e.g. "2025-01-14"  (no Nepali conversion in this layer)
        public string? FromDate { get; set; } = string.Empty;
        public string? ToDate { get; set; } = string.Empty;
        public string? BranchSelected { get; set; }
        public List<long> BranchId { get; set; } = new();

        public string? BranchName { get; set; }

        public bool SameCompanyName { get; set; } = true; // default to true

        // Summary | SubLedger | Detail
        public string? ReportType { get; set; } 

        // All | Cash | Bank | CashBank | NonCash
        public string? TransactionType { get; set; } //= "All";

        // -1 | Ledger Name | Debit Amount | Credit Amount | Balance
        public string? OrderBy { get; set; } //= "-1";


       
    }

    // ── Main SP output — exact column names from sp_6_56_GetAccountStatementType ──
    // SP final SELECT returns:
    //   LedgerNo, LedgerHead, MainLedger, SubLedger, SubLedger1,
    //   DebitAmount, CreditAmount, Balance
    public class AccountStatementModel
    {
        public string? LedgerNo { get; set; }   // "80","90" or AcoLedgerHead.LedgerNo
        public string? LedgerHead { get; set; }   // ASSETS / LIABILITIES / INCOME / EXPENSES
        public string? MainLedger { get; set; }   // CASH BALANCE / BANK BALANCE / ledger name
        public string? SubLedger { get; set; }   // sub ledger name
        public string? SubLedger1 { get; set; }   // member reg no or deeper sub-ledger
        public decimal DebitAmount { get; set; }
        public decimal CreditAmount { get; set; }
        public decimal Balance { get; set; }   // Dr-Cr or Cr-Dr depending on LedgerHead
    }

    // ── Opening/Closing SP output — sp_6_56_GetCashAndBankBalanceBankOpeningClosing ──
    // One row returned containing both opening and closing in separate columns
    public class CashBankBalanceModel
    {
        public decimal OpeningCashBalance { get; set; }
        public decimal TodayCashDr { get; set; }
        public decimal TodayCashCr { get; set; }
        public decimal TodayCashBalance { get; set; }
        public decimal ClosingCashBalance { get; set; }
        public decimal OpeningBankBalance { get; set; }
        public decimal TodayBankDr { get; set; }
        public decimal TodayBankCr { get; set; }
        public decimal TodayBankBalance { get; set; }
        public decimal ClosingBankBalance { get; set; }
    }

    public class AccountStatementDtos
    {

    }
}