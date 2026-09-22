namespace NexgenCosysReport.Dtos.RequestDtos.Loan.OtherReports
{
    public class MaturedLoanRequestDto
    {
        public string? MemberId { get; set; }
        public string? FromDateBs { get; set; }
        public string? ToDateBs { get; set; }
        public string? BranchIds { get; set; }
        public long MemberGroupId { get; set; } = -1;
        public string OrderBy { get; set; } = "MemberId";
        public bool VisualReport { get; set; } = false;
    }


    public class MaturedLoanRowDto
    {
        public string? MemberId { get; set; }
        public string? MemberIdFirst { get; set; }
        public decimal? MemberIdLast { get; set; }
        public string? FullName { get; set; }
        public string? MobileNo { get; set; }
        public string? Addess { get; set; }
        public string? LoanAccountNo { get; set; }
        public string? LoanTypeName { get; set; }
        public string? LoanIssueDate { get; set; }
        public string? MaturityDate { get; set; }
        public decimal? InterestRate { get; set; }
        public decimal? LoanIssueAmount { get; set; }
        public decimal? PrinciplePaid { get; set; }
        public decimal? PrincipleDue { get; set; }
        public decimal? InterestDue { get; set; }
        public decimal? TotalDueAmount { get; set; }

    }

    public class MaturedLoanData
    {
        public List<MaturedLoanRowDto> Rows { get; set; } = [];
        public int TotalRecords { get; set; }
        public int TotalMembers { get; set; }
        public decimal TotalLoanIssueAmount { get; set; }
        public decimal TotalPrincipleDue { get; set; }
        public decimal TotalInterestDue { get; set; }
        public decimal TotalDueAmount { get; set; }
        public string? FromDateBs { get; set; }
        public string? ToDateBs { get; set; }
        public string? BranchName { get; set; }
        public string? MemberName { get; set; }
        public string? MemberGroupName { get; set; }
        public string? OrderBy { get; set; }
    }
}