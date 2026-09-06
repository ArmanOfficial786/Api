// Dtos/RequestDtos/Account/OtherReports/VoucherDetailsRequestDto.cs
namespace NexgenCosysReport.Dtos.RequestDtos.Account.OtherReports
{
    public class VoucherDetailsRequestDto
    {
        public string FromDate { get; set; } = string.Empty;
        public string ToDate { get; set; } = string.Empty;
        public string? BranchIds { get; set; }
        public long? VoucherId { get; set; }
        public string OrderBy { get; set; } = "Main Ledger";
        public string ViewType { get; set; } = "None"; // "None" or "Grouping"
        public bool VisualReport { get; set; } = false;
    }

    public class VoucherDetailsRowDto
    {
        public string? VoucherNo { get; set; }
        public string? VoucherOnBs { get; set; }
        public int? AcoLedgerHeadId { get; set; }
        public int? ParentId { get; set; }
        public int? NodeLevel { get; set; }
        public string? MainLedger { get; set; }
        public string? SubLedger1 { get; set; }
        public string? SubLedger2 { get; set; }
        public string? SubLedger3 { get; set; }
        public string? Narration { get; set; }
        public string? NarrationLedger { get; set; }
        public decimal? DebitAmount { get; set; }
        public decimal? CreditAmount { get; set; }
        public string? VoucherType { get; set; }
        public bool? IsActive { get; set; }
        public string? OfficeName { get; set; }
        public long? UsmOfficeId { get; set; }
    }

    public class VoucherDetailsData
    {
        public List<VoucherDetailsRowDto> Rows { get; set; } = new List<VoucherDetailsRowDto>();
        public int TotalRecords { get; set; }
        public decimal TotalDebitAmount { get; set; }
        public decimal TotalCreditAmount { get; set; }
        public string? FromDateBs { get; set; }
        public string? ToDateBs { get; set; }
        public string? BranchNames { get; set; }
        public string? OrderBy { get; set; }
        public string? ViewType { get; set; }
        public long? VoucherId { get; set; }
        public string? VoucherNo { get; set; }
    }
}