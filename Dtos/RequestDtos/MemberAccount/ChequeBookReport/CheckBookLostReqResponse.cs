namespace NexgenCosysReport.Dtos.RequestDtos.MemberAccount.ChequeBookReport
{
    public class ChequeBookLostRequestDto
    {
        public long MemberId { get; set; } = -1;
        public string MemberIdText { get; set; } = string.Empty;
        public string MemberName { get; set; } = string.Empty;
        public string FromDateBs { get; set; } = string.Empty;
        public string ToDateBs { get; set; } = string.Empty;
        public string BranchIds { get; set; } = "-1";
        public string BranchName { get; set; } = "All Branches";
        public string OrderBy { get; set; } = "Member Id";
        public string ReportView { get; set; } = "Date";
        public bool VisualReport { get; set; } = false;
    }

    public class ChequeBookLostRowDto
    {
        public string? MemberId { get; set; }
        public string? MemberName { get; set; }
        public string? AccountNo { get; set; }
        public string? ChequeIssueDate { get; set; }
        public string? ChequeIssueDateBs { get; set; }
        public long? ChequeNo { get; set; }
        public string? Status { get; set; }
        public string? Remarks { get; set; }
        public string? BranchName { get; set; }
        public string? LostDate { get; set; }
        public string? LostDateBs { get; set; }
        public string? Operator { get; set; }
        public string? LastModifiedOn { get; set; }
        public string? LastModifiedBy { get; set; }
    }

    public class ChequeBookLostData
    {
        public List<ChequeBookLostRowDto> Rows { get; set; } = new();
        public int TotalRecords { get; set; }
        public int TotalChequesLost { get; set; }
        public string? FromDateBs { get; set; }
        public string? ToDateBs { get; set; }
        public string? BranchNames { get; set; }
        public string? OrderBy { get; set; }
        public string? MemberId { get; set; }
        public string? MemberName { get; set; }
        public string? ReportView { get; set; }
    }
    public class CheckBookLostReqResponse
    {
    }
}
