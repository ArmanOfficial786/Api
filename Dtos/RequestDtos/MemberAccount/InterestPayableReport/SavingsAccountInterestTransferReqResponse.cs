namespace NexgenCosysReport.Dtos.RequestDtos.MemberAccount.InterestPayableReport
{

    public class SavingsAccountInterestTransferRequestDto
    {
        public string FromDateBs { get; set; } = string.Empty;
        public string ToDateBs { get; set; } = string.Empty;
        public string BranchIds { get; set; } = "-1";
        public string BranchName { get; set; } = "All Branches";
        public long DepositTypeId { get; set; } = -1;
        public string OrderBy { get; set; } = "Member Id";
        public bool VisualReport { get; set; } = false;
    }

    public class SavingsAccountInterestTransferRowDto
    {
        public string? DepositTypeName { get; set; }
        public string? MemberId { get; set; }
        public string? MemberName { get; set; }
        public string? AccountNo { get; set; }
        public string? AccountOpenDate { get; set; }
        public string? AccountOpenDateBs { get; set; }
        public string? NextInterestDate { get; set; }
        public string? NextInterestDateBs { get; set; }
        public decimal? InterestRate { get; set; }
        public decimal? Balance { get; set; }
        public decimal? InterestAmount { get; set; }
        public string? Remarks { get; set; }
    }

    public class SavingsAccountInterestTransferData
    {
        public List<SavingsAccountInterestTransferRowDto> Rows { get; set; } = new();
        public int TotalRecords { get; set; }
        public decimal TotalBalance { get; set; }
        public decimal TotalInterestAmount { get; set; }
        public string? FromDateBs { get; set; }
        public string? ToDateBs { get; set; }
        public string? BranchNames { get; set; }
        public string? OrderBy { get; set; }
        public string? DepositTypeName { get; set; }
        public int TotalDepositTypes { get; set; }
    }
    public class SavingsAccountInterestTransferReqResponse
    {
    }
}
