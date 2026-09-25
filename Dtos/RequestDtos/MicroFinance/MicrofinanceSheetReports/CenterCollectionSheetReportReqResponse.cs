// Dtos/RequestDtos/Microfinance/MicrofinanceSheetReports/CenterCollectionSheetReportRequestDto.cs
namespace NexgenCosysReport.Dtos.RequestDtos.Microfinance.MicrofinanceSheetReports
{
    public class CenterCollectionSheetReportRequestDto
    {
        public string TillDateBs { get; set; } = string.Empty;
        public string? CollectionCenterId { get; set; }
        public bool VisualReport { get; set; } = false;
    }

    // Result set 1: Member + Loan summary (one row per member)
    public class CenterCollectionSheetMemberDto
    {
        public string? GroupCode { get; set; }
        public string? GroupName { get; set; }
        public long? MemMemberRegistrationId { get; set; }
        public string? MemberId { get; set; }
        public string? MemberName { get; set; }

        public decimal? Saving1 { get; set; }
        public decimal? Saving2 { get; set; }
        public decimal? Saving3 { get; set; }
        public decimal? Saving4 { get; set; }
        public decimal? Saving5 { get; set; }
        public decimal? Saving6 { get; set; }
        public decimal? Share { get; set; }

        public string? Loan1Date { get; set; }
        public decimal? Loan1 { get; set; }
        public string? Loan2Date { get; set; }
        public decimal? Loan2 { get; set; }
        public string? Loan3Date { get; set; }
        public decimal? Loan3 { get; set; }
        public string? Loan4Date { get; set; }
        public decimal? Loan4 { get; set; }
        public string? Loan5Date { get; set; }
        public decimal? Loan5 { get; set; }
        public string? Loan6Date { get; set; }
        public decimal? Loan6 { get; set; }
    }

    // Result set 2: Member Receivable
    public class CenterCollectionSheetReceivableDto
    {
        public string? GroupCode { get; set; }
        public string? GroupName { get; set; }
        public long? MemMemberRegistrationId { get; set; }
        public string? MemberId { get; set; }
        public string? MemberName { get; set; }
        public decimal? ReceivableAmt { get; set; }

        public decimal? SavingDue2 { get; set; }
        public decimal? SavingDue3 { get; set; }
        public decimal? SavingDue4 { get; set; }
        public decimal? SavingDue5 { get; set; }
        public decimal? SavingDue6 { get; set; }
        public decimal? OtherSaving { get; set; }

        public string? Loan1Inst { get; set; }
        public decimal? Loan1Principle { get; set; }
        public decimal? Loan1Intrest { get; set; }
        public decimal? Loan1Penalty { get; set; }
        public decimal? Loan1Total { get; set; }

        public string? Loan2Inst { get; set; }
        public decimal? Loan2Principle { get; set; }
        public decimal? Loan2Intrest { get; set; }
        public decimal? Loan2Penalty { get; set; }
        public decimal? Loan2Total { get; set; }

        public string? Loan3Inst { get; set; }
        public decimal? Loan3Principle { get; set; }
        public decimal? Loan3Intrest { get; set; }
        public decimal? Loan3Penalty { get; set; }
        public decimal? Loan3Total { get; set; }

        public string? Loan4Inst { get; set; }
        public decimal? Loan4Principle { get; set; }
        public decimal? Loan4Intrest { get; set; }
        public decimal? Loan4Penalty { get; set; }
        public decimal? Loan4Total { get; set; }

        public string? Loan5Inst { get; set; }
        public decimal? Loan5Principle { get; set; }
        public decimal? Loan5Intrest { get; set; }
        public decimal? Loan5Penalty { get; set; }
        public decimal? Loan5Total { get; set; }

        public string? Loan6Inst { get; set; }
        public decimal? Loan6Principle { get; set; }
        public decimal? Loan6Intrest { get; set; }
        public decimal? Loan6Penalty { get; set; }
        public decimal? Loan6Total { get; set; }
    }

    // Result set 3: Saving Summary
    public class CenterCollectionSheetSavingSummaryDto
    {
        public string? SavingTypeName { get; set; }
        public decimal? LedgerBalance { get; set; }
        public decimal? DueAmount { get; set; }
    }

    // Result set 4: Loan Summary
    public class CenterCollectionSheetLoanSummaryDto
    {
        public string? LoanTypeName { get; set; }
        public decimal? LoanIssueAmount { get; set; }
        public decimal? RemainingPrinciple { get; set; }
        public decimal? DuePrinciple { get; set; }
        public decimal? DueInterest { get; set; }
        public decimal? DuePenalty { get; set; }
        public decimal? TotalInstallment { get; set; }
    }

    // Result set 5: Evaluation Summary
    public class CenterCollectionSheetEvaluationDto
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

    public class CenterCollectionSheetReportData
    {
        public List<CenterCollectionSheetMemberDto> Members { get; set; } = [];
        public List<CenterCollectionSheetReceivableDto> Receivables { get; set; } = [];
        public List<CenterCollectionSheetSavingSummaryDto> SavingSummary { get; set; } = [];
        public List<CenterCollectionSheetLoanSummaryDto> LoanSummary { get; set; } = [];
        public CenterCollectionSheetEvaluationDto? Evaluation { get; set; }

        public int TotalMembers { get; set; }
        public decimal TotalSaving1 { get; set; }
        public decimal TotalSaving2 { get; set; }
        public decimal TotalSaving3 { get; set; }
        public decimal TotalSaving4 { get; set; }
        public decimal TotalSaving5 { get; set; }
        public decimal TotalSaving6 { get; set; }
        public decimal TotalShare { get; set; }
        public decimal TotalLoan1 { get; set; }
        public decimal TotalLoan2 { get; set; }
        public decimal TotalLoan3 { get; set; }
        public decimal TotalLoan4 { get; set; }
        public decimal TotalLoan5 { get; set; }
        public decimal TotalLoan6 { get; set; }
        public decimal TotalReceivable { get; set; }

        public string? TillDateBs { get; set; }
        public string? TillDateAd { get; set; }
        public string? CollectionCenterName { get; set; }
        public string? CollectionCenterAddress { get; set; }
        public string? NextMeetingDate { get; set; }
    }
}