namespace NexgenCosysReport.Dtos.RequestDtos.MemberAccount.InterestPayableReport
{

    public class SavingsAccountMaturityRequestDto
    {
        public string FromDateBs { get; set; } = string.Empty;
        public string ToDateBs { get; set; } = string.Empty;
        public string BranchIds { get; set; } = "-1";
        public string BranchName { get; set; } = "All Branches";
        public long DepositTypeId { get; set; } = -1;
        public string OrderBy { get; set; } = "Member Id";
        public bool VisualReport { get; set; } = false;
    }

    public class SavingsAccountMaturityRowDto
    {
        public string? DepositTypeName { get; set; }
        public string? MemberId { get; set; }
        public string? MemberName { get; set; }
        public string? AccountNo { get; set; }
        public string? AccountOpenDate { get; set; }
        public string? AccountOpenDateBs { get; set; }
        public string? MaturityDate { get; set; }
        public string? MaturityDateBs { get; set; }
        public decimal? DepositAmount { get; set; }
        public decimal? InterestRate { get; set; }
        public decimal? MaturityAmount { get; set; }
        public string? Remarks { get; set; }
    }

    public class SavingsAccountMaturityData
    {
        public List<SavingsAccountMaturityRowDto> Rows { get; set; } = new();
        public int TotalRecords { get; set; }
        public decimal TotalDepositAmount { get; set; }
        public decimal TotalMaturityAmount { get; set; }
        public string? FromDateBs { get; set; }
        public string? ToDateBs { get; set; }
        public string? BranchNames { get; set; }
        public string? OrderBy { get; set; }
        public string? DepositTypeName { get; set; }
        public int TotalDepositTypes { get; set; }
    }
    public class SavingsAccountMaturityReqResponse
    {
    }
}
