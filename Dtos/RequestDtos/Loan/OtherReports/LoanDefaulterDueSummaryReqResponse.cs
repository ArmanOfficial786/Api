namespace NexgenCosysReport.Dtos.RequestDtos.Loan.OtherReports
{
    public class LoanDefaulterDueSummaryRequestDto
    {
        public string TillDate { get; set; } = string.Empty;
        public string? BranchIds { get; set; }
        public string? CollectionCenterId { get; set; } = "-1";
        public bool EnableCollectionCenter { get; set; } = false;
        public string? CollectorId { get; set; } = "-1";
        public string ReportType { get; set; } = "LDR";
        public string OrderBy { get; set; } = "-1";
        public bool VisualReport { get; set; } = false;
    }

    public class LoanDefaulterDueSummaryRowDto
    {
        public long? LmtLoanIssueId { get; set; }
        public string? MemberId { get; set; }
        public string? MemberIdFirst { get; set; }
        public decimal? MemberIdLast { get; set; }
        public string? FullName { get; set; }
        public string? LoanAccountNo { get; set; }
        public decimal? LoanIssueAmount { get; set; }
        public string? LoanTypeName { get; set; }
        public decimal? InstallamentAmount { get; set; }
        public decimal? Interest { get; set; }
        public decimal? PrincipleAmount { get; set; }
        public string? DateOnBS { get; set; }
        public DateTime? DateOn { get; set; }
        public string? MobileNo { get; set; }
        public string? TemporaryAddressDetail { get; set; }
        public string? CollectionCenterName { get; set; }
        // ADDED — matches the SP's new COUNT(*) AS InstallmentCount column.
        // Only sp_7_16_LoanDefaulterDueSummary (LDR) returns this now; the
        // TobePaid SP (LDTPR) was left unchanged, so this will be null there.
        public int? InstallmentCount { get; set; }
    }

    public class LoanDefaulterDueSummaryData
    {
        public List<LoanDefaulterDueSummaryRowDto> Rows { get; set; } = [];
        public int TotalRecords { get; set; }
        public decimal TotalLoanIssueAmount { get; set; }
        public decimal TotalInterest { get; set; }
        public decimal TotalPrincipleAmount { get; set; }
        public decimal TotalInstallmentAmount { get; set; }
        public string? TillDate { get; set; }
        public string? BranchName { get; set; }
        public string? CollectionCenterName { get; set; }
        public string? CollectorName { get; set; }
        public string? ReportType { get; set; }
        public string? OrderBy { get; set; }
    }
}