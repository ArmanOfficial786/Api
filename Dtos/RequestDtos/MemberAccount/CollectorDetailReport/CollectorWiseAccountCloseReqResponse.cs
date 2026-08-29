namespace NexgenCosysReport.Dtos.RequestDtos.MemberAccount.CollectorDetailReport
{
    public class CollectorWiseAccountCloseRequestDto
    {
        public string FromDateBs { get; set; } = string.Empty;
        public string ToDateBs { get; set; } = string.Empty;
        public string OrderBy { get; set; } = "Member Id";
        public long CollectorId { get; set; } = -1;
        public string CollectorName { get; set; } = string.Empty;
        public bool VisualReport { get; set; } = false;
    }

    public class CollectorWiseAccountCloseRowDto
    {
        public string? MemberId { get; set; }
        public string? MemberName { get; set; }
        public string? AccountNo { get; set; }
        public string? AccountOpenOnBs { get; set; }
        public string? AccountOpenDate { get; set; }
        public string? AccountCloseOnBs { get; set; }
        public string? CloseDate { get; set; }
        public string? DepositTypeName { get; set; }
        public decimal? InterestRate { get; set; }
        public decimal? CloseAmount { get; set; }
        public string? Operator { get; set; }
        public string? Reason { get; set; }
        public string? CollectorName { get; set; }
    }

    public class CollectorWiseAccountCloseData
    {
        public List<CollectorWiseAccountCloseRowDto> Rows { get; set; } = new();
        public int TotalRecords { get; set; }
        public decimal TotalCloseAmount { get; set; }
        public string? FromDateBs { get; set; }
        public string? ToDateBs { get; set; }
        public string? OrderBy { get; set; }
        public string? CollectorName { get; set; }
        public long CollectorId { get; set; }
        public int TotalClosedAccounts { get; set; }
    }

    public class CollectorWiseAccountCloseReqResponse
    {
    }
}
