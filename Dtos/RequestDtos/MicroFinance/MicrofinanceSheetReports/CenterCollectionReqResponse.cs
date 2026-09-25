// Dtos/RequestDtos/Microfinance/MicrofinanceSheetReports/CenterCollectionRequestDto.cs
namespace NexgenCosysReport.Dtos.RequestDtos.Microfinance.MicrofinanceSheetReports
{
    public class CenterCollectionRequestDto
    {
        public string TillDateBs { get; set; } = string.Empty;
        public string? CollectionCenterId { get; set; }
        public bool VisualReport { get; set; } = false;
    }

    // Result set 1 — member-wise collection detail
    public class CenterCollectionRowDto
    {
        public string? CenterName { get; set; }
        public string? CenterAddress { get; set; }
        public string? GroupName { get; set; }
        public string? GroupAddress { get; set; }
        public string? MemberId { get; set; }
        public string? MemberName { get; set; }

        public string? SavingTypeName { get; set; }
        public string? AccountNo { get; set; }
        public decimal? LedgerBalance { get; set; }
        public decimal? NetBalance { get; set; }
        public decimal? DueAmount { get; set; }
        public string? AccountOpenDate { get; set; }

        public string? LoanTypeName { get; set; }
        public string? LoanAccountNo { get; set; }
        public decimal? LoanIssueAmount { get; set; }
        public decimal? RemainingPrinciple { get; set; }
        public decimal? DuePrinciple { get; set; }
        public decimal? DueInterest { get; set; }
        public decimal? DuePenalty { get; set; }
        public decimal? TotalInstallment { get; set; }
        public string? LoanIssueDate { get; set; }
    }

    // Result set 2 — saving type totals
    public class CenterCollectionSavingSummaryDto
    {
        public string? SavingTypeName { get; set; }
        public decimal? LedgerBalance { get; set; }
        public decimal? DueAmount { get; set; }
    }

    // Result set 3 — loan type totals
    public class CenterCollectionLoanSummaryDto
    {
        public string? LoanTypeName { get; set; }
        public decimal? LoanIssueAmount { get; set; }
        public decimal? RemainingPrinciple { get; set; }
        public decimal? DuePrinciple { get; set; }
        public decimal? DueInterest { get; set; }
        public decimal? DuePenalty { get; set; }
        public decimal? TotalInstallment { get; set; }
    }

    public class CenterCollectionData
    {
        public List<CenterCollectionRowDto> Rows { get; set; } = [];
        public List<CenterCollectionSavingSummaryDto> SavingSummary { get; set; } = [];
        public List<CenterCollectionLoanSummaryDto> LoanSummary { get; set; } = [];

        public int TotalRecords { get; set; }
        public decimal TotalLedgerBalance { get; set; }
        public decimal TotalNetBalance { get; set; }
        public decimal TotalSavingDue { get; set; }
        public decimal TotalLoanIssueAmount { get; set; }
        public decimal TotalRemainingPrinciple { get; set; }
        public decimal TotalDuePrinciple { get; set; }
        public decimal TotalDueInterest { get; set; }
        public decimal TotalDuePenalty { get; set; }
        public decimal TotalInstallment { get; set; }
        public decimal TotalCollection { get; set; }

        public string? TillDateBs { get; set; }
        public string? TillDateAd { get; set; }
        public string? CollectionCenterName { get; set; }
        public string? CollectionCenterAddress { get; set; }
        public string? PreviousMeetingDate { get; set; }
    }
}