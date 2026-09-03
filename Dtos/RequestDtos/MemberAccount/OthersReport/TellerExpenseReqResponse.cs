namespace NexgenCosysReport.Dtos.RequestDtos.MemberAccount.OthersReport
{
    public class TellerWiseExpenseRequestDto
    {
        public string FromDateBs { get; set; } = string.Empty;
        public string ToDateBs { get; set; } = string.Empty;
        public long? TellerId { get; set; }
        public string OrderBy { get; set; } = "Account No";
        public bool VisualReport { get; set; } = false;
    }
    public class TellerWiseExpenseRowDto
    {
        public string? Date { get; set; }
        public string? MemberId { get; set; }
        public string? MemberName { get; set; }
        public string? AccountNo { get; set; }
        public string? Type { get; set; }
        public decimal? SavingWithdrawlAmount { get; set; }
        public decimal? ShareReturnAmount { get; set; }
        public decimal? LoanIssueAmount { get; set; }
        public decimal? MiscellaneousAmount { get; set; }
        public decimal? RowTotal { get; set; }
        public string? BillNo { get; set; }
        public string? TellerName { get; set; }
        public string? Details { get; set; }
    }
    public class TellerWiseExpenseData
    {
        public List<TellerWiseExpenseRowDto> Rows { get; set; } = new();
        public int TotalRecords { get; set; }
        public decimal TotalSavingWithdrawlAmount { get; set; }
        public decimal TotalShareReturnAmount { get; set; }
        public decimal TotalLoanIssueAmount { get; set; }
        public decimal TotalMiscellaneousAmount { get; set; }
        public decimal TotalAmount { get; set; }
        public string? FromDateBs { get; set; }
        public string? ToDateBs { get; set; }
        public long? TellerId { get; set; }
        public string? TellerName { get; set; }
        public string? OrderBy { get; set; }
    }
    public class TellerExpenseReqResponse
    {
    }
}
