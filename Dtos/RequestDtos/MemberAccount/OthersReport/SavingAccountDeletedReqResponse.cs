namespace NexgenCosysReport.Dtos.RequestDtos.MemberAccount.OthersReport
{

    public class SavingAccountDeletedRequestDto
    {
        public string FromDateBs { get; set; } = string.Empty;
        public string ToDateBs { get; set; } = string.Empty;
        public string BranchIds { get; set; } = "-1";
        public string OrderBy { get; set; } = "Member Id";
        public string BranchName { get; set; } = "All Branches";
        public bool VisualReport { get; set; } = false;
    }
    public class SavingAccountDeletedRowDto
    {
        public string? MemberId { get; set; }
        public string? MemberName { get; set; }
        public string? AccountNo { get; set; }
        public string? AccountOpenOnBs { get; set; }
        public string? AccountOpenDate { get; set; }
        public string? DepositTypeName { get; set; }
        public decimal? InterestRate { get; set; }
        public decimal? AccountBalance { get; set; }
        public string? DeletedDate { get; set; }
        public string? DeletedBy { get; set; }
        public string? Reason { get; set; }
    }

    public class SavingAccountDeletedData
    {
        public List<SavingAccountDeletedRowDto> Rows { get; set; } = new();
        public int TotalRecords { get; set; }
        public decimal TotalBalance { get; set; }
        public string? FromDateBs { get; set; }
        public string? ToDateBs { get; set; }
        public string? BranchNames { get; set; }
        public string? OrderBy { get; set; }
        public int TotalDeletedAccounts { get; set; }
    }

    public class SavingAccountDeletedReqResponse
    {
    }
}
