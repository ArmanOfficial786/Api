namespace NexgenCosysReport.Dtos.RequestDtos.Account
{
    public class PLAccountRequest
    {
        public string FromDate { get; set; } = string.Empty;           // Nepali date "yyyy/MM/dd"
        public string ToDate { get; set; } = string.Empty;
        public string? BranchIds { get; set; }                         // comma-separated, e.g. "2,5"
        public string BranchName { get; set; } = "All";
        public string ReportType { get; set; } = "Summary";            // Summary, SubLedger, Detail
        public string OrderBy { get; set; } = "Ledger Name";
        public string DisplayType { get; set; } = "Horizontal";       // Horizontal or Vertical
        public bool IsNepaliReport { get; set; } = false;
        public bool SameCompanyName { get; set; } = true;
        public bool VisualReport { get; set; } = false;
    }

    public class PLAccountRowDto
    {
        public string? LedgerHead { get; set; }
        public string? MainLedger { get; set; }
        public string? SubLedger { get; set; }
        public string? SubLedger1 { get; set; }
        public string? SubLedger2 { get; set; }
        public decimal DebitAmount { get; set; }
        public decimal CreditAmount { get; set; }
        public decimal Balance { get; set; }
        // For grouping/indentation
        public int? Level { get; set; }
    }

    public class PLAccountHorizontalData
    {
        public List<PLAccountRowDto> IncomeRows { get; set; } = new();
        public List<PLAccountRowDto> ExpenseRows { get; set; } = new();
        public decimal TotalIncome { get; set; }
        public decimal TotalExpense { get; set; }
        public decimal NetProfit => TotalIncome - TotalExpense;
    }

    public class PLAccountVerticalData
    {
        public List<PLAccountRowDto> Rows { get; set; } = new();
        public decimal TotalIncome { get; set; }
        public decimal TotalExpense { get; set; }
        public decimal NetProfit { get; set; }
        // Rows might be grouped by LedgerHead (Income / Expense)
    }

    public class PLAccountReqResponse
    {
    }
}
