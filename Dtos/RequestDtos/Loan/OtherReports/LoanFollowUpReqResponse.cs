namespace NexgenCosysReport.Dtos.RequestDtos.Loan.OtherReports
{
    public class LoanFollowUpRequestDto
    {
        public string? MemberId { get; set; }
        public string? FromDateBs { get; set; }
        public string? ToDateBs { get; set; }
        public string? BranchId { get; set; }
        public bool VisualReport { get; set; }
        public string OrderBy { get; set; }
    }


    public class LoanFollowUpRowDto
    {
        public string? MemberId { get; set; }
        public string? MemberName { get; set; }
        public string? LoanAccountNo { get; set; }
        public string? FollowUpDateOnBs { get; set; }
        public string? FollowUpPerson { get; set; }
        public string? Description { get; set; }
        public string? FollowUpBy { get; set; }
        public string? CreatedOnBs { get; set; }
    }

    public class LoanFollowUpData
    {
        public List<LoanFollowUpRowDto> Rows { get; set; } = new();
        public int TotalRecords { get; set; }
        public string? FromDateBs { get; set; }
        public string? ToDateBs { get; set; }
        public string? BranchName { get; set; }
        public string? MemberName { get; set; }
        public string? OrderBy { get; set; }
    }
}