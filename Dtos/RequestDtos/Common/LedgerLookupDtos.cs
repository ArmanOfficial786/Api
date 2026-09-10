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
}
