namespace NexgenCosysReport.Dtos.RequestDtos.MemberAccount.SavingAcWiseReport
{
    public class MemberPenaltyDepositWithdrawRequest
    {
        public string FromDate { get; set; } = string.Empty;   // Nepali "yyyy/MM/dd"
        public string ToDate { get; set; } = string.Empty;     // Nepali "yyyy/MM/dd"
        public string BranchIds { get; set; } = "-1";          // comma-separated office ids
        public string BranchName { get; set; } = "All";
        public int TransactionType { get; set; } = 1;          // 1=Penalty, 2=Deposit, 3=Withdraw, 4=Balance
        public decimal Amount { get; set; } = 0m;
        public string OrderBy { get; set; } = "-1";            // MemberId | MemberName | Penalty | Deposit | Withdraw | Balance
        public bool SameCompanyName { get; set; } = true;
        public bool VisualReport { get; set; } = false;
    }

    public class MemberPenaltyDepositWithdrawRowDto
    {
        public string? MemberId { get; set; }
        public string? MemberName { get; set; }
        public string? Address { get; set; }
        public string? Contact { get; set; }
        public decimal PenaltyAmount { get; set; }
        public decimal DepositAmount { get; set; }
        public decimal WithdrawAmount { get; set; }
        public decimal BalanceAmount { get; set; }
    }

    public class MemberPenaltyDepositWithdrawData
    {
        public List<MemberPenaltyDepositWithdrawRowDto> Rows { get; set; } = new();
        public int TotalRecords { get; set; }
        public decimal TotalPenalty { get; set; }
        public decimal TotalDeposit { get; set; }
        public decimal TotalWithdraw { get; set; }
        public decimal TotalBalance { get; set; }
    }

    public class MemberPenaltyDepositeWithdrawReqRespone
    {
    }
}
