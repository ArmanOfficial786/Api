namespace NexgenCosysReport.Dtos.RequestDtos.MemberAccount.OthersReport
{

    public class SavingIssueRequestDto
    {
        public string FromDateBs { get; set; } = string.Empty;
        public string ToDateBs { get; set; } = string.Empty;
        public string BranchIds { get; set; } = "-1";
        public string OrderBy { get; set; } = "Member Id";
        public string BranchName { get; set; } = "All Branches";
        public bool VisualReport { get; set; } = false;
        public long? DepositTypeId { get; set; } = -1;
        public long? CollectorId { get; set; } = -1;
        public long? MemberGroupId { get; set; } = -1;
        public string ReportMode { get; set; } = "DateWise"; // DateWise, DepositTypeWise
    }

    public class SavingIssueRowDto
    {
        public string? MemberId { get; set; }
        public string? MemberName { get; set; }
        public string? AccountNo { get; set; }
        public string? AccountOpenOnBs { get; set; }
        public string? AccountOpenDate { get; set; }
        public string? DepositTypeName { get; set; }
        public decimal? InterestRate { get; set; }
        public string? SmsCategory { get; set; }
        public string? CollectorName { get; set; }
        public string? MemberGroupName { get; set; }
        public decimal? OpeningBalance { get; set; }
        public string? Operator { get; set; }
    }

    public class SavingIssueData
    {
        public List<SavingIssueRowDto> Rows { get; set; } = new();
        public int TotalRecords { get; set; }
        public decimal TotalOpeningBalance { get; set; }
        public string? FromDateBs { get; set; }
        public string? ToDateBs { get; set; }
        public string? BranchNames { get; set; }
        public string? OrderBy { get; set; }
        public string? ReportMode { get; set; }
        public string? DepositTypeName { get; set; }
        public string? CollectorName { get; set; }
        public string? MemberGroupName { get; set; }
        public int TotalAccounts { get; set; }
    }
    public class SavingIssueReqResponse
    {
    }
}
