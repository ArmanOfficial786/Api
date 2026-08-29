namespace NexgenCosysReport.Dtos.RequestDtos.MemberAccount.InterestExpenseReport
{

    public class InterestAndTaxDetailRequestDto
    {
        public string FromDateBs { get; set; } = string.Empty;
        public string ToDateBs { get; set; } = string.Empty;
        public string BranchIds { get; set; } = "-1";
        public string OrderBy { get; set; } = "Member Id";
        public string BranchName { get; set; } = "All Branches";
        public long MemberRegistrationId { get; set; } = -1;
        public string MemberId { get; set; } = string.Empty;
        public string MemberName { get; set; } = string.Empty;
        public bool VisualReport { get; set; } = false;
    }

    public class InterestAndTaxDetailRowDto
    {
        public string? DepositTypeName { get; set; }
        public string? Date { get; set; }
        public string? DateBs { get; set; }
        public string? MemberId { get; set; }
        public string? MemberName { get; set; }
        public string? AccountNo { get; set; }
        public string? Narration { get; set; }
        public decimal? Interest { get; set; }
        public decimal? Tax { get; set; }
        public decimal? PercentTax { get; set; }
        public decimal? NetAmount { get; set; }
    }

    public class InterestAndTaxDetailData
    {
        public List<InterestAndTaxDetailRowDto> Rows { get; set; } = new();
        public int TotalRecords { get; set; }
        public decimal TotalInterest { get; set; }
        public decimal TotalTax { get; set; }
        public decimal TotalNetAmount { get; set; }
        public string? FromDateBs { get; set; }
        public string? ToDateBs { get; set; }
        public string? BranchNames { get; set; }
        public string? OrderBy { get; set; }
        public string? MemberId { get; set; }
        public string? MemberName { get; set; }
        public int TotalDepositTypes { get; set; }
    }
    public class InterestAndTaxDetailReqResponse
    {
    }
}
