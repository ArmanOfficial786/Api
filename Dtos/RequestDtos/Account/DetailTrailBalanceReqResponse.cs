namespace NexgenCosysReport.Dtos.RequestDtos.Account
{
    public class DetailTrialBalanceRequest
    {
        public string FromDate { get; set; } = string.Empty;   // Nepali "yyyy/MM/dd"
        public string ToDate { get; set; } = string.Empty;
        public string? BranchIds { get; set; }                 // comma‑separated, e.g. "2,5"
        public string BranchName { get; set; } = "All";
        public string OrderBy { get; set; } = "Sub Ledger";    // Sub Ledger, Debit Amount, Credit Amount, Balance
        public bool SameCompanyName { get; set; } = true;
        public bool VisualReport { get; set; } = false;
    }

    public class DetailTrialBalanceRowDto
    {
        public string? LedgerHead { get; set; }
        public string? MainLedger { get; set; }
        public string? SubLedger { get; set; }
        public string? SubLedger1 { get; set; }
        public string? SubLedger2 { get; set; }
        public decimal DebitAmount { get; set; }
        public decimal CreditAmount { get; set; }
        public decimal Balance { get; set; }
    }

    public class DetailTrialBalanceData
    {
        public List<DetailTrialBalanceRowDto> Rows { get; set; } = new();
        public decimal TotalAssetExpenses { get; set; }
        public decimal TotalLiabilitiesIncome { get; set; }
        public decimal TotalDebit => Rows.Sum(r => r.DebitAmount);
        public decimal TotalCredit => Rows.Sum(r => r.CreditAmount);
    }
    public class DetailTrailBalanceReqResponse
    {
    }
}
