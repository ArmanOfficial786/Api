// Dtos/RequestDtos/Loan/OtherReports/LoanDueInstallmentRequestDto.cs
namespace NexgenCosysReport.Dtos.RequestDtos.Loan.OtherReports
{
    public class LoanDueInstallmentRequestDto
    {
        // From/To dates in BS format (required for date range filter)
        public string? FromDateBs { get; set; }
        public string? ToDateBs { get; set; }

        // Member ID (human-readable code, e.g. "M-001") - optional
        public string? MemberId { get; set; }

        // Payment Duration Type filter (-1 = all)
        public int LmtPaymentDurationTypeId { get; set; } = -1;

        // Comma-separated office ids from the checkbox list ("-1" or empty = all offices)
        public string? BranchIds { get; set; }

        // Member Group filter (empty/-1 = all groups)
        public string? MemberGroupId { get; set; }

        // Order By column name
        public string OrderBy { get; set; } = "-1";

        // Visual report flag
        public bool VisualReport { get; set; } = false;
    }

    // Columns match sp_7_16_LoanDueInstallmentReport's output SELECT list
    public class LoanDueInstallmentRowDto
    {
        public long? LmtLoanIssueId { get; set; }
        public string? MemberId { get; set; }
        public string? FullName { get; set; }
        public string? LoanAccountNo { get; set; }
        public decimal? LoanIssueAmount { get; set; }
        public string? LoanTypeName { get; set; }
        public string? PaymentDurationType { get; set; }
        public decimal? PrincipleAmount { get; set; }
        public decimal? InterestAmount { get; set; }
        public decimal? InstallmentAmount { get; set; }
        public decimal? BalanceAmount { get; set; }
        public DateTime? DateOnAD { get; set; }
        public string? DateOnBS { get; set; }
        public string? TemporaryAddressDetail { get; set; }
        public string? MobileNo { get; set; }
    }

    public class LoanDueInstallmentData
    {
        public List<LoanDueInstallmentRowDto> Rows { get; set; } = [];
        public int TotalRecords { get; set; }
        public decimal TotalLoanIssueAmount { get; set; }
        public decimal TotalPrincipleAmount { get; set; }
        public decimal TotalInterestAmount { get; set; }
        public decimal TotalInstallmentAmount { get; set; }
        public decimal TotalBalanceAmount { get; set; }
        public string? FromDateBs { get; set; }
        public string? ToDateBs { get; set; }
        public string? BranchNames { get; set; }
        public string? MemberName { get; set; }
        public string? MemberGroupName { get; set; }
        public string? PaymentDurationTypeName { get; set; }
        public string? OrderBy { get; set; }
    }
}