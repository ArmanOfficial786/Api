namespace NexgenCosysReport.Dtos.RequestDtos.MemberAccount.CollectorDetailReport
{

    public class CollectorWiseCommissionSummaryRequestDto
    {
        public string FromDateBs { get; set; } = string.Empty;
        public string ToDateBs { get; set; } = string.Empty;
        public string BranchIds { get; set; } = "-1";
        public string OrderBy { get; set; } = "Type";
        public string BranchName { get; set; } = "All Branches";
        public bool VisualReport { get; set; } = false;
    }
    public class CollectorWiseCommissionSummaryRowDto
    {
        public string? Collector { get; set; }
        public string? Type { get; set; }
        public decimal? CollectedAmount { get; set; }
        public decimal? CommissionAmount { get; set; }
        public string? Description { get; set; }
    }

    public class CollectorWiseCommissionSummaryData
    {
        public List<CollectorWiseCommissionSummaryRowDto> Rows { get; set; } = new();
        public int TotalRecords { get; set; }
        public decimal TotalCollectedAmount { get; set; }
        public decimal TotalCommissionAmount { get; set; }
        public string? FromDateBs { get; set; }
        public string? ToDateBs { get; set; }
        public string? BranchNames { get; set; }
        public string? OrderBy { get; set; }
        public int TotalCollectors { get; set; }
    }

    public class CollectorWiseCommissionSummaryReqResponse
    {
    }
}
