namespace NexgenCosysReport.Dtos.RequestDtos.Loan.OtherReports
{
    public class LoanReScheduleRequestDto
    {
        public string? FromDateBs { get; set; }
        public string? ToDateBs { get; set; }
        public string? BranchIds { get; set; }
        public long MemberGroupId { get; set; } = -1;
        public string OrderBy { get; set; } = "-1";
        public bool VisualReport { get; set; } = false;
    }
    public class LoanReScheduleRowDto
    {
        public string? MemberId { get; set; }
        public string? MemberIdFirst { get; set; }
        public decimal? MemberIdLast { get; set; }
        public string? FullName { get; set; }
        public string? LoanAccountNo { get; set; }
        public string? LoanTypeName { get; set; }
        public string? LoanIssueDate { get; set; }
        public decimal? LoanIssueAmount { get; set; }
        public string? LoanRescheduleDate { get; set; }
        public decimal? LoanRescheduleAmount { get; set; }
        public string? Duration { get; set; }
        public string? InterestRate { get; set; }
        public string? LoanType { get; set; }
        public string? PaymentType { get; set; }
        public string? MaturityDate { get; set; }
    }

    public class LoanReScheduleData
    {
        public List<LoanReScheduleRowDto> Rows { get; set; } = [];
        public int TotalRecords { get; set; }
        public decimal TotalLoanIssueAmount { get; set; }
        public decimal TotalLoanRescheduleAmount { get; set; }
        public string? FromDateBs { get; set; }
        public string? ToDateBs { get; set; }
        public string? BranchName { get; set; }
        public string? MemberGroupName { get; set; }
        public string? OrderBy { get; set; }
    }
}