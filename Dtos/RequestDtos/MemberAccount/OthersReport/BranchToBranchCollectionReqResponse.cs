namespace NexgenCosysReport.Dtos.RequestDtos.MemberAccount.OthersReport
{
    public class BranchToBranchCollectionRequestDto
    {
        public string FromDateBs { get; set; } = string.Empty;
        public string ToDateBs { get; set; } = string.Empty;
        public long BranchFromId { get; set; }
        public long BranchToId { get; set; }
        public long? CollectorId { get; set; }
        public string ReportType { get; set; } = "All";
        public string OrderBy { get; set; } = "Member Id";
        public bool SameCompanyName { get; set; } = true;
        public bool VisualReport { get; set; } = false;
    }
    public class BranchToBranchCollectionRowDto
    {
        public string? Date { get; set; }
        public string? MemberId { get; set; }
        public string? MemberName { get; set; }
        public string? AccountNo { get; set; }
        public string? Type { get; set; }
        public decimal? Amount { get; set; }
        public string? Collector { get; set; }
        public string? Operator { get; set; }
        public string? Details { get; set; }
        public string? SavingType { get; set; }
    }

    public class BranchToBranchCollectionData
    {
        public List<BranchToBranchCollectionRowDto> Rows { get; set; } = new();
        public int TotalRecords { get; set; }
        public decimal TotalAmount { get; set; }
        public string? FromDateBs { get; set; }
        public string? ToDateBs { get; set; }
        public long BranchFromId { get; set; }
        public string? BranchFromName { get; set; }
        public long BranchToId { get; set; }
        public string? BranchToName { get; set; }
        public long? CollectorId { get; set; }
        public string? CollectorName { get; set; }
        public string? ReportType { get; set; }
        public string? OrderBy { get; set; }
    }
    public class BranchToBranchCollectionReqResponse
    {
    }
}
