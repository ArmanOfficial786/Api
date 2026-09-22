// Dtos/RequestDtos/Loan/LoanAnalysisReport/LoanRegisterRequestDto.cs
namespace NexgenCosysReport.Dtos.RequestDtos.Loan.LoanAnalysisReport
{
    public class LoanRegisterRequestDto
    {
        public string TillDateBs { get; set; } = string.Empty;
        public string? BranchIds { get; set; }
        public string? LoanTypeMasterId { get; set; } = "-1";
        public string? OrderBy { get; set; } = "-1";
        public string? CollectionCenterIds { get; set; } = "-1";
        public bool EnableCollectionCenter { get; set; } = false;
        public string? CollectorId { get; set; } = "-1";
        public string SelectLoan { get; set; } = "-1"; // -1 = All, N = Normal, O = OverDraft
        public bool NepaliReport { get; set; } = false;
        public bool VisualReport { get; set; } = false;
    }

    public class LoanRegisterRowDto
    {
        public string? MemberId { get; set; }
        public string? FullName { get; set; }
        public string? ContactAddress { get; set; }
        public string? ContactNo { get; set; }
        public string? LoanTypeName { get; set; }
        public string? LoanAccountNo { get; set; }
        public string? LoanIssueOnBs { get; set; }
        public string? MaturityOnBs { get; set; }
        public decimal? LoanIssueAmount { get; set; }
        public decimal? InterestRate { get; set; }
        public string? LoanPaymentTypeCode { get; set; }
        public decimal? InterestReceivableAmount { get; set; }
        public decimal? PenaltyReceivableAmount { get; set; }
        public DateTime? LastPaymentOnAD { get; set; }
        public int? LmtLoanPaymentTypeId { get; set; }
        public int? NoOfDays { get; set; }
        public decimal? PriPaid { get; set; }
        public decimal? PriBalance { get; set; }
        public decimal? IntBalance { get; set; }
        public decimal? PenaltyBalance { get; set; }
        public decimal? TotalBalance { get; set; }
        public decimal? PriBalanceAtLastPaymenton { get; set; }
        public int? LmtPenaltyTypeId { get; set; }
        public string? CollectionCenterName { get; set; }
        public decimal? IssueAmounAtLastPaymenton { get; set; }
        public decimal? defaulterAmount { get; set; }
        public int? defaulterCount { get; set; }
    }

    public class LoanRegisterData
    {
        public List<LoanRegisterRowDto> Rows { get; set; } = [];
        public int TotalRecords { get; set; }
        public decimal TotalLoanIssueAmount { get; set; }
        public decimal TotalPriBalance { get; set; }
        public decimal TotalIntBalance { get; set; }
        public decimal TotalPenaltyBalance { get; set; }
        public decimal TotalBalance { get; set; }
        public decimal TotalPriPaid { get; set; }
        public string? TillDateBs { get; set; }
        public string? TillDateAd { get; set; }
        public string? BranchName { get; set; }
        public string? LoanTypeName { get; set; }
        public string? CollectorName { get; set; }
        public string? CollectionCenterName { get; set; }
        public string? SelectLoan { get; set; }
        public string? SelectLoanName { get; set; }
        public string? OrderBy { get; set; }
        public bool EnableCollectionCenter { get; set; }
        public bool NepaliReport { get; set; }
    }
}