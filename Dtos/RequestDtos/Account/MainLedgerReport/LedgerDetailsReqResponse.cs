//namespace NexgenCosysReport.Dtos.RequestDtos.Account.SubLedgerDetailsReport
//{
//    public class SubLedgerDetailsRequestDto
//    {
//        public string FromDateBs { get; set; } = string.Empty;
//        public string ToDateBs { get; set; } = string.Empty;
//        public string? BranchId { get; set; }
//        public string? VoucherType { get; set; }
//        public string? OrderBy { get; set; }
//        public string? ReportType { get; set; }
//        public long LedgerHeadId { get; set; }
//        public string? LedgerName { get; set; }
//        public bool ShowOpeningBalance { get; set; } = true;
//        public int SelectedAccountType { get; set; }
//        public string? LedgerHead { get; set; }
//        public bool IsSummary { get; set; }
//    }

//    // Columns match sp_6_56_GetLedgerDetails' output SELECT list
//    public class SubLedgerDetailsRowDto
//    {
//        public string? AccountType { get; set; }
//        public string? VoucherNo { get; set; }
//        public string? VoucherDate { get; set; }      // BS text, e.g. "2079/03/01"
//        public DateTime? VoucherDateOn { get; set; }   // AD date
//        public string? MainLedger { get; set; }
//        public string? SubLedger1 { get; set; }
//        public string? SubLedger2 { get; set; }
//        public string? SubLedger3 { get; set; }
//        public string? SubLedger4 { get; set; }
//        public string? SubLedger5 { get; set; }
//        public string? Narration { get; set; }         // Already prefixed "A - " / "M - " by the SP
//        public string? NarrationLedger { get; set; }
//        public decimal? OpeningDebitAmount { get; set; }   // Summary SP only
//        public decimal? OpeningCreditAmount { get; set; }  // Summary SP only
//        public decimal? DebitAmount { get; set; }
//        public decimal? CreditAmount { get; set; }
//        public decimal? BalanceAmount { get; set; }
//        public string? Type { get; set; }              // "DR" or "CR" — computed by the SP, don't recompute
//        public bool? IsAutomatic { get; set; }
//    }

//    public class SubLedgerDetailsData
//    {
//        public List<SubLedgerDetailsRowDto> Rows { get; set; } = new();
//        public int TotalRecords { get; set; }
//        public decimal TotalDebitAmount { get; set; }
//        public decimal TotalCreditAmount { get; set; }
//        public decimal TotalBalance { get; set; }
//        public decimal OpeningBalance { get; set; }
//        public decimal ClosingBalance { get; set; }
//        public string? AccountType { get; set; }
//        public string? FromDateBs { get; set; }
//        public string? ToDateBs { get; set; }
//        public string? BranchName { get; set; }
//        public string? LedgerName { get; set; }
//        public string? SubLedgerName { get; set; }
//        public string? VoucherType { get; set; }
//        public string? ReportType { get; set; }
//        public bool ShowOpeningBalance { get; set; }
//        public string? OrderBy { get; set; }
//        public string? LedgerHead { get; set; }
//        public bool IsSummary { get; set; }
//    }
//}






namespace NexgenCosysReport.Dtos.RequestDtos.Account.SubLedgerDetailsReport
{
    public class SubLedgerDetailsRequestDto
    {
        public string FromDateBs { get; set; } = string.Empty;
        public string ToDateBs { get; set; } = string.Empty;
        public string? BranchId { get; set; }
        public string? VoucherType { get; set; }
        public string? OrderBy { get; set; }

        /// <summary>
        /// "LedgerDetailsReport" (Main Ledger only), "1stLedgerDetailsReport",
        /// "2ndLedgerDetailsReport", "3rdLedgerDetailsReport", or
        /// "4thLedgerDetailsReport" — mirrors CAccountOperationReports.GetLedgerDetails'
        /// reportType parameter from the webform.
        /// </summary>
        public string? ReportType { get; set; }

        public long LedgerHeadId { get; set; }
        public string? LedgerName { get; set; }
        public bool ShowOpeningBalance { get; set; } = true;
        public int SelectedAccountType { get; set; }

        /// <summary>
        /// Same shape as the webform's `List&lt;string&gt; ledgerHead`:
        /// index 0 = Main Ledger, 1 = Sub Ledger 1, 2 = Sub Ledger 2,
        /// 3 = Sub Ledger 3, 4 = Sub Ledger 4. Only as many entries as
        /// ReportType needs are required — BuildLedgerFilter pads the
        /// rest with empty strings. This was previously typed as a bare
        /// `string?`, which doesn't have .Count/.Add — that mismatch is
        /// what caused the CS1929/CS0019 build errors.
        /// </summary>
        public List<string> LedgerHead { get; set; } = new();

        public bool IsSummary { get; set; }
    }

    // Columns match sp_6_56_GetLedgerDetails' output SELECT list
    public class SubLedgerDetailsRowDto
    {
        public string? AccountType { get; set; }
        public string? VoucherNo { get; set; }
        public string? VoucherDate { get; set; }      // BS text, e.g. "2079/03/01"
        public DateTime? VoucherDateOn { get; set; }   // AD date
        public string? MainLedger { get; set; }
        public string? SubLedger1 { get; set; }
        public string? SubLedger2 { get; set; }
        public string? SubLedger3 { get; set; }
        public string? SubLedger4 { get; set; }
        public string? SubLedger5 { get; set; }
        public string? Narration { get; set; }         // Already prefixed "A - " / "M - " by the SP
        public string? NarrationLedger { get; set; }
        public decimal? OpeningDebitAmount { get; set; }   // Summary SP only
        public decimal? OpeningCreditAmount { get; set; }  // Summary SP only
        public decimal? DebitAmount { get; set; }
        public decimal? CreditAmount { get; set; }
        public decimal? BalanceAmount { get; set; }
        public string? Type { get; set; }              // "DR" or "CR" — computed by the SP, don't recompute
        public bool? IsAutomatic { get; set; }
    }

    public class SubLedgerDetailsData
    {
        public List<SubLedgerDetailsRowDto> Rows { get; set; } = new();
        public int TotalRecords { get; set; }
        public decimal TotalDebitAmount { get; set; }
        public decimal TotalCreditAmount { get; set; }
        public decimal TotalBalance { get; set; }
        public decimal OpeningBalance { get; set; }
        public decimal ClosingBalance { get; set; }
        public string? AccountType { get; set; }
        public string? FromDateBs { get; set; }
        public string? ToDateBs { get; set; }
        public string? BranchName { get; set; }
        public string? LedgerName { get; set; }
        public string? SubLedgerName { get; set; }
        public string? VoucherType { get; set; }
        public string? ReportType { get; set; }
        public bool ShowOpeningBalance { get; set; }
        public string? OrderBy { get; set; }
        public List<string> LedgerHead { get; set; } = new();
        public bool IsSummary { get; set; }
    }
}