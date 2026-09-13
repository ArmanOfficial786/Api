// Dtos/RequestDtos/Account/IBTReports/IBTStatementBranchwiseRequestDto.cs
namespace NexgenCosysReport.Dtos.RequestDtos.Account.IBTReports
{
    public class IBTStatementBranchwiseRequestDto
    {
        public string FromDateBs { get; set; } = string.Empty;
        public string ToDateBs { get; set; } = string.Empty;

        // Login office id — resolved server-side from the authenticated user's
        // office in the legacy WebForm (hfdOfficeId.Value = ucTO.LoginOfficeId),
        // NOT user-selectable. Kept here so the API caller (or controller,
        // via claims) can supply it explicitly.
        public string? OfficeId { get; set; }

        // Payable branch — single dropdown selection, "-1" = none selected (required)
        public string? PayableBranchId { get; set; }

        // "Detail" | "SubLedger" | "InterestCalculation"
        public string ReportType { get; set; } = "Detail";

        // Only used when ReportType == "InterestCalculation"
        public decimal InterestRate { get; set; } = 0;
        public decimal MinimumClosingBalance { get; set; } = 0;
    }

    // INFERRED — columns unknown; typical branch-statement shape.
    // Confirm against sp_5_43_GetIBTStatementBranchwise's actual SELECT list.
    public class IBTStatementBranchwiseRowDto
    {
        public string? VoucherDate { get; set; }
        public string? VoucherNo { get; set; }
        public string? Narration { get; set; }
        public decimal? DebitAmount { get; set; }
        public decimal? CreditAmount { get; set; }
        public decimal? Balance { get; set; }
    }

    // INFERRED — for the "SubLedger" (Ledger Wise) report
    public class IBTStatementBranchwiseLedgerGroupRowDto
    {
        public string? MainLedger { get; set; }
        public string? SubLedger { get; set; }
        public decimal? DebitAmount { get; set; }
        public decimal? CreditAmount { get; set; }
        public decimal? Balance { get; set; }
    }

    // INFERRED — for the "InterestCalculation" report
    public class IBTStatementBranchwiseInterestRowDto
    {
        public string? VoucherDate { get; set; }
        public string? VoucherNo { get; set; }
        public string? Narration { get; set; }
        public decimal? ClosingBalance { get; set; }
        public decimal? InterestAmount { get; set; }
    }

    public class IBTStatementBranchwiseData
    {
        public List<IBTStatementBranchwiseRowDto> DetailRows { get; set; } = [];
        public List<IBTStatementBranchwiseLedgerGroupRowDto> LedgerGroupRows { get; set; } = [];
        public List<IBTStatementBranchwiseInterestRowDto> InterestRows { get; set; } = [];

        public string? FromDateBs { get; set; }
        public string? ToDateBs { get; set; }
        public string? LoginBranchName { get; set; }
        public string? PayableBranchName { get; set; }
        public string ReportType { get; set; } = "Detail";
        public decimal InterestRate { get; set; }
        public decimal MinimumClosingBalance { get; set; }

        public int TotalRecords =>
            ReportType switch
            {
                "SubLedger" => LedgerGroupRows.Count,
                "InterestCalculation" => InterestRows.Count,
                _ => DetailRows.Count
            };
    }
}