// Dtos/RequestDtos/AccountOperation/OthersReport/CashAndBankBalanceRequestDto.cs
namespace NexgenCosysReport.Dtos.RequestDtos.Account.OthersReport
{
    public class CashAndBankBalanceRequestDto
    {
        public string TillDateBs { get; set; } = string.Empty;
        public string? BranchId { get; set; }
        public string OrderBy { get; set; } = "-1";
        public bool NepaliReport { get; set; } = false;
    }

    // Matches #tempBank columns exactly:
    // LedgerHead, LedgerHeadInNepali, BankName, BankNameInNepali,
    // OpeningBankBalance, ClosingBankBalance, TodayBankDr, TodayBankCr, TodayBankBalance
    public class CashAndBankBalanceBankRowDto
    {
        public string? LedgerHead { get; set; }
        public string? LedgerHeadInNepali { get; set; }
        public string? BankName { get; set; }
        public string? BankNameInNepali { get; set; }
        public decimal? OpeningBankBalance { get; set; }
        public decimal? ClosingBankBalance { get; set; }
        public decimal? TodayBankDr { get; set; }
        public decimal? TodayBankCr { get; set; }
        public decimal? TodayBankBalance { get; set; }
    }

    // Matches #FinalCash columns exactly:
    // CashName, OpeningCashBalance, ClosingCashBalance, TodayCashDr, TodayCashCr, TodayCashBalance
    public class CashAndBankBalanceCashRowDto
    {
        public string? CashName { get; set; }
        public decimal? OpeningCashBalance { get; set; }
        public decimal? ClosingCashBalance { get; set; }
        public decimal? TodayCashDr { get; set; }
        public decimal? TodayCashCr { get; set; }
        public decimal? TodayCashBalance { get; set; }
    }

    // ASSUMPTION — sp_6_56_GetCashAndBankBalanceTeller wasn't provided.
    // Columns inferred from the report image (Teller Name | Cash In | Cash Out | Cash Balance).
    // Please confirm/share the SP so this can be verified.
    public class CashAndBankBalanceTellerRowDto
    {
        public string? TellerName { get; set; }
        public decimal? CashIn { get; set; }
        public decimal? CashOut { get; set; }
        public decimal? CashBalance { get; set; }
    }

    public class CashAndBankBalanceData
    {
        public List<CashAndBankBalanceBankRowDto> BankRows { get; set; } = [];
        public List<CashAndBankBalanceTellerRowDto> TellerRows { get; set; } = [];
        public List<CashAndBankBalanceCashRowDto> CashRows { get; set; } = [];

        public decimal BankBalanceOutput { get; set; }
        public decimal CashBalanceOutput { get; set; }

        public decimal TellerBalance { get; set; }
        public decimal FinalBankAmount => BankBalanceOutput;
        public decimal FinalCashAmount => CashBalanceOutput + TellerBalance;

        public string? TillDateBs { get; set; }
        public string? BranchName { get; set; }
        public string? OrderBy { get; set; }
        public bool NepaliReport { get; set; }
    }
}