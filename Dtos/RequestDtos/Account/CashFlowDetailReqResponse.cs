namespace NexgenCosysReport.Dtos.RequestDtos.Account
{
    public class CashFlowDetailsRequest
    {
        public string TillDate { get; set; } = string.Empty;   // Nepali "yyyy/MM/dd"
        public string? BranchIds { get; set; }                  // comma-separated, e.g. "2,5" or "-1"
        public string BranchName { get; set; } = "All";
        public bool SameCompanyName { get; set; } = true;
        public bool VisualReport { get; set; } = false;
    }

    public class CashFlowRowDto
    {
        public string? MainLedger { get; set; }
        public string? SubLedger { get; set; }
        public decimal Amount { get; set; }
        // For grouping
        public string? ActivityType { get; set; } // "Operating", "Investing", "Financing"
    }
    public class CashFlowDetailsData
    {
        public List<CashFlowRowDto> OperatingRows { get; set; } = new();
        public List<CashFlowRowDto> InvestingRows { get; set; } = new();
        public List<CashFlowRowDto> FinancingRows { get; set; } = new();
        public decimal NetOperating { get; set; }
        public decimal NetInvesting { get; set; }
        public decimal NetFinancing { get; set; }
        public decimal OpeningCashBalance { get; set; }
        public decimal ClosingCashBalance { get; set; }
        public decimal NetIncrease => NetOperating + NetInvesting + NetFinancing;
    }
    public class CashFlowDetailReqResponse
    {
    }
}
