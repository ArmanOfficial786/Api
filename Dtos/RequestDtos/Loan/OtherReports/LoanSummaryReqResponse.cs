namespace NexgenCosysReport.Dtos.RequestDtos.Loan.OtherReports
{
    public class LoanSummaryRequestDto
    {
        public long LoanTypeId { get; set; } = -1;
        public string? BranchIds { get; set; }
        public long MemberGroupId { get; set; } = -1;
        public string OrderBy { get; set; } = "-1";
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