namespace NexgenCosysReport.Dtos.RequestDtos.Account
{

    // Dtos/RequestDtos/AccountOperation/BalanceSheetRequest.cs
    namespace NexgenCosysReport.Dtos.RequestDtos.AccountOperation
    {
        public class BalanceSheetRequest
        {
            public string TillDate { get; set; } = string.Empty;        // "yyyy/MM/dd"
            public string? BranchIds { get; set; }                      // "2,5" or "-1"
            public string BranchName { get; set; } = "All";
            public string ReportType { get; set; } = "Summary";         // Summary, SubLedger, Detail
            public string OrderBy { get; set; } = "Ledger Name";
            public bool IncludePreviousYearBalance { get; set; } = false;
            public bool SameCompanyName { get; set; } = true;
            public bool VisualReport { get; set; } = false;
        }
    }

    public class BalanceSheetDto
    {
        public int? LedgerNo { get; set; }
        public string? LedgerHead { get; set; }
        public string? MainLedger { get; set; }
        public string? SubLedger { get; set; }
        public string? SubLedger1 { get; set; }
        public string? SubLedger2 { get; set; }
        public decimal DebitAmount { get; set; }
        public decimal CreditAmount { get; set; }
        public decimal CurrentAmount { get; set; }
        public decimal PreviousAmount { get; set; }
    }

    // For the overall response including metadata
    public class BalanceSheetReportData
    {
        public List<BalanceSheetDto> Rows { get; set; } = new();
        public decimal TotalDebit { get; set; }
        public decimal TotalCredit { get; set; }
        public DateTime FiscalYearFrom { get; set; }
        public DateTime FiscalYearTo { get; set; }
        public string FiscalYearLabel { get; set; } = "";
    }
    public class BalanceSheetReqResponse
    {
    }
}
