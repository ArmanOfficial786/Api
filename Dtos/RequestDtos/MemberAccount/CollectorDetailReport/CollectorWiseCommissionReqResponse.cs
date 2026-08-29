namespace NexgenCosysReport.Dtos.RequestDtos.MemberAccount.CollectorDetailReport
{

    public class CollectorWiseCommissionRequestDto
    {
        public string FromDateBs { get; set; } = string.Empty;
        public string ToDateBs { get; set; } = string.Empty;
        public string OrderBy { get; set; } = "Member Id";
        public long CollectorId { get; set; } = -1;
        public string CollectorName { get; set; } = string.Empty;
        public bool VisualReport { get; set; } = false;
    }


    public class CollectorWiseCommissionRowDto
    {
        public string? Date { get; set; }
        public string? MemberId { get; set; }
        public string? MemberName { get; set; }
        public string? AccountNo { get; set; }
        public string? Type { get; set; }
        public decimal? CollectedAmount { get; set; }
        public decimal? CommissionAmount { get; set; }
        public string? Operator { get; set; }
        public string? Description { get; set; }
    }

    public class CollectorWiseCommissionData
    {
        public List<CollectorWiseCommissionRowDto> Rows { get; set; } = new();
        public int TotalRecords { get; set; }
        public decimal TotalCollectedAmount { get; set; }
        public decimal TotalCommissionAmount { get; set; }
        public string? FromDateBs { get; set; }
        public string? ToDateBs { get; set; }
        public string? OrderBy { get; set; }
        public string? CollectorName { get; set; }
        public long CollectorId { get; set; }
    }
    public class CollectorWiseCommissionReqResponse
    {
    }
}
