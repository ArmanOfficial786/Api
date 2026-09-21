namespace NexgenCosysReport.Dtos.RequestDtos.Share
{
    public class ShareStatementRequestDto
    {
        public long MemberId { get; set; } = -1;
        public long ShareTypeId { get; set; } = -1;
        public bool EnableHeader { get; set; } = true;
        public bool EnableBillNo { get; set; } = false;
        public bool VisualReport { get; set; } = false;
    }

    public class ShareStatementRowDto
    {
        public string? MemberId { get; set; }
        public string? MemberName { get; set; }
        public string? Date { get; set; }
        public string? Address { get; set; }
        public string? PhoneNo { get; set; }
        public string? Description { get; set; }
        public decimal? PurchasedAmount { get; set; }
        public decimal? ReturnedAmount { get; set; }
        public decimal? Balance { get; set; }
        public string? ShareType { get; set; }
        public decimal? TransactionNumber { get; set; }
    }

    public class ShareStatementData
    {
        public List<ShareStatementRowDto> Rows { get; set; } = [];
        public int TotalRecords { get; set; }
        public decimal TotalPurchasedAmount { get; set; }
        public decimal TotalReturnedAmount { get; set; }
        public decimal ClosingBalance { get; set; }
        public string? MemberId { get; set; }
        public string? MemberName { get; set; }
        public string? Address { get; set; }
        public string? PhoneNo { get; set; }
        public string? ShareTypeName { get; set; }
    }
}
