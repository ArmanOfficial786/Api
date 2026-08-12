// Dtos/RequestDtos/AccountOperation/DepositWithdrawMaxAmountRangeRequest.cs
namespace NexgenCosysReport.Dtos.RequestDtos.MemberAccount
{
    public class DepositWithdrawMaxAmountRangeRequest
    {
        public string? FromDate { get; set; }    // Nepali "yyyy/MM/dd"
        public string? ToDate { get; set; }     // Nepali "yyyy/MM/dd"
        public string BranchIds { get; set; } = "-1";          // comma-separated office ids
        public string BranchName { get; set; } = "All";
        public int TransactionType { get; set; } = 1;          // 1=Deposit, 2=Withdraw, 3=Both
        public decimal Amount { get; set; } = 1000000;
        public string OrderBy { get; set; } = "-1";            // MemberId | MemberName | Deposit | Withdraw | Account | Date
        public bool SameCompanyName { get; set; } = true;
        public bool VisualReport { get; set; } = false;
    }
    public class DepositWithdrawMaxAmountRowDto
    {
        public string? MemberId { get; set; }
        public string? MemberName { get; set; }
        public string? AccountNo { get; set; }
        public string? TransactionDateBs { get; set; }
        public DateTime? TransactionDate { get; set; }
        public decimal DepositAmount { get; set; }
        public decimal WithdrawAmount { get; set; }
        public string? Particulars { get; set; }
    }

    public class DepositWithdrawMaxAmountData
    {
        public List<DepositWithdrawMaxAmountRowDto> Rows { get; set; } = new();
        public int TotalRecords { get; set; }
        public decimal TotalDeposit { get; set; }
        public decimal TotalWithdraw { get; set; }
    }
}

