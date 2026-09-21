namespace NexgenCosysReport.Dtos.RequestDtos.Share
{
    public class ShareDividendRequestDto
    {
        public long FiscalYearId { get; set; } = -1;
        public long OfficeId { get; set; } = -1;
        public long ShareTypeId { get; set; } = -1;
        public long MemberGroupId { get; set; } = -1;
        public string OrderBy { get; set; } = "MemberId";
        public bool VisualReport { get; set; } = false;
    }

    public class ShareDividendRowDto
    {
        public string? MemberId { get; set; }
        public string? MemberIdFirst { get; set; }
        public decimal? MemberIdLast { get; set; }
        public string? MemberName { get; set; }
        public string? MemberGroup { get; set; }
        public DateTime? ShareHoldingPeriodFromOn { get; set; }
        public string? ShareHoldingPeriodFromOnBs { get; set; }
        public DateTime? ShareHoldingPeriodToOn { get; set; }
        public string? ShareHoldingPeriodToOnBs { get; set; }
        public decimal? PurchaseAmount { get; set; }
        public int? HoldingNoDays { get; set; }
        public decimal? AggregateAmount { get; set; }
        public decimal? Reserve { get; set; }
        public decimal? DividendPayableAmount { get; set; }
        public decimal? Allotment { get; set; }
    }

    public class ShareDividendData
    {
        public List<ShareDividendRowDto> Rows { get; set; } = [];
        public int TotalRecords { get; set; }
        public int TotalMembers { get; set; }
        public decimal TotalPurchaseAmount { get; set; }
        public decimal TotalAggregateAmount { get; set; }
        public decimal TotalDividendPayableAmount { get; set; }
        public decimal ShareDividendPercent { get; set; }
        public decimal DividendAmount { get; set; }
        public decimal RemainingReserveAmount { get; set; }
        public string? FiscalYear { get; set; }
        public string? FromDateBs { get; set; }
        public string? ToDateBs { get; set; }
        public string? OfficeName { get; set; }
        public string? ShareTypeName { get; set; }
        public string? MemberGroupName { get; set; }
        public string? OrderBy { get; set; }
    }
}
