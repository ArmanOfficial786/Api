namespace NexgenCosysReport.Dtos.RequestDtos.Share
{
    public class ShareReturnRequestDto
    {
        public string? FromDateBs { get; set; }
        public string? ToDateBs { get; set; }
        public long OfficeId { get; set; } = -1;
        public long ShareTypeId { get; set; } = -1;
        public long MemberGroupId { get; set; } = -1;
        public string OrderBy { get; set; } = "MemberId";
        public bool VisualReport { get; set; } = false;
    }

    public class ShareReturnRowDto
    {
        public string? MemberId { get; set; }
        public string? MemberName { get; set; }
        public string? MemberGroup { get; set; }
        public decimal? Amount { get; set; }
        public string? PurchaseDate { get; set; }
        public string? ReturnDate { get; set; }
        public decimal? TotalNoShare { get; set; }
        public string? ShareType { get; set; }
    }

    public class ShareReturnData
    {
        public List<ShareReturnRowDto> Rows { get; set; } = [];
        public int TotalRecords { get; set; }
        public int TotalMembers { get; set; }
        public decimal TotalNoShare { get; set; }
        public decimal TotalAmount { get; set; }
        public string? FromDateBs { get; set; }
        public string? ToDateBs { get; set; }
        public string? OfficeName { get; set; }
        public string? ShareTypeName { get; set; }
        public string? MemberGroupName { get; set; }
        public string? OrderBy { get; set; }
    }
}
