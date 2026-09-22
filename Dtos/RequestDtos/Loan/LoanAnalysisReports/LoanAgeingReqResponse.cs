// Dtos/RequestDtos/Loan/LoanAnalysisReport/LoanAgeingRequestDto.cs
namespace NexgenCosysReport.Dtos.RequestDtos.Loan.LoanAnalysisReport
{
    public class LoanAgeingRequestDto
    {
        public string TillDateBs { get; set; } = string.Empty;
        public string? BranchIds { get; set; }
        public string? MemberGroupId { get; set; } = "-1";
        public string? CollectorId { get; set; } = "-1";
        public string PenaltyType { get; set; } = "S";
        public string ShowLoanIssueDate { get; set; } = "ID";
        public string AgeingOn { get; set; } = "P";
        public string OrderBy { get; set; } = "-1";
        public bool IsNepaliReport { get; set; } = false;
        public bool VisualReport { get; set; } = false;
    }

    public class LoanAgeingRowDto
    {
        public string? MemberId { get; set; }
        public string? FullName { get; set; }
        public string? ContactAddress { get; set; }
        public string? ContactNo { get; set; }
        public string? LoanAccountNo { get; set; }
        public long? LmtLoanIssueId { get; set; }
        public decimal? LoanIssueAmount { get; set; }
        public string? LoanIssueOnBs { get; set; }
        public string? MaturityOnBs { get; set; }
        public DateTime? MaturityOn { get; set; }
        public string? LoanTypeName { get; set; }
        public decimal? PrinciplePaidAmt { get; set; }
        public decimal? TotalPrincipalAmount { get; set; }
        public decimal? RemainingPrinciplefromSche { get; set; }
        public int? InstallmentNo { get; set; }
        public int? DueDays { get; set; }
        public decimal? OverDue { get; set; }
        public DateTime? DefaultDateOn { get; set; }
        public string? DefaultDateBs { get; set; }
        public string? LoanODorNormal { get; set; }
        public string? PaymentMode { get; set; }
        public string? PrincapalPaymentMode { get; set; }
    }

    public class LoanAgeingSectionDto
    {
        public string? SectionName { get; set; }
        public List<LoanAgeingRowDto> Rows { get; set; } = new List<LoanAgeingRowDto>();
        public int TotalRecords { get; set; }
        public decimal TotalLoanIssueAmount { get; set; }
        public decimal TotalOverDue { get; set; }
        public decimal TotalPrincipalAmount { get; set; }
    }

    public class LoanAgeingData
    {
        public List<LoanAgeingSectionDto> Sections { get; set; } = new List<LoanAgeingSectionDto>();
        public int TotalRecords { get; set; }
        public decimal TotalLoanIssueAmount { get; set; }
        public decimal TotalOverDue { get; set; }
        public decimal TotalPrincipalAmount { get; set; }
        public string? TillDateBs { get; set; }
        public string? BranchName { get; set; }
        public string? MemberGroupName { get; set; }
        public string? CollectorName { get; set; }
        public string? PenaltyType { get; set; }
        public string? PenaltyTypeName { get; set; }
        public string? AgeingOn { get; set; }
        public string? AgeingOnName { get; set; }
        public string? ShowLoanIssueDate { get; set; }
        public string? ShowLoanIssueDateName { get; set; }
        public string? OrderBy { get; set; }
        public bool IsNepaliReport { get; set; }
    }
}