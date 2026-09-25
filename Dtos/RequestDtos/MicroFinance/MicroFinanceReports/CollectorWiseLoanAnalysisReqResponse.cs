// Dtos/RequestDtos/Microfinance/MicrofinanceReport/CollectorWiseLoanAnalysisRequestDto.cs
namespace NexgenCosysReport.Dtos.RequestDtos.MicroFinance.MicroFinanceReports
{
    public class CollectorWiseLoanAnalysisRequestDto
    {
        public string FromDateBs { get; set; } = string.Empty;
        public string ToDateBs { get; set; } = string.Empty;
        public string? BranchIds { get; set; }
        public string? CollectorId { get; set; } = "-1";
        public string? CollectionCenterIds { get; set; } = "-1";
        public string PenaltyType { get; set; } = "S";
        public string OrderBy { get; set; } = "-1";
        public bool GroupByCollectionCenter { get; set; } = false;
        public string ReportMode { get; set; } = "1";
        public bool VisualReport { get; set; } = false;
    }

    public class CollectorWiseLoanAnalysisRowDto
    {
        public long? LmtLoanIssueId { get; set; }
        public string? MemberId { get; set; }
        public string? FullName { get; set; }
        public string? LoanAccountNo { get; set; }
        public string? ContactAddress { get; set; }
        public string? ContactNo { get; set; }
        public string? LoanType { get; set; }
        public decimal? LoanIssueAmount { get; set; }
        public string? LoanIssueOnBs { get; set; }
        public string? MaturityOnBs { get; set; }
        public DateTime? MaturityOn { get; set; }
        public decimal? PaidAmount { get; set; }
        public decimal? BalanceAmount { get; set; }
        public decimal? BalanceAmountFromSch { get; set; }
        public decimal? Goodloan { get; set; }
        public decimal? GoodloanPercent { get; set; }
        public decimal? Arrear { get; set; }
        public decimal? ArrearPercent { get; set; }
        public decimal? ArrearFor0 { get; set; }
        public decimal? Arrearfrm1to365 { get; set; }
        public decimal? Arrearfrm1to365Percent { get; set; }
        public decimal? Arreargrtthan365 { get; set; }
        public decimal? Arreargrtthan365Percent { get; set; }
        public decimal? GoodAmt { get; set; }
        public decimal? frm1to365 { get; set; }
        public decimal? grtthan365 { get; set; }
        public decimal? TotalProvision { get; set; }
        public decimal? Noofdays { get; set; }
        public decimal? OverDue { get; set; }
        public decimal? rescheduleAmt { get; set; }
        public DateTime? DefaultDateOn { get; set; }
        public string? DefaultDateBs { get; set; }
        public string? LoanODorNormal { get; set; }
        public string? CollectionCenterName { get; set; }
        public string? PaymentMode { get; set; }
        public string? CollectorName { get; set; }
        public decimal? OpeningDisburse { get; set; }
        public decimal? OpeningPaid { get; set; }
        public decimal? OpeningBalance { get; set; }
        public decimal? ClosingBalance { get; set; }
        public decimal? CollectorNamePercent { get; set; }
        public decimal? CenterPercent { get; set; }
        public int? OpeningActiveLoanCount { get; set; }
        public int? CurrentActiveLoanCount { get; set; }
        public int? TotalActiveLoanCount { get; set; }
    }

    public class CollectorWiseLoanAnalysisData
    {
        public List<CollectorWiseLoanAnalysisRowDto> Rows { get; set; } = [];
        public int TotalRecords { get; set; }
        public decimal TotalLoanIssueAmount { get; set; }
        public decimal TotalPaidAmount { get; set; }
        public decimal TotalBalanceAmount { get; set; }
        public decimal TotalGoodloan { get; set; }
        public decimal TotalArrear { get; set; }
        public decimal TotalArrearfrm1to365 { get; set; }
        public decimal TotalArreargrtthan365 { get; set; }
        public decimal TotalOverDue { get; set; }
        public decimal TotalProvision { get; set; }
        public decimal TotalOpeningDisburse { get; set; }
        public decimal TotalOpeningPaid { get; set; }
        public decimal TotalOpeningBalance { get; set; }
        public decimal TotalClosingBalance { get; set; }
        public int TotalActiveLoanCount { get; set; }
        public string? FromDateBs { get; set; }
        public string? ToDateBs { get; set; }
        public string? FromDateAd { get; set; }
        public string? ToDateAd { get; set; }
        public string? BranchName { get; set; }
        public string? CollectorName { get; set; }
        public string? CollectionCenterName { get; set; }
        public string? PenaltyType { get; set; }
        public string? PenaltyTypeName { get; set; }
        public string? OrderBy { get; set; }
        public bool GroupByCollectionCenter { get; set; }
        public string? ReportMode { get; set; }
        public string? ReportModeName { get; set; }
    }
}