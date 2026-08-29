namespace NexgenCosysReport.Dtos.RequestDtos.MemberAccount.InterestExpenseReport
{

    public class InterestAndTaxTypeWiseRequestDto
    {
        public string FromDateBs { get; set; } = string.Empty;
        public string ToDateBs { get; set; } = string.Empty;
        public string BranchIds { get; set; } = "-1";
        public string OrderBy { get; set; } = "Date";
        public string ReportView { get; set; } = "1"; // 1=Normal, 2=TaxWise Normal, 3=TaxWise All
        public string BranchName { get; set; } = "All Branches";
        public bool VisualReport { get; set; } = false;
    }

    public class InterestAndTaxTypeWiseRowDto
    {
        public string? DepositTypeName { get; set; }
        public string? Date { get; set; }
        public string? DateBs { get; set; }
        public decimal? Interest { get; set; }
        public decimal? Tax { get; set; }
        public decimal? PercentTax { get; set; }
        public decimal? NetAmount { get; set; }
        public string? Remarks { get; set; }
    }

    public class InterestAndTaxTypeWiseData
    {
        public List<InterestAndTaxTypeWiseRowDto> Rows { get; set; } = new();
        public int TotalRecords { get; set; }
        public decimal TotalInterest { get; set; }
        public decimal TotalTax { get; set; }
        public decimal TotalNetAmount { get; set; }
        public string? FromDateBs { get; set; }
        public string? ToDateBs { get; set; }
        public string? BranchNames { get; set; }
        public string? OrderBy { get; set; }
        public string? ReportView { get; set; }
        public int TotalDepositTypes { get; set; }
    }
    public class InterestAndTaxTypeWiseReqResponse
    {
    }
}
