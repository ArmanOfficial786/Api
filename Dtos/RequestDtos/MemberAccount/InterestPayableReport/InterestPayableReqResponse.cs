namespace NexgenCosysReport.Dtos.RequestDtos.MemberAccount.InterestPayableReport
{

    public class InterestPayableRequestDto
    {
        public string TillDateBs { get; set; } = string.Empty;
        public string OfficeId { get; set; } = "-1";
        public string OrderBy { get; set; } = "Member Id";
        public string OfficeName { get; set; } = "All";
        public string ReportView { get; set; } = "A"; // A=All, P=Only On Till Date
        public bool VisualReport { get; set; } = false;
    }

    public class InterestPayableRowDto
    {
        public string? DepositTypeName { get; set; }
        public string? MemberId { get; set; }
        public string? MemberName { get; set; }
        public string? AccountNo { get; set; }
        public string? InterestFrom { get; set; }
        public string? InterestFromBs { get; set; }
        public decimal? InterestRate { get; set; }
        public decimal? InterestAmount { get; set; }
        public decimal? TaxAmount { get; set; }
        public decimal? Balance { get; set; }
    }

    public class InterestPayableData
    {
        public List<InterestPayableRowDto> Rows { get; set; } = new();
        public int TotalRecords { get; set; }
        public decimal TotalInterest { get; set; }
        public decimal TotalTax { get; set; }
        public decimal TotalBalance { get; set; }
        public string? TillDateBs { get; set; }
        public string? OfficeName { get; set; }
        public string? OrderBy { get; set; }
        public string? ReportView { get; set; }
        public string? ReportViewName { get; set; }
        public int TotalDepositTypes { get; set; }
    }
    public class InterestPayableReqResponse
    {
    }
}
