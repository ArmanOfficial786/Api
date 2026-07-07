namespace NexgenCosysReport.Dtos.RequestDtos.Account
{
    public class SummaryTrialBalanceRequest
    {
        public string FromDate { get; set; } = string.Empty;   // Nepali date "yyyy/MM/dd"
        public string ToDate { get; set; } = string.Empty;
        public string? BranchIds { get; set; }                 // comma-separated
        public string BranchName { get; set; } = "All";
        public string OrderBy { get; set; } = "Ledger Name";   // Ledger Name, Debit Amount, Credit Amount, Balance
        public bool WithClosingBalance { get; set; } = true;   // true = WithClosingBalance, false = WithoutClosingBalance
        public string ReportType { get; set; } = "Detail";     // Detail or Summary (affects grouping)
        public bool SameCompanyName { get; set; } = true;
        public bool VisualReport { get; set; } = false;
        public bool IsSubLedger { get; set; } = false;         // true for SubLedger detail report
    }

    public class SummaryTrialBalanceRowDto
    {
        public string? LedgerHead { get; set; }
        public string? MainLedger { get; set; }
        public string? SubLedger { get; set; }
        public string? SubLedger1 { get; set; }
        public string? SubLedger2 { get; set; }
        public decimal DebitAmount { get; set; }
        public decimal CreditAmount { get; set; }
        public decimal Balance { get; set; }
        // For SubLedger report we may need additional fields
    }

    public class SummaryTrailBalanceReqResponse
    {
    }
}
