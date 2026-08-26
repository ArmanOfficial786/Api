namespace NexgenCosysReport.Dtos.RequestDtos.MemberAccount.SavingAcWiseReport
{

    public class SavingTypeWiseIndividualBalanceRequest
    {
        public string FromDate { get; set; } = string.Empty;   // Nepali "yyyy/MM/dd"
        public string ToDate { get; set; } = string.Empty;
        public string? BranchId { get; set; }                 // comma-separated
        public string BranchName { get; set; } = "All";
        public string? CollectionCenterId { get; set; }        // single or multiple
        public string? MemberGroupId { get; set; }
        public string? CollectorId { get; set; }
        public string OrderBy { get; set; } = "SavingType";    // SavingType, Deposit, Withdraw, Balance
        public bool OpeningBalance { get; set; } = false;
        public bool PercentageBalance { get; set; } = false;
        public bool GroupByBranch { get; set; } = false;
        public bool GroupByCollectionCenter { get; set; } = false;
        public bool GroupByMemberGroup { get; set; } = false;
        public bool ViewDetail { get; set; } = true;
        public bool SameCompanyName { get; set; } = true;
        public bool VisualReport { get; set; } = false;
    }

    public class SavingTypeWiseIndividualBalanceRowDto
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
    }

    public class SavingTypeWiseIndividualBalanceData
    {
        public List<SavingTypeWiseIndividualBalanceRowDto> Rows { get; set; } = new();
        public decimal TotalOpening { get; set; }
        public decimal TotalDeposit { get; set; }
        public decimal TotalWithdraw { get; set; }
        public decimal TotalBalance { get; set; }
        public decimal TotalClosing { get; set; }
        public int TotalRecords { get; set; }
    }




    public class SavingTypeWiseIndividualBalanceReqResponse
    {
    }
}
