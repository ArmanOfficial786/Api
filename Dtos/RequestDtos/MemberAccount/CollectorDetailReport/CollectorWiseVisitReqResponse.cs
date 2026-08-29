namespace NexgenCosysReport.Dtos.RequestDtos.MemberAccount.CollectorDetailReport
{

    public class CollectorWiseVisitRequestDto
    {
        public string Month { get; set; } = string.Empty; // 01-12
        public string Year { get; set; } = string.Empty;
        public long CollectorId { get; set; } = -1;
        public string CollectorName { get; set; } = string.Empty;
        public string VisitType { get; set; } = "Deposit"; // All, Deposit, Loan
        public string OrderBy { get; set; } = "Member Id";
        public string ReportType { get; set; } = "L"; // L=Landscape, P=Portrait
        public string AmountType { get; set; } = "C"; // C=Count, A=Amount
        public string GenerateBy { get; set; } = "V"; // V=Only Visit, A=All Account
        public bool VisualReport { get; set; } = false;
    }

    public class CollectorWiseVisitRowDto
    {
        public string? MemberId { get; set; }
        public string? MemberName { get; set; }
        public string? AccountNo { get; set; }
        public string? DepositTypeName { get; set; }
        public decimal? TotalAmount { get; set; }
        public int? VisitCount { get; set; }
        public string? CollectorName { get; set; }
        public string? MonthName { get; set; }
        public string? Year { get; set; }
    }

    public class CollectorWiseVisitData
    {
        public List<CollectorWiseVisitRowDto> Rows { get; set; } = new();
        public int TotalRecords { get; set; }
        public decimal TotalAmount { get; set; }
        public int TotalVisits { get; set; }
        public string? Month { get; set; }
        public string? Year { get; set; }
        public string? CollectorName { get; set; }
        public long CollectorId { get; set; }
        public string? VisitType { get; set; }
        public string? OrderBy { get; set; }
        public string? ReportType { get; set; }
        public string? AmountType { get; set; }
        public string? GenerateBy { get; set; }
        public string? MonthName { get; set; }
    }

    public class CollectorWiseVisitReqResponse
    {
    }
}
