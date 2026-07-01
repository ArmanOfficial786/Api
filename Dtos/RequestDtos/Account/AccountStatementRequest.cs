namespace NexgenCosysReport.Dtos.RequestDtos.Account
{
    // -- Request ---------------------------------------------------
    public class AccountStatementRequest
    {

        public string? FromDate { get; set; } = string.Empty;
        public string? ToDate { get; set; } = string.Empty;
        public string? BranchSelected { get; set; }
        public string? BranchName { get; set; }
        public bool SameCompanyName { get; set; }
        public string? ReportType { get; set; }
        public string? TransactionType { get; set; }
        public string? OrderBy { get; set; }
        public bool VisualReport { get; set; } = false;
    }

    public class AccountStatementModelResponse
    {
        public string? LedgerNo { get; set; }
        public string? LedgerHead { get; set; }
        public string? MainLedger { get; set; }
        public string? SubLedger { get; set; }
        public string? SubLedger1 { get; set; }
        public decimal DebitAmount { get; set; }
        public decimal CreditAmount { get; set; }
        public decimal Balance { get; set; }
    }


    public class CashBankBalanceModelResponse
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