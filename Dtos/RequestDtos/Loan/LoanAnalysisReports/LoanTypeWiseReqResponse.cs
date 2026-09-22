// Dtos/RequestDtos/Loan/LoanAnalysisReport/LoanTypeWiseRequestDto.cs
namespace NexgenCosysReport.Dtos.RequestDtos.Loan.LoanAnalysisReport
{
    public class LoanTypeWiseRequestDto
    {
        public string FromDateBs { get; set; } = string.Empty;
        public string ToDateBs { get; set; } = string.Empty;
        public string? BranchIds { get; set; }
        public string? LoanTypeId { get; set; } = "-1";
        public string? MemberGroupId { get; set; } = "-1";
        public string? CollectionCenterId { get; set; } = "-1";
        public bool EnableCollectionCenter { get; set; } = false;
        public string? CollectorId { get; set; } = "-1";
        public string LoanGuarantee { get; set; } = "-1";
        public int ShareType { get; set; } = -1;
        public bool SameCompanyName { get; set; } = true;
        public bool ShowOpeningBalance { get; set; } = false;
        public bool ShowDetail { get; set; } = true;
        public bool GroupByBranch { get; set; } = false;
        public bool GroupByMemberGroup { get; set; } = false;
        public bool ViewCollector { get; set; } = false;
        public string OrderBy { get; set; } = "-1";
        public bool VisualReport { get; set; } = false;
    }

    public class LoanTypeWiseRowDto
    {
        public string? MemberId { get; set; }
        public string? MemberIdFirst { get; set; }
        public decimal? MemberIdLast { get; set; }
        public string? FullName { get; set; }
        public string? CollectorName { get; set; }
        public string? LoanAccountNo { get; set; }
        public string? LoanTypeName { get; set; }
        public decimal? DisburseAmount { get; set; }
        public decimal? OpeningDisburseAmount { get; set; }
        public decimal? Repaid { get; set; }
        public decimal? BalanceAmount { get; set; }
        public decimal? OpeningPaid { get; set; }
        public decimal? OpeningBalance { get; set; }
        public decimal? ClosingBalance { get; set; }
        public DateTime? TransactionOn { get; set; }
        public DateTime? LoanIssueOn { get; set; }
        public string? CollectionCenterName { get; set; }
        public string? GroupName { get; set; }
        public string? OfficeName { get; set; }
    }

    public class LoanTypeWiseData
    {
        public List<LoanTypeWiseRowDto> Rows { get; set; } = [];
        public int TotalRecords { get; set; }
        public decimal TotalDisburseAmount { get; set; }
        public decimal TotalRepaid { get; set; }
        public decimal TotalBalanceAmount { get; set; }
        public decimal TotalOpeningBalance { get; set; }
        public decimal TotalClosingBalance { get; set; }
        public string? FromDateBs { get; set; }
        public string? ToDateBs { get; set; }
        public string? BranchName { get; set; }
        public string? LoanTypeName { get; set; }
        public string? MemberGroupName { get; set; }
        public string? CollectionCenterName { get; set; }
        public string? CollectorName { get; set; }
        public string? LoanGuarantee { get; set; }
        public string? OrderBy { get; set; }
        public bool ShowOpeningBalance { get; set; }
    }
}