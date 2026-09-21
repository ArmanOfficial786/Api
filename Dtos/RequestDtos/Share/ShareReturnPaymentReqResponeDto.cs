namespace NexgenCosysReport.Dtos.RequestDtos.Share
{
    public class ShareReturnPaymentRequestDto
    {
        public string? FromDateBs { get; set; }
        public string? ToDateBs { get; set; }
        public string? OfficeIds { get; set; }
        public bool VisualReport { get; set; } = false;
    }

    public class ShareReturnPaymentRowDto
    {
        public long? AcoTransactionId { get; set; }
        public long? MemMemberRegistrationId { get; set; }
        public string? MemberId { get; set; }
        public string? MemberName { get; set; }
        public string? PanVatNo { get; set; }
        public string? AccountNo { get; set; }
        public decimal? CashAmount { get; set; }
        public decimal? TaxAmount { get; set; }
        public decimal? BonusShareAmount { get; set; }
        public string? TransactionOnBs { get; set; }
    }

    public class ShareReturnPaymentData
    {
        public List<ShareReturnPaymentRowDto> Rows { get; set; } = [];
        public int TotalRecords { get; set; }
        public int TotalMembers { get; set; }
        public decimal TotalCashAmount { get; set; }
        public decimal TotalTaxAmount { get; set; }
        public decimal TotalBonusShareAmount { get; set; }
        public string? FromDateBs { get; set; }
        public string? ToDateBs { get; set; }
        public string? BranchName { get; set; }
    }
}
