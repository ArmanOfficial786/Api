// Dtos/RequestDtos/Microfinance/MicrofinanceSheetReports/CenterCollectionSheetSingleRequestDto.cs
namespace NexgenCosysReport.Dtos.RequestDtos.Microfinance.MicrofinanceSheetReports
{
    public class CenterCollectionSheetSingleRequestDto
    {
        public string TillDateBs { get; set; } = string.Empty;
        public string? CollectionCenterId { get; set; }
        public string ViewType { get; set; } = "T"; // "A" = Show Account No, "T" = Show Account Type
        public bool VisualReport { get; set; } = false;
    }

    // Result set 1 — main sheet row (combined saving + loan)
    public class CenterCollectionSheetSingleRowDto
    {
        public string? CenterName { get; set; }
        public string? CenterAddress { get; set; }
        public string? GroupName { get; set; }
        public string? GroupAddress { get; set; }
        public string? MemberId { get; set; }
        public string? MemberName { get; set; }

        // Saving columns
        public string? SavingTypeName { get; set; }
        public string? SAccountNo { get; set; }
        public decimal? SLedgerBalance { get; set; }
        public decimal? SNetBalance { get; set; }
        public decimal? SDueAmount { get; set; }
        public decimal? SGrandTotal { get; set; }
        public string? SAccountOpenDate { get; set; }

        // Loan columns
        public decimal? LoanIssueAmount { get; set; }
        public string? LoanTypeName { get; set; }
        public decimal? LRemainingPrinciple { get; set; }
        public decimal? LDuePrinciple { get; set; }
        public decimal? LDueInterest { get; set; }
        public decimal? LDuePenalty { get; set; }
        public string? LoanAccountNo { get; set; }
        public string? LInstallment { get; set; }
        public decimal? LTotalInstallment { get; set; }
        public string? LoanIssueDate { get; set; }

        // Other amount (yearly due for member's first row only)
        public decimal? OtherAmount { get; set; }
    }

    // Result set 2 — loan type summary
    public class CenterCollectionSheetSingleLoanSummaryDto
    {
        public string? LoanTypeName { get; set; }
        public decimal? LoanIssueAmount { get; set; }
        public decimal? RemainingPrinciple { get; set; }
        public decimal? DuePrinciple { get; set; }
        public decimal? DueInterest { get; set; }
        public decimal? DuePenalty { get; set; }
        public decimal? TotalInstallment { get; set; }
    }

    // Result set 3 — saving type summary
    public class CenterCollectionSheetSingleSavingSummaryDto
    {
        public string? SavingTypeName { get; set; }
        public decimal? LedgerBalance { get; set; }
        public decimal? DueAmount { get; set; }
    }

    // Result set 4 — center evaluation
    public class CenterCollectionSheetSingleEvaluationDto
    {
        public decimal? TotalGroup { get; set; }
        public decimal? TotalActiveGroup { get; set; }
        public decimal? TotalMember { get; set; }
        public decimal? TotalActiveMember { get; set; }
        public decimal? TotalSaving { get; set; }
        public decimal? TotalLoan { get; set; }
        public decimal? TotalSavingMember { get; set; }
        public decimal? TotalLoanMember { get; set; }
        public decimal? HitKoshBalance { get; set; }
        public decimal? TotalCollection { get; set; }
    }

    public class CenterCollectionSheetSingleData
    {
        public List<CenterCollectionSheetSingleRowDto> Rows { get; set; } = [];
        public List<CenterCollectionSheetSingleLoanSummaryDto> LoanSummary { get; set; } = [];
        public List<CenterCollectionSheetSingleSavingSummaryDto> SavingSummary { get; set; } = [];
        public CenterCollectionSheetSingleEvaluationDto? Evaluation { get; set; }

        public int TotalRecords { get; set; }
        public decimal TotalSLedgerBalance { get; set; }
        public decimal TotalSNetBalance { get; set; }
        public decimal TotalSDueAmount { get; set; }
        public decimal TotalSGrandTotal { get; set; }
        public decimal TotalLoanIssueAmount { get; set; }
        public decimal TotalLRemainingPrinciple { get; set; }
        public decimal TotalLDuePrinciple { get; set; }
        public decimal TotalLDueInterest { get; set; }
        public decimal TotalLDuePenalty { get; set; }
        public decimal TotalLTotalInstallment { get; set; }
        public decimal TotalOtherAmount { get; set; }

        public string? TillDateBs { get; set; }
        public string? TillDateAd { get; set; }
        public string? CollectionCenterName { get; set; }
        public string? CollectionCenterAddress { get; set; }
        public string? NextMeetingDate { get; set; }
        public string? ViewType { get; set; }
        public string? ViewTypeName { get; set; }
    }
}