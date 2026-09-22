
namespace NexgenCosysReport.Dtos.RequestDtos.Loan.LoanAnalysisReport
{
    public class LoanAllDetailsRequestDto
    {
        public long MemberRegistrationId { get; set; } = -1;
        public string? LoanTypeId { get; set; }
        public string? Status { get; set; }
        public string TillDateBs { get; set; } = string.Empty;
        public string? BranchIds { get; set; }
        public string? CollectionCenterId { get; set; } = "-1";
        public bool EnableCollectionCenter { get; set; } = false;
        public string? CollectorId { get; set; } = "-1";
        public string? MemberGroupId { get; set; } = "-1";
        public string OrderBy { get; set; } = "-1";
        public bool VisualReport { get; set; } = false;
    }

    public class LoanAllDetailsRowDto
    {
        public string? MemberId { get; set; }
        public string? MemberName { get; set; }
        public string? TemAddress { get; set; }
        public string? MobileNo { get; set; }
        public string? ContactNo { get; set; }
        public string? CitizenshipNo { get; set; }
        public string? CitizenshipIssuePlace { get; set; }
        public string? LoanAccountNo { get; set; }
        public string? LoanPaymentTypeCode { get; set; }
        public string? IssuePeriod { get; set; }
        public string? InstallmentType { get; set; }
        public string? LoanCategory { get; set; }
        public string? LoanTypeName { get; set; }
        public string? InterestRate { get; set; }
        public string? IssueDate { get; set; }
        public string? MaturityDate { get; set; }
        public string? NormalOD { get; set; }
        public decimal? ODSansationAmount { get; set; }
        public decimal? RevolvingODBalance { get; set; }
        public string? LoanIssueType { get; set; }
        public decimal? SansationAmount { get; set; }
        public decimal? LoanIssueAmount { get; set; }
        public decimal? TotalInterestPaid { get; set; }
        public decimal? TotalPenaltyPaid { get; set; }
        public decimal? TotalPrincipalpaid { get; set; }
        public string? LastInterestPaymentDate { get; set; }
        public string? LastPenaltyPaymentDate { get; set; }
        public decimal? RemainingPrinciple { get; set; }
        public decimal? DefaultPrinciple { get; set; }
        public decimal? PreviousInterest { get; set; }
        public decimal? PreviousPenalty { get; set; }
        public decimal? CurrInterest { get; set; }
        public decimal? CurrPenalty { get; set; }
        public decimal? TotalDue { get; set; }
        public decimal? TotalRemaining { get; set; }
        public decimal? InstallmentAmount { get; set; }
        public decimal? PrincipleDefaulter { get; set; }
        public decimal? InterestDefaulter { get; set; }
        public decimal? TotalDefaulter { get; set; }
        public int? InstallmentCount { get; set; }
        public decimal? ProvisionalAmount { get; set; }
        public string? LoanStatus { get; set; }
        public string? LoanAnalysisStatus { get; set; }
        public string? CollectionCenterName { get; set; }
        public decimal? TotalInterest { get; set; }
        public decimal? TotalPenalty { get; set; }
        public string? MemberGroup { get; set; }
    }

    public class LoanAllDetailsData
    {
        public List<LoanAllDetailsRowDto> Rows { get; set; } = [];
        public int TotalRecords { get; set; }
        public decimal TotalLoanIssueAmount { get; set; }
        public decimal TotalRemainingPrinciple { get; set; }
        public decimal TotalDefaultPrinciple { get; set; }
        public decimal TotalCurrInterest { get; set; }
        public decimal TotalCurrPenalty { get; set; }
        public decimal TotalDue { get; set; }
        public decimal TotalRemaining { get; set; }
        public decimal TotalProvisionAmount { get; set; }
        public string? TillDateBs { get; set; }
        public string? BranchName { get; set; }
        public string? MemberGroupName { get; set; }
        public string? CollectionCenterName { get; set; }
        public string? CollectorName { get; set; }
        public string? LoanTypeName { get; set; }
        public string? StatusName { get; set; }
        public string? OrderBy { get; set; }
    }
}