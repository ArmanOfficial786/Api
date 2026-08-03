namespace NexgenCosysReport.Dtos.RequestDtos.MemberAccount
{

    public class SavingTypeWiseBalanceRequest
    {
        public string FromDate { get; set; }
        public string ToDate { get; set; }
        public string? BranchId { get; set; }
        public string BranchName { get; set; } = "All";
        public string? CollectionCenterId { get; set; }
        public string? MemberGroupId { get; set; }
        public string? CollectorId { get; set; }
        public string OrderBy { get; set; } = "SavingType";
        public bool IsNepali { get; set; } = false;
        public bool OpeningBalance { get; set; } = false;
        public bool PercentageBalance { get; set; } = false;
        public bool GroupByBranch { get; set; } = false;
        public bool GroupByCollectionCenter { get; set; } = false;
        public bool GroupByMemberGroup { get; set; } = false;
        public bool ViewCollector { get; set; } = false;
        public bool ViewDetail { get; set; } = true;
        public bool SameCompanyName { get; set; } = true;
        public bool VisualReport { get; set; } = false;
    }



    public class SavingTypeWiseBalanceRowDto
    {
        public string? SavingType { get; set; }
        public string? BranchName { get; set; }
        public string? CollectionCenterName { get; set; }
        public string? MemberGroupName { get; set; }
        public string? MemberName { get; set; }
        public string? MemberId { get; set; }
        public string? AccountNo { get; set; }
        public decimal Opening { get; set; }
        public decimal Deposit { get; set; }
        public decimal Withdraw { get; set; }
        public decimal Balance { get; set; }
        public decimal Closing { get; set; }
        public decimal Percentage { get; set; }  // percentage of total balance
        public int TransAccCount { get; set; }
    }

    public class SavingTypeWiseBalanceData
    {
        public List<SavingTypeWiseBalanceRowDto> Rows { get; set; } = new();
        public decimal TotalOpening { get; set; }
        public decimal TotalDeposit { get; set; }
        public decimal TotalWithdraw { get; set; }
        public decimal TotalBalance { get; set; }
        public decimal TotalClosing { get; set; }
        public int TotalRecords { get; set; }
    }
    public class SavingTypeWiseBalanceReqResponse
    {
    }
}
