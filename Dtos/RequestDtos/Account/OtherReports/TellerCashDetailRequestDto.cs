// Dtos/RequestDtos/AccountOperation/OthersReport/TellerCashDetailRequestDto.cs
namespace NexgenCosysReport.Dtos.RequestDtos.AccountOperation.OthersReport
{
    public class TellerCashDetailRequestDto
    {
        public string TransactionDateBs { get; set; } = string.Empty;
        public string? BranchId { get; set; }      // single office id — "-1" not used here; legacy requires selection
        public string? TellerId { get; set; }       // single teller/user id, "-1" = All Teller
        public string OrderBy { get; set; } = "Member Id";
        public string ReportType { get; set; } = "D"; // "D" = Detail, "S" = Summary
    }

    // dt1 — "Auto"/system transactions (TransactionDetailAuto), from sp_6_56_GetTellerCashDetailTransaction
    // Column names are INFERRED from the OrderBy options and typical teller-transaction fields —
    // confirm against the actual SP output and adjust if names differ.
    public class TellerCashDetailTransactionRowDto
    {
        public string? MemberId { get; set; }
        public string? MemberName { get; set; }
        public string? AccountNo { get; set; }
        public string? BillNo { get; set; }
        public string? TransactionType { get; set; }
        public decimal? DebitAmount { get; set; }
        public decimal? CreditAmount { get; set; }
        public DateTime? TransactionOn { get; set; }
        public string? Operator { get; set; }
    }

    // dt2 — Manual voucher detail (DetailReport1), from sp_6_56_GetTellerCashDetailManual
    public class TellerCashDetailManualRowDto
    {
        public string? VoucherNo { get; set; }
        public string? Narration { get; set; }
        public decimal? DebitAmount { get; set; }
        public decimal? CreditAmount { get; set; }
        public DateTime? VoucherOn { get; set; }
        public string? Operator { get; set; }
    }

    public class TellerCashDetailData
    {
        public List<TellerCashDetailTransactionRowDto> TransactionRows { get; set; } = [];
        public List<TellerCashDetailManualRowDto> ManualRows { get; set; } = [];

        // From sp_6_56_GetTellerCashDetailTransaction output params
        public decimal TransactionCashBalanceDR { get; set; }
        public decimal TransactionCashBalanceCR { get; set; }

        // From sp_6_56_GetTellerCashDetailManual output params
        public decimal VoucherCashBalanceDR { get; set; }
        public decimal VoucherCashBalanceCR { get; set; }
        public decimal TellerCashFromVault { get; set; }
        public decimal TellerCashFromTeller { get; set; }
        public decimal TellerCashToTeller { get; set; }
        public decimal TellerCashToVault { get; set; }

        // Computed, matching TellerCashTransactionDetailXtraReport.GetTellerCashDetailBind
        public decimal TotalDR => TransactionCashBalanceDR + VoucherCashBalanceDR;
        public decimal TotalCR => TransactionCashBalanceCR + VoucherCashBalanceCR;
        public decimal TotalBalance => TotalDR - TotalCR;
        public decimal GrandTotal =>
            TellerCashFromVault + TellerCashFromTeller - TellerCashToTeller - TellerCashToVault + TotalBalance;

        public string? TransactionDateBs { get; set; }
        public string? BranchName { get; set; }
        public string? TellerName { get; set; }
        public string? OrderBy { get; set; }
        public string ReportType { get; set; } = "D";
    }
}