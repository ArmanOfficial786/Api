namespace NexgenCosysReport.Dtos.RequestDtos.MemberAccount.OthersReport
{

    public class SavingAccountRenewedRequestDto
    {
        public string FromDateBs { get; set; } = string.Empty;
        public string ToDateBs { get; set; } = string.Empty;
        public string BranchIds { get; set; } = "-1";
        public string OrderBy { get; set; } = "Member Id";
        public string BranchName { get; set; } = "All Branches";
        public bool VisualReport { get; set; } = false;
        public long? MemberId { get; set; } = -1;
        public string ReportMode { get; set; } = "DateWise"; // DateWise, MemberWise
    }

    public class SavingAccountRenewedRowDto
    {
        public string? MemberId { get; set; }
        public string? MemberName { get; set; }
        public string? AccountNo { get; set; }
        public string? AccountOpenOnBs { get; set; }
        public string? AccountOpenDate { get; set; }
        public string? MaturityOnBs { get; set; }
        public string? RenewedDate { get; set; }
        public string? DepositTypeName { get; set; }
        public decimal? InterestRate { get; set; }
        public decimal? AccountBalance { get; set; }
        public string? Operator { get; set; }
        public string? Description { get; set; }
    }

    public class SavingAccountRenewedData
    {
        public List<SavingAccountRenewedRowDto> Rows { get; set; } = new();
        public int TotalRecords { get; set; }
        public decimal TotalBalance { get; set; }
        public string? FromDateBs { get; set; }
        public string? ToDateBs { get; set; }
        public string? BranchNames { get; set; }
        public string? OrderBy { get; set; }
        public string? ReportMode { get; set; }
        public string? SelectedMemberId { get; set; }
        public string? SelectedMemberName { get; set; }
        public int TotalRenewedAccounts { get; set; }
    }
    public class SavingAccountRenewedReqResponse
    {
    }
}
