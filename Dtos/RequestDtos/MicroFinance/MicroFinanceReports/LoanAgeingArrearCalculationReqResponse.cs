// Dtos/RequestDtos/Microfinance/MicrofinanceReport/LoanAgeingArrearCalculationRequestDto.cs
namespace NexgenCosysReport.Dtos.RequestDtos.MicroFinance.MicroFinanceReports
{
    public class LoanAgeingArrearCalculationRequestDto
    {
        public string TillDateBs { get; set; } = string.Empty;
        public string? BranchIds { get; set; }
        public string? CollectionCenterIds { get; set; } = "-1";
        public string? CollectorId { get; set; } = "-1";
        public string? MemberGroupId { get; set; } = "-1";
        public string PenaltyType { get; set; } = "S";
        public string OrderBy { get; set; } = "-1";
        public bool Enable1To30Days { get; set; } = false;
        public bool GroupByCollectionCenter { get; set; } = false;
        public bool VisualReport { get; set; } = false;
    }

    public class LoanAgeingArrearCalculationRowDto
    {
        public string? MemberId { get; set; }
        public string? FullName { get; set; }
        public string? ContactAddress { get; set; }
        public string? ContactNo { get; set; }
        public string? LoanAccountNo { get; set; }
        public string? LoanTypeName { get; set; }
        public string? LoanODorNormal { get; set; }
        public decimal? InterestRate { get; set; }
        public string? PaymentType { get; set; }
        public string? PaymentMode { get; set; }
        public decimal? InstallamentAmount { get; set; }
        public long? LmtLoanIssueId { get; set; }
        public decimal? LoanIssueAmount { get; set; }
        public string? LoanIssueOnBs { get; set; }
        public DateTime? LoanIssueOn { get; set; }
        public string? MaturityOnBs { get; set; }
        public DateTime? MaturityOn { get; set; }
        public decimal? PrinciplePaidAmt { get; set; }
        public decimal? TotalPrincipalAmount { get; set; }
        public decimal? RemainingPrinciplefromSche { get; set; }
        public int? TotalInstallmentNo { get; set; }
        public int? InstallmentNo { get; set; }
        public int? DueDays { get; set; }
        public decimal? OverDue { get; set; }
        public DateTime? DefaultDateOn { get; set; }
        public string? DefaultDateBs { get; set; }
        public string? LastPaymentDate { get; set; }
        public string? LastInstallmentDate { get; set; }
        public decimal? Between0to30Days { get; set; }
        public decimal? Between31to365Days { get; set; }
        public decimal? GraterThan365Days { get; set; }
        public int? DefaulterDay { get; set; }
        public decimal? LoanBalance { get; set; }
        public decimal? Goodloan { get; set; }
    }

    public class LoanAgeingArrearCalculationData
    {
        public List<LoanAgeingArrearCalculationRowDto> Rows { get; set; } = [];
        public int TotalRecords { get; set; }
        public decimal TotalLoanIssueAmount { get; set; }
        public decimal TotalPrinciplePaidAmount { get; set; }
        public decimal TotalLoanBalance { get; set; }
        public decimal TotalGoodLoan { get; set; }
        public decimal TotalBetween0to30Days { get; set; }
        public decimal TotalBetween31to365Days { get; set; }
        public decimal TotalGraterThan365Days { get; set; }
        public decimal TotalOverdue { get; set; }
        public string? TillDateBs { get; set; }
        public string? TillDateAd { get; set; }
        public string? BranchName { get; set; }
        public string? CollectionCenterName { get; set; }
        public string? CollectorName { get; set; }
        public string? MemberGroupName { get; set; }
        public string? PenaltyType { get; set; }
        public string? PenaltyTypeName { get; set; }
        public string? OrderBy { get; set; }
        public bool Enable1To30Days { get; set; }
        public bool GroupByCollectionCenter { get; set; }
    }
}