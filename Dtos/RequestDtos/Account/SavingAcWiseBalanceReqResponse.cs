namespace NexgenCosysReport.Dtos.RequestDtos.Account
{
    public class SavingAcWiseBalanceReqResponse
    {
    }

    public class SavingAcWiseBalanceRequest
    {
        public string TillDate { get; set; } = string.Empty;

        public long DepositId { get; set; } = -1;

        public string? BranchSelected { get; set; }
        public string? BranchName { get; set; }
        public string? Status { get; set; }

        public string OrderBy { get; set; } = "-1";

        public long CollectorId { get; set; } = -1;

        public long MemberGroupId { get; set; } = -1;

        public string CollectionCenterId { get; set; } = "-1";

        public bool EnableCollectionCenter { get; set; }

        public bool EnableGroup { get; set; }

        public bool SameCompanyName { get; set; }
    }
    public class SavingAcWiseBalanceResponse
    {
        public string? BranchName { get; set; }
        public string? MemberName { get; set; }
        public string? MemberId { get; set; }
        public string? AccountNo { get; set; }
        public string? InterestType { get; set; }
        public string? AccountOpenOnBS { get; set; }
        public decimal Deposit { get; set; }
        public decimal Withdraw { get; set; }
        public decimal Balance { get; set; }
        public decimal InterestRate { get; set; }
        public string? SavingType { get; set; }

    }
}
