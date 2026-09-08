// Dtos/RequestDtos/AccountOperation/OthersReport/CashAndBankBalanceRequestDto.cs
namespace NexgenCosysReport.Dtos.RequestDtos.Account.OthersReport
{
    public class CashAndBankBalanceRequestDto
    {
        public string TillDateBs { get; set; } = string.Empty;
        public string? BranchId { get; set; }       // single office id, "-1" = All
        public string OrderBy { get; set; } = "-1";
        public bool NepaliReport { get; set; } = false; // picks between English/Nepali view templates
    }

    // INFERRED columns — confirm against sp_6_56_GetNepaliCashAndBankBalanceBank's actual SELECT list
    public class CashAndBankBalanceBankRowDto
    {
        public string? MainLedger { get; set; }
        public string? SubLedger { get; set; }
        public decimal? DebitAmount { get; set; }
        public decimal? CreditAmount { get; set; }
        public decimal? Balance { get; set; }
    }

    // Confirmed shape: CashBalance column is used directly (dt.Compute("Sum(CashBalance)"))
    public class CashAndBankBalanceTellerRowDto
    {
        public string? TellerName { get; set; }
        public string? OfficeName { get; set; }
        public decimal? CashBalance { get; set; }
    }

    // INFERRED columns — confirm against sp_6_56_GetCashAndBankBalanceCash's actual SELECT list
    public class CashAndBankBalanceCashRowDto
    {
        public string? MainLedger { get; set; }
        public string? SubLedger { get; set; }
        public decimal? DebitAmount { get; set; }
        public decimal? CreditAmount { get; set; }
        public decimal? Balance { get; set; }
    }

    public class CashAndBankBalanceData
    {
        public List<CashAndBankBalanceBankRowDto> BankRows { get; set; } = [];
        public List<CashAndBankBalanceTellerRowDto> TellerRows { get; set; } = [];
        public List<CashAndBankBalanceCashRowDto> CashRows { get; set; } = [];

        // Raw output parameters from the two balance SPs
        public decimal BankBalanceOutput { get; set; }
        public decimal CashBalanceOutput { get; set; }

        // Computed exactly as GetCashAndBankBalanceDetails:
        //   TellerBalance = SUM(TellerRows.CashBalance)
        //   FinalBank      = BankBalanceOutput
        //   FinalCash      = CashBalanceOutput + TellerBalance
        public decimal TellerBalance { get; set; }
        public decimal FinalBankAmount => BankBalanceOutput;
        public decimal FinalCashAmount => CashBalanceOutput + TellerBalance;

        public string? TillDateBs { get; set; }
        public string? BranchName { get; set; }
        public string? OrderBy { get; set; }
        public bool NepaliReport { get; set; }
    }
}