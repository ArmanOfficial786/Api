namespace NexgenCosysReport.Dtos.RequestDtos.MicroFinance.MicroFinanceReports
{
    public class FundWithdrawlRequestDto
    {
        public string FromDateBs { get; set; } = string.Empty;
        public string ToDateBs { get; set; } = string.Empty;
        public string? BranchIds { get; set; }
        public string? OrderBy { get; set; } = "Name";
        public string ReportMode { get; set; } = "1";
        public bool VisualReport { get; set; } = false;
    }

    public class FundWithdrawlRowDto
    {
        public string? TypeName { get; set; }
        public string? CollectionCenterShortCode { get; set; }
        public string? MemberId { get; set; }
        public string? Name { get; set; }
        public string? PermanentAddessDetail { get; set; }
        public string? TransactionOnBs { get; set; }
        public decimal? CashWithdrawl { get; set; }
    }

    public class FundWithdrawlData
    {
        public List<FundWithdrawlRowDto> Rows { get; set; } = [];
        public int TotalRecords { get; set; }
        public decimal TotalCashWithdrawl { get; set; }
        public string? FromDateBs { get; set; }
        public string? ToDateBs { get; set; }
        public string? FromDateAd { get; set; }
        public string? ToDateAd { get; set; }
        public string? BranchName { get; set; }
        public string? OrderBy { get; set; }
        public string? ReportMode { get; set; }
        public string? ReportModeName { get; set; }
    }
}