namespace NexgenCosysReport.Dtos.RequestDtos.Account
{
    public class CashFlowRequest
    {
        public string FromDate { get; set; } = string.Empty;
        public string ToDate { get; set; } = string.Empty;
        public string? BranchId { get; set; }
        public string BranchName { get; set; } = "All";
        public string OrderBy { get; set; } = "Voucher Date";
        public bool SameCompanyName { get; set; } = true;
        public bool VisualReport { get; set; } = false;
    }

    // Row DTO – matches SP result columns (VoucherDate is a string, e.g., "2081/04/32")
    public class CashFlowSpDto
    {
        public string? VoucherDate { get; set; }   // Changed from DateTime? to string
        public string? VoucherNo { get; set; }
        public string? Narration { get; set; }
        public decimal Amount { get; set; }
        public string? Type { get; set; }          // optional, not used
    }

    public class CashFlowData
    {
        public List<CashFlowSpDto> InflowRows { get; set; } = new();
        public List<CashFlowSpDto> OutflowRows { get; set; } = new();
        public decimal TotalInflow { get; set; }
        public decimal TotalOutflow { get; set; }
        public decimal OpeningCashBalance { get; set; }
        public decimal ClosingCashBalance => OpeningCashBalance + TotalInflow - TotalOutflow;
        public decimal NetCashFlow => TotalInflow - TotalOutflow;
    }

    public class CashFlowReqResponse
    {
    }
}
