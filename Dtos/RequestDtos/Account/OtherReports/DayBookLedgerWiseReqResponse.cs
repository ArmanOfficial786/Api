// Dtos/RequestDtos/Account/OtherReports/DayBookLedgerWiseRequestDto.cs
namespace NexgenCosysReport.Dtos.RequestDtos.Account.OtherReports
{
    public class DayBookLedgerWiseRequestDto
    {
        public string FromDate { get; set; } = string.Empty;
        public string ToDate { get; set; } = string.Empty;
        public string? BranchIds { get; set; }
        public string OrderBy { get; set; } = "Sub Ledger";
        public bool VisualReport { get; set; } = false;
    }

    public class DayBookLedgerWiseRowDto
    {
        public string? LedgerHead { get; set; }
        public int? AcoLedgerHeadId { get; set; }
        public int? ParentId { get; set; }
        public string? MainLedger { get; set; }
        public string? SubLedger1 { get; set; }
        public string? SubLedger2 { get; set; }
        public string? SubLedger3 { get; set; }
        public int? NodeLevel { get; set; }
        public string? Narration { get; set; }
        public decimal? DebitAmount { get; set; }
        public decimal? CreditAmount { get; set; }
        public string? VoucherType { get; set; }
        public string? VoucherNo { get; set; }
        public decimal? TodayBalance { get; set; }
        public decimal? OpeningBalance { get; set; }
        public decimal? ClosingBalance { get; set; }
    }

    public class DayBookLedgerWiseData
    {
        public List<DayBookLedgerWiseRowDto> Rows { get; set; } = new List<DayBookLedgerWiseRowDto>();
        public int TotalRecords { get; set; }
        public decimal TotalDebitAmount { get; set; }
        public decimal TotalCreditAmount { get; set; }
        public decimal TotalBalance { get; set; }
        public string? FromDateBs { get; set; }
        public string? ToDateBs { get; set; }
        public string? BranchNames { get; set; }
        public string? OrderBy { get; set; }
    }
}