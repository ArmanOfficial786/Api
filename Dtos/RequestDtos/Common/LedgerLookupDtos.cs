namespace NexgenCosysReport.Dtos.RequestDtos.Common
{
    public class LedgerHeadRowDto
    {
        public int AcoAccountTypeId { get; set; }
        public string? AccountType { get; set; }
    }

    public class LedgerNameRequestDto
    {
        public string FromDate { get; set; } = string.Empty;
        public string ToDate { get; set; } = string.Empty;
        public string? BranchId { get; set; }      // "-1" = All
        public int AccountTypeId { get; set; } = -1;
    }

    public class LedgerNameRowDto
    {
        public string? MainLedger { get; set; }
    }

    public class SubLedgerNameRequestDto
    {
        public string FromDate { get; set; } = string.Empty;
        public string ToDate { get; set; } = string.Empty;
        public string? BranchId { get; set; }      // "-1" = All
        public int AccountTypeId { get; set; } = -1;
        public string MainLedger { get; set; } = string.Empty;
    }

    public class SubLedgerNameRowDto
    {
        public string? SubLedger1 { get; set; }
    }

    // ---- 2nd Sub Ledger ----
    public class SecondSubLedgerNameRequestDto
    {
        public string FromDate { get; set; } = string.Empty;
        public string ToDate { get; set; } = string.Empty;
        public string? BranchId { get; set; }      // "-1" = All
        public int AccountTypeId { get; set; } = -1;
        public string SubLedger1 { get; set; } = string.Empty;
    }

    public class SecondSubLedgerNameRowDto
    {
        public string? SubLedger2 { get; set; }
    }

    // ---- 3rd Sub Ledger ----
    public class ThirdSubLedgerNameRequestDto
    {
        public string FromDate { get; set; } = string.Empty;
        public string ToDate { get; set; } = string.Empty;
        public string? BranchId { get; set; }      // "-1" = All
        public int AccountTypeId { get; set; } = -1;
        public string SubLedger2 { get; set; } = string.Empty;
    }

    public class ThirdSubLedgerNameRowDto
    {
        public string? SubLedger3 { get; set; }
    }

    // ---- 4th Sub Ledger ----
    public class FourthSubLedgerNameRequestDto
    {
        public string FromDate { get; set; } = string.Empty;
        public string ToDate { get; set; } = string.Empty;
        public string? BranchId { get; set; }      // "-1" = All
        public int AccountTypeId { get; set; } = -1;
        public string SubLedger3 { get; set; } = string.Empty;
    }

    public class FourthSubLedgerNameRowDto
    {
        public string? SubLedger4 { get; set; }
    }
}
