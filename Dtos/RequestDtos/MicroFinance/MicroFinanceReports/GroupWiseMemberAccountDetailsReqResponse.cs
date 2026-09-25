// Dtos/RequestDtos/Microfinance/MicrofinanceReport/GroupWiseMemberAccountDetailsRequestDto.cs
namespace NexgenCosysReport.Dtos.RequestDtos.MicroFinance.MicroFinanceReports
{
    public class GroupWiseMemberAccountDetailsRequestDto
    {
        public string FromDateBs { get; set; } = string.Empty;
        public string ToDateBs { get; set; } = string.Empty;
        public string? BranchIds { get; set; }
        public string? CollectionCenterIds { get; set; } = "-1";
        public string? MemberGroupId { get; set; } = "-1";
        public string OrderBy { get; set; } = "-1";
        public bool GroupByCollectionCenter { get; set; } = false;
        public bool GroupByMemberGroup { get; set; } = false;
        public bool GroupByBranch { get; set; } = false;
        public bool SameCompanyName { get; set; } = true;
        public bool VisualReport { get; set; } = false;
    }

    public class GroupWiseMemberAccountDetailsRowDto
    {
        public string? OfficeName { get; set; }
        public string? CollectionCenterName { get; set; }
        public string? GroupCode { get; set; }
        public string? GroupName { get; set; }
        public string? MemberId { get; set; }
        public string? MemberName { get; set; }
        public string? AccountNo { get; set; }
        public decimal? Balance { get; set; }
    }

    public class GroupWiseMemberAccountDetailsData
    {
        public List<GroupWiseMemberAccountDetailsRowDto> Rows { get; set; } = [];
        public int TotalRecords { get; set; }
        public decimal TotalBalance { get; set; }
        public string? FromDateBs { get; set; }
        public string? ToDateBs { get; set; }
        public string? FromDateAd { get; set; }
        public string? ToDateAd { get; set; }
        public string? BranchName { get; set; }
        public string? CollectionCenterName { get; set; }
        public string? MemberGroupName { get; set; }
        public string? OrderBy { get; set; }
        public bool GroupByCollectionCenter { get; set; }
        public bool GroupByMemberGroup { get; set; }
        public bool GroupByBranch { get; set; }
    }
}