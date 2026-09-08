// Dtos/RequestDtos/Account/MainLedgerReport/LedgerDetailsRequestDto.cs
namespace NexgenCosysReport.Dtos.RequestDtos.Account.MainLedgerReport

{
    public class LedgerDetailsReqResponse
    {
        public string FromDateBs { get; set; } = string.Empty;
        public string ToDateBs { get; set; } = string.Empty;
        public string? BranchId { get; set; }          // single office id, "-1" = All

        // "Manual" | "Auto" | "All"
        public string VoucherType { get; set; } = "All";

        public string OrderBy { get; set; } = "-1";

        // Drives which SP-side filter block (SqlFilterMainLedger) gets built:
        // "LedgerDetailsReport"    -> MainLedger only
        // "1stLedgerDetailsReport" -> + SubLedger1
        // "2ndLedgerDetailsReport" -> + SubLedger2
        // "3rdLedgerDetailsReport" -> + SubLedger3
        // "4thLedgerDetailsReport" -> + SubLedger4
        public string ReportType { get; set; } = "4thLedgerDetailsReport";

        // Ledger hierarchy selections, in order:
        // [0] MainLedger, [1] SubLedger1, [2] SubLedger2, [3] SubLedger3, [4] SubLedger4
        // Must always contain exactly 5 entries (empty string for unused levels),
        // matching the legacy ledgerHead List<string> built from the 5 dropdowns.
        public List<string> LedgerHead { get; set; } = new() { "", "", "", "", "" };

        public long SelectedAccountType { get; set; } = -1; // ddlLedgerHead.SelectedValue (AcoAccountTypeId)
        public bool ShowOpeningBalance { get; set; } = true;
        public bool IsSummary { get; set; } = false;
    }

    // Columns match sp_6_56_GetLedgerDetails' output SELECT list
    public class LedgerDetailsRowDto
    {
        public string? MainLedger { get; set; }
        public string? SubLedger1 { get; set; }
        public string? SubLedger2 { get; set; }
        public string? SubLedger3 { get; set; }
        public string? SubLedger4 { get; set; }
        public DateTime? VoucherOn { get; set; }
        public string? VoucherNo { get; set; }
        public string? Narration { get; set; }
        public decimal? DebitAmount { get; set; }
        public decimal? CreditAmount { get; set; }
        public decimal? BalanceAmount { get; set; }
    }

    public class LedgerDetailsData
    {
        public List<LedgerDetailsRowDto> Rows { get; set; } = [];
        public int TotalRecords { get; set; }

        // Output parameters from the SP
        public decimal OpeningBalance { get; set; }
        public decimal ClosingBalance { get; set; }
        public string? AccountType { get; set; }

        public string? FromDateBs { get; set; }
        public string? ToDateBs { get; set; }
        public string? BranchName { get; set; }
        public string? VoucherType { get; set; }
        public string? OrderBy { get; set; }
        public List<string> LedgerHead { get; set; } = new();
        public bool ShowOpeningBalance { get; set; }
        public bool IsSummary { get; set; }
    }
}