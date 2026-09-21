namespace NexgenCosysReport.Dtos.RequestDtos.Share
{
    public class ShareHoldingRequestDto
    {
        public long MemberId { get; set; } = -1;
        public string? FromDateBs { get; set; }
        public string? ToDateBs { get; set; }
        public long OfficeId { get; set; } = -1;
        public long ShareTypeId { get; set; } = -1;
        public long MemberTypeId { get; set; } = -1;
        public long MemberGroupId { get; set; } = -1;
        public string OrderBy { get; set; } = "MemberId";
        public bool VisualReport { get; set; } = false;
    }

    public class ShareHoldingRowDto
    {
        public string? MemberId { get; set; }
        public string? MemberIdFirst { get; set; }
        public decimal? MemberIdLast { get; set; }
        public long? MemMemberRegistrationId { get; set; }
        public string? MemberGroup { get; set; }
        public string? MemberName { get; set; }
        public string? SharePurchaseOn { get; set; }
        public string? ShareTransferredOn { get; set; }
        public string? ShareReturnedOn { get; set; }
        public decimal? TotalNoShare { get; set; }
        public string? ShareFrom { get; set; }
        public string? ShareTO { get; set; }
        public decimal? Amount { get; set; }
        public string? ShareType { get; set; }
        public string? HoldingPeriodFrom { get; set; }
        public string? HoldingPeriodFromBs { get; set; }
        public string? HoldingPeriodTo { get; set; }
        public string? HoldingPeriodToBs { get; set; }
        public int? HoldingNoDays { get; set; }
    }

    public class ShareHoldingData
    {
        public List<ShareHoldingRowDto> Rows { get; set; } = [];
        public int TotalRecords { get; set; }
        public int TotalMembers { get; set; }
        public decimal TotalNoShare { get; set; }
        public decimal TotalAmount { get; set; }
        public string? FromDateBs { get; set; }
        public string? ToDateBs { get; set; }
        public string? OfficeName { get; set; }
        public string? ShareTypeName { get; set; }
        public string? MemberTypeName { get; set; }
        public string? MemberGroupName { get; set; }
        public string? OrderBy { get; set; }
    }
    public class ShareHoldingReqResponseDto
    {
    }
}
