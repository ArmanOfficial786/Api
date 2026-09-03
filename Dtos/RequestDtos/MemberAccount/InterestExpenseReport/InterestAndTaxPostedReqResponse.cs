namespace NexgenCosysReport.Dtos.RequestDtos.MemberAccount.InterestExpenseReport
{

    public class InterestAndTaxPostedRequestDto
    {
        public string FromDateBs { get; set; } = string.Empty;
        public string ToDateBs { get; set; } = string.Empty;
        public string BranchIds { get; set; } = "-1";
        public string OrderBy { get; set; } = "Member Id";
        public string BranchName { get; set; } = "All Branches";
        public bool VisualReport { get; set; } = false;
    }
    public class InterestAndTaxPostedRowDto
    {
        public string DepositTypeName { get; set; }
        public string MemberId { get; set; }
        public string MemberName { get; set; }
        public string AccountNo { get; set; }
        public string InterestDate { get; set; }
        public string InterestRate { get; set; } // Changed to string to handle "11.0%" format
        public decimal? Interest { get; set; }
        public decimal? Tax { get; set; }
        public decimal? NetBalance { get; set; }
        public decimal? NetInterest { get; set; }
    }

    public class InterestAndTaxPostedData
    {
        public List<InterestAndTaxPostedRowDto> Rows { get; set; } = new();
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
    public class InterestAndTaxPostedReqResponse
    {
    }
}
