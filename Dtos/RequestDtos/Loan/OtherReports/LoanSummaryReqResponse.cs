// Dtos/RequestDtos/Loan/OtherReports/LoanSummaryRequestDto.cs
namespace NexgenCosysReport.Dtos.RequestDtos.Loan.OtherReports
{
    public class LoanSummaryRequestDto
    {
        // Loan Type filter (required, > 0)
        public long LoanTypeId { get; set; } = -1;

        // Comma-separated office ids from the checkbox list ("-1" or empty = all offices)
        public string? BranchIds { get; set; }

        // Member Group filter (empty/-1 = all groups)
        public string? MemberGroupId { get; set; }

        // "MemberId" | "FullName" | "LoanAccountNo" | "LoanTypeName" | "LoanIssueAmount" | "LoanIssueDate" | "Period" | "InterestRate"
        public string OrderBy { get; set; } = "-1";

        // Visual report flag
        public bool VisualReport { get; set; } = false;
    }

    // Columns match sp_7_16_LoanSummaryReport's output SELECT list
    public class LoanSummaryRowDto
    {
        public string? MemberId { get; set; }
        public string? MemberIdFirst { get; set; }
        public decimal? MemberIdLast { get; set; }
        public string? FullName { get; set; }
        public string? LoanAccountNo { get; set; }
        public string? LoanTypeName { get; set; }
        public decimal? LoanIssueAmount { get; set; }
        public string? LoanIssueDate { get; set; }
        public string? Period { get; set; }
        public string? InterestRate { get; set; }
    }

    public class LoanSummaryData
    {
        public List<LoanSummaryRowDto> Rows { get; set; } = [];
        public int TotalRecords { get; set; }
        public decimal TotalLoanIssueAmount { get; set; }
        public string? BranchName { get; set; }
        public string? LoanTypeName { get; set; }
        public string? MemberGroupName { get; set; }
        public string? OrderBy { get; set; }
    }
}