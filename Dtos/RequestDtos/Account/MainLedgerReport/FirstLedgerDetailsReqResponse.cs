// Dtos/RequestDtos/Account/1stLedgerDetailsReport/FirstLedgerDetailsRequestDto.cs
namespace NexgenCosysReport.Dtos.RequestDtos.Account.FirstLedgerDetailsReport
{
    public class FirstLedgerDetailsRequestDto
    {
        public string FromDate { get; set; } = string.Empty;
        public string ToDate { get; set; } = string.Empty;
        public string? BranchIds { get; set; }
        public int LedgerHeadId { get; set; } = -1;
        public string? LedgerName { get; set; }
        public string? SubLedgerName { get; set; }
        public string VoucherType { get; set; } = "All"; // "All", "Manual", "Auto"
        public string ReportType { get; set; } = "Detail"; // "Detail" or "Summary"
        public bool ShowOpeningBalance { get; set; } = false;
        public string OrderBy { get; set; } = "Voucher Date";
        public bool VisualReport { get; set; } = false;
    }

    public class FirstLedgerDetailsRowDto
    {
        public string? VoucherNo { get; set; }
        public string? VoucherOnBs { get; set; }
        public DateTime? VoucherOn { get; set; }
        public string? MainLedger { get; set; }
        public string? SubLedger1 { get; set; }
        public string? SubLedger2 { get; set; }
        public string? SubLedger3 { get; set; }
        public string? SubLedger4 { get; set; }
        public string? Narration { get; set; }
        public string? NarrationLedger { get; set; }
        public decimal? DebitAmount { get; set; }
        public decimal? CreditAmount { get; set; }
        public string? VoucherType { get; set; }
        public bool? IsActive { get; set; }
        public decimal? Balance { get; set; }
        public string? OfficeName { get; set; }
    }

    public class FirstLedgerDetailsSummaryDto
    {
        public string? VoucherNo { get; set; }
        public string? VoucherOnBs { get; set; }
        public DateTime? VoucherOn { get; set; }
        public string? MainLedger { get; set; }
        public string? SubLedger1 { get; set; }
        public string? SubLedger2 { get; set; }
        public string? SubLedger3 { get; set; }
        public string? SubLedger4 { get; set; }
        public string? Narration { get; set; }
        public string? NarrationLedger { get; set; }
        public decimal? DebitAmount { get; set; }
        public decimal? CreditAmount { get; set; }
        public string? VoucherType { get; set; }
        public bool? IsActive { get; set; }
        public decimal? Balance { get; set; }
        public string? OfficeName { get; set; }
    }

    public class FirstLedgerDetailsData
    {
        public List<FirstLedgerDetailsRowDto> Rows { get; set; } = new List<FirstLedgerDetailsRowDto>();
        public List<FirstLedgerDetailsSummaryDto> SummaryRows { get; set; } = new List<FirstLedgerDetailsSummaryDto>();
        public int TotalRecords { get; set; }
        public decimal TotalDebitAmount { get; set; }
        public decimal TotalCreditAmount { get; set; }
        public decimal TotalBalance { get; set; }
        public decimal OpeningBalance { get; set; }
        public decimal ClosingBalance { get; set; }
        public string? AccountType { get; set; }
        public string? FromDateBs { get; set; }
        public string? ToDateBs { get; set; }
        public string? BranchNames { get; set; }
        public string? LedgerName { get; set; }
        public string? SubLedgerName { get; set; }
        public string? VoucherType { get; set; }
        public string? ReportType { get; set; }
        public bool ShowOpeningBalance { get; set; }
        public string? OrderBy { get; set; }
        public string? LedgerHeadName { get; set; }
    }
}