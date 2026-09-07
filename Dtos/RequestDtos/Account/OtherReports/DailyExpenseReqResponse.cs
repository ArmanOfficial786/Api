// Dtos/RequestDtos/Account/OtherReports/DailyExpenseRequestDto.cs
namespace NexgenCosysReport.Dtos.RequestDtos.Account.OtherReports
{
    public class DailyExpenseRequestDto
    {
        public string FromDate { get; set; } = string.Empty;
        public string ToDate { get; set; } = string.Empty;
        public string? BranchIds { get; set; }
        public string OrderBy { get; set; } = "Sub Ledger";
        public bool VisualReport { get; set; }
    }

    public class DailyExpenseRowDto
    {
        public string? LedgerHead { get; set; }
        public int? AcoLedgerHeadId { get; set; }
        public int? ParentId { get; set; }
        public string? MainLedger { get; set; }
        public string? SubLedger { get; set; }
        public int? NodeLevel { get; set; }
        public decimal? DebitAmount { get; set; }
        public decimal? CreditAmount { get; set; }
        public string? VoucherType { get; set; }
        public decimal? Balance { get; set; }
    }

    public class DailyExpenseData
    {
        public List<DailyExpenseRowDto> Rows { get; set; } = new List<DailyExpenseRowDto>();
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