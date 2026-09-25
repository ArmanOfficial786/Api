// Dtos/RequestDtos/Microfinance/MicrofinanceReport/SavingTypeWiseBalanceRequestDto.cs
namespace NexgenCosysReport.Dtos.RequestDtos.MicroFinance.MicroFinanceReports
{
    public class SavingTypeWiseBalanceRequestDto
    {
        public string FromDateBs { get; set; } = string.Empty;
        public string ToDateBs { get; set; } = string.Empty;
        public string? BranchIds { get; set; }
        public string? CollectionCenterIds { get; set; } = "-1";
        public string? MemberGroupId { get; set; } = "-1";
        public string? CollectorId { get; set; } = "-1";
        public string OrderBy { get; set; } = "SavingType";
        public bool SameCompanyName { get; set; } = true;
        public bool ShowOpeningBalance { get; set; } = false;
        public bool ShowPercentage { get; set; } = false;
        public bool ShowDetail { get; set; } = true;
        public bool GroupByBranch { get; set; } = false;
        public bool GroupByCollectionCenter { get; set; } = false;
        public bool GroupByMemberGroup { get; set; } = false;
        public bool ViewCollector { get; set; } = false;
        public bool NepaliReport { get; set; } = false;
        public bool VisualReport { get; set; } = false;
    }

    public class SavingTypeWiseBalanceRowDto
    {
        public string? OfficeName { get; set; }
        public string? GroupName { get; set; }
        public string? CenterName { get; set; }
        public string? CollectorName { get; set; }
        public int? Count { get; set; }
        public string? SavingType { get; set; }
        public decimal? Opening { get; set; }
        public decimal? Deposit { get; set; }
        public decimal? Withdraw { get; set; }
        public decimal? Balance { get; set; }
        public decimal? Closing { get; set; }
    }

    public class SavingTypeWiseBalanceData
    {
        public List<SavingTypeWiseBalanceRowDto> Rows { get; set; } = [];
        public int TotalRecords { get; set; }
        public int TotalAccountCount { get; set; }
        public decimal TotalOpening { get; set; }
        public decimal TotalDeposit { get; set; }
        public decimal TotalWithdraw { get; set; }
        public decimal TotalBalance { get; set; }
        public decimal TotalClosing { get; set; }
        public string? FromDateBs { get; set; }
        public string? ToDateBs { get; set; }
        public string? FromDateAd { get; set; }
        public string? ToDateAd { get; set; }
        public string? BranchName { get; set; }
        public string? CollectionCenterName { get; set; }
        public string? MemberGroupName { get; set; }
        public string? CollectorName { get; set; }
        public string? OrderBy { get; set; }
        public bool SameCompanyName { get; set; }
        public bool ShowOpeningBalance { get; set; }
        public bool ShowPercentage { get; set; }
        public bool ShowDetail { get; set; }
        public bool GroupByBranch { get; set; }
        public bool GroupByCollectionCenter { get; set; }
        public bool GroupByMemberGroup { get; set; }
        public bool ViewCollector { get; set; }
    }
}