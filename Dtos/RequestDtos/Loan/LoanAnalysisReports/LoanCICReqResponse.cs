// Dtos/RequestDtos/Loan/LoanAnalysisReport/LoanCICRequestDto.cs
namespace NexgenCosysReport.Dtos.RequestDtos.Loan.LoanAnalysisReport
{
    public class LoanCICRequestDto
    {
        public string TillDateBs { get; set; } = string.Empty;
        public string? BranchIds { get; set; }
        public string? LoanTypeId { get; set; } = "-1";
        public string ProvisionType { get; set; } = "S"; // S = Schedule Wise, R = Remaining Principle, A = After Maturity
        public bool Enable1To30Days { get; set; } = false;
        public string OrderBy { get; set; } = "-1";
        public bool VisualReport { get; set; } = false;
    }

    public class LoanCICRowDto
    {
        public string? MemberId { get; set; }
        public string? FullName { get; set; }
        public string? PermanentAddessDetail { get; set; }
        public string? TemporaryAddressDetail { get; set; }
        public string? ContactNo { get; set; }
        public string? CitizenshipNo { get; set; }
        public string? CitizenShipIssuedOnBs { get; set; }
        public string? CitizenShipIssuedDistrict { get; set; }
        public string? LoanTypeName { get; set; }
        public long? LmtLoanIssueId { get; set; }
        public string? LoanAccountNo { get; set; }
        public string? LoanIssueOnBs { get; set; }
        public string? MaturityOnBs { get; set; }
        public decimal? LoanIssueAmount { get; set; }
        public int? NoOfDays { get; set; }
        public decimal? PriPaid { get; set; }
        public decimal? PriBalance { get; set; }
        public decimal? IntBalance { get; set; }
        public decimal? PenaltyBalance { get; set; }
        public decimal? TotalBalance { get; set; }
        public decimal? PriBalanceAtLastPaymenton { get; set; }
        public decimal? InterestRate { get; set; }
        public string? LoanPaymentTypeCode { get; set; }
        public decimal? InterestReceivableAmount { get; set; }
        public decimal? PenaltyReceivableAmount { get; set; }
        public DateTime? LastPaymentOnAD { get; set; }
        public int? LmtLoanPaymentTypeId { get; set; }
        public int? LmtPenaltyTypeId { get; set; }
        public string? CollectionCenterName { get; set; }
        public decimal? IssueAmounAtLastPaymenton { get; set; }
        public decimal? defaulterAmount { get; set; }
        public int? defaulterCount { get; set; }
        public decimal? GoodLoan { get; set; }
        public decimal? LoanRisk30 { get; set; }
        public decimal? LoanRisk31to365 { get; set; }
        public decimal? LoanRisk1to365 { get; set; }
        public decimal? LoanRiskGreaterThan365 { get; set; }
        public string? Guaranteer { get; set; }
        public string? GuaranteerCitizenshipNo { get; set; }
    }

    public class LoanCICData
    {
        public List<LoanCICRowDto> Rows { get; set; } = [];
        public int TotalRecords { get; set; }
        public decimal TotalLoanIssueAmount { get; set; }
        public decimal TotalPriPaid { get; set; }
        public decimal TotalPriBalance { get; set; }
        public decimal TotalIntBalance { get; set; }
        public decimal TotalPenaltyBalance { get; set; }
        public decimal TotalBalance { get; set; }
        public decimal TotalGoodLoan { get; set; }
        public decimal TotalLoanRisk30 { get; set; }
        public decimal TotalLoanRisk31to365 { get; set; }
        public decimal TotalLoanRisk1to365 { get; set; }
        public decimal TotalLoanRiskGreaterThan365 { get; set; }
        public string? TillDateBs { get; set; }
        public string? TillDateAd { get; set; }
        public string? BranchName { get; set; }
        public string? LoanTypeName { get; set; }
        public string? ProvisionType { get; set; }
        public string? ProvisionTypeName { get; set; }
        public string? OrderBy { get; set; }
        public bool Enable1To30Days { get; set; }
    }
}