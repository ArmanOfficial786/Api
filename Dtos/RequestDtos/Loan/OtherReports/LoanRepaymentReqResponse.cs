// Dtos/RequestDtos/Loan/OtherReports/LoanRepaymentRequestDto.cs
namespace NexgenCosysReport.Dtos.RequestDtos.Loan.OtherReports
{
    public class LoanRepaymentRequestDto
    {
        // Member ID (human-readable code, e.g. "M-001") - optional
        public string? MemberId { get; set; }

        // Comma-separated office ids from the checkbox list ("-1" or empty = all offices)
        public string? BranchIds { get; set; }

        // Visual report flag
        public bool VisualReport { get; set; } = false;
    }

    // Columns match sp_7_16_LoanRepaymentAccountDetails's output SELECT list
    public class LoanRepaymentRowDto
    {
        public string? MemberId { get; set; }
        public string? FullName { get; set; }
        public string? ContactAddress { get; set; }
        public string? ContactNo { get; set; }
        public decimal? LoanIssueAmount { get; set; }
        public decimal? PrincipleBalance { get; set; }
        public decimal? InterestRate { get; set; }
        public string? LoanAccountNo { get; set; }
        public string? LoanIssueOnBs { get; set; }
        public string? MaturityOnBs { get; set; }
        public string? LoanType { get; set; }
        public string? Period { get; set; }
        public string? AccountNo { get; set; }
        public string? AccountStatus { get; set; }
    }

    public class LoanRepaymentData
    {
        public List<LoanRepaymentRowDto> Rows { get; set; } = [];
        public int TotalRecords { get; set; }
        public decimal TotalLoanIssueAmount { get; set; }
        public decimal TotalPrincipleBalance { get; set; }
        public string? BranchName { get; set; }
        public string? MemberName { get; set; }
        public string? MemberId { get; set; }
    }
}