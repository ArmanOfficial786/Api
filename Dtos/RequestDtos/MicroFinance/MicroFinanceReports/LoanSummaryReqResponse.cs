namespace NexgenCosysReport.Dtos.RequestDtos.MicroFinance.MicroFinanceReports
{
    public class LoanSummaryRequestDto
    {
        public string TillDateBs { get; set; } = string.Empty;
        public string? BranchIds { get; set; }
        public string OrderBy { get; set; } = string.Empty;
        public bool VisualReport { get; set; } = false;
    }

    public class LoanSummaryRowDto
    {
        public string? MemberId { get; set; }
        public string? MemberIdFirst { get; set; }
        public decimal? MemberIdLast { get; set; }
        public string? FullName { get; set; }
        public string? LoanAccountNo { get; set; }
        public string? LoanTypeName { get; set; }
        public decimal? DisburseAmount { get; set; }
        public decimal? OpeningDisburseAmount { get; set; }
        public decimal? Repaid { get; set; }
        public decimal? BalanceAmount { get; set; }
        public decimal? OpeningPaid { get; set; }
        public decimal? OpeningBalance { get; set; }
        public decimal? ClosingBalance { get; set; }
        public DateTime? TransactionOn { get; set; }
        public DateTime? LoanIssueOn { get; set; }
        public string? CollectionCenterName { get; set; }
        public string? GroupName { get; set; }
        public string? OfficeName { get; set; }
    }

    public class LoanSummaryData
    {
        public List<LoanSummaryRowDto> Rows { get; set; } = [];
        public int TotalRecords { get; set; }
        public decimal TotalDisburseAmount { get; set; }
        public decimal TotalRepaid { get; set; }
        public decimal TotalBalanceAmount { get; set; }
        public string? TillDateBs { get; set; }
        public string? TillDateAd { get; set; }
        public string? BranchName { get; set; }
        public string? OrderBy { get; set; }
    }
}