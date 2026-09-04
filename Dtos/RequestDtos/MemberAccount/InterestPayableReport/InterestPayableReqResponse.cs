namespace NexgenCosysReport.Dtos.RequestDtos.MemberAccount.InterestPayableReport
{

    public class InterestPayableRequestDto
    {
        public string TillDateBs { get; set; } = string.Empty;
        public string? BranchIds { get; set; }
        public string OrderBy { get; set; } = "Member Id";
        public string? BranchName { get; set; }
        public string ReportView { get; set; } = "A";
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
        public string? InterestCalculationType { get; set; }   // e.g. "Normal Flat Interest"
        public string? BalanceType { get; set; }                // e.g. "DAILY MINIMUM BALANCE"
        public string? MaturityDate { get; set; }
        public string? Type { get; set; }
        public string? IsActualOrProvisional { get; set; }
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
