namespace NexgenCosysReport.Dtos.RequestDtos.MemberAccount.InterestPayableReport
{

    public class PayableInterestTransferredRequestDto
    {
        public string FromDateBs { get; set; } = string.Empty;
        public string ToDateBs { get; set; } = string.Empty;
        public string BranchIds { get; set; } = "-1";
        public string OrderBy { get; set; } = "Member Id";
        public string BranchName { get; set; } = "All Branches";
        public bool VisualReport { get; set; } = false;
    }

    public class PayableInterestTransferredRowDto
    {
        public string? DepositTypeName { get; set; }
        public string? MemberId { get; set; }
        public string? MemberName { get; set; }
        public string? AccountNo { get; set; }
        public string? InterestDate { get; set; }
        public string? InterestDateBs { get; set; }
        public decimal? InterestRate { get; set; }
        public decimal? Interest { get; set; }
        public decimal? Tax { get; set; }
        public decimal? NetBalance { get; set; }
    }


    public class PayableInterestTransferredData
    {
        public List<PayableInterestTransferredRowDto> Rows { get; set; } = new();
        public int TotalRecords { get; set; }
        public decimal TotalInterest { get; set; }
        public decimal TotalTax { get; set; }
        public decimal TotalNetBalance { get; set; }
        public string? FromDateBs { get; set; }
        public string? ToDateBs { get; set; }
        public string? BranchNames { get; set; }
        public string? OrderBy { get; set; }
        public int TotalDepositTypes { get; set; }
    }
    public class PayableInterestTransferredReqResponse
    {
    }
}
