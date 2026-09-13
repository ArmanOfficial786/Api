// Dtos/RequestDtos/Loan/OtherReports/LoanDefaulterDueSummaryRequestDto.cs
namespace NexgenCosysReport.Dtos.RequestDtos.Loan.OtherReports
{
    public class LoanDefaulterDueSummaryRequestDto
    {
        // Till date in BS format (required)
        public string TillDate { get; set; } = string.Empty;

        // Comma-separated office ids from the checkbox list ("-1" or empty = all offices)
        public string? BranchIds { get; set; }

        // Collection Center filter (-1 = all)
        public string? CollectionCenterId { get; set; } = "-1";

        // Enable collection center grouping
        public bool EnableCollectionCenter { get; set; } = false;

        // Collector filter (-1 = all)
        public string? CollectorId { get; set; } = "-1";

        // Report type: "LDR" (Schedulewise Interest) or "LDTPR" (Till Date Interest)
        public string ReportType { get; set; } = "LDR";

        // Order By column name
        public string OrderBy { get; set; } = "-1";

        // Visual report flag
        public bool VisualReport { get; set; } = false;
    }

    // Columns match sp_7_16_LoanDefaulterDueSummaryTobePaid output SELECT list
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