
namespace NexgenCosysReport.Dtos.RequestDtos.Loan.LoanAnalysisReport
{
    public class LoanAgingRiskCalculationRequestDto
    {
        public string TillDateBs { get; set; } = string.Empty;
        public string? BranchIds { get; set; }
        public string? MemberGroupId { get; set; }
        public string PenaltyType { get; set; } = "S";
        public string OrderBy { get; set; } = "-1";
        public bool VisualReport { get; set; } = false;
    }

    public class LoanAgingRiskCalculationRowDto
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
        public decimal? Arrear { get; set; }
        public decimal? LoanOverDue { get; set; }
        public decimal? Frm1to365 { get; set; }
        public decimal? Grtrhan365 { get; set; }
        public int? Noofdays { get; set; }
        public decimal? OverDue { get; set; }
        public decimal? RescheduleAmt { get; set; }
        public DateTime? DefaultDateOn { get; set; }
        public string? DefaultDateBs { get; set; }
        public string? LoanODorNormal { get; set; }
        public string? LastPaymentDate { get; set; }
    }

    public class LoanAgingRiskCalculationData
    {
        public List<LoanAgingRiskCalculationRowDto> Rows { get; set; } = [];
        public int TotalRecords { get; set; }
        public decimal TotalLoanIssueAmount { get; set; }
        public decimal TotalBalanceAmount { get; set; }
        public decimal TotalOverDue { get; set; }
        public decimal TotalGoodLoan { get; set; }
        public decimal TotalArrear { get; set; }
        public decimal TotalFrm1to365 { get; set; }
        public decimal TotalGrtthan365 { get; set; }
        public string? TillDateBs { get; set; }
        public string? BranchName { get; set; }
        public string? MemberGroupName { get; set; }
        public string? PenaltyType { get; set; }
        public string? PenaltyTypeName { get; set; }
        public string? OrderBy { get; set; }
    }
}