
namespace NexgenCosysReport.Dtos.RequestDtos.Loan.LoanAnalysisReport
{
    public class LoanProvisionRequestDto
    {

        public string TillDateBs { get; set; } = string.Empty;

        public string? BranchIds { get; set; }

        public string? MemberGroupId { get; set; }


        public string PenaltyType { get; set; } = "S";


        public bool VisualReport { get; set; } = false;
    }

    public class LoanProvisionRowDto
    {
        public int? LmtLoanProvisionMasterId { get; set; }
        public string? ProvisionName { get; set; }
        public decimal? ProvisionPercentage { get; set; }
        public int? NoofLoan { get; set; }
        public decimal? Amount { get; set; }
        public decimal? ProvisionAmount { get; set; }
    }

    public class LoanProvisionData
    {
        public List<LoanProvisionRowDto> Rows { get; set; } = [];
        public int TotalRecords { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal TotalProvisionAmount { get; set; }
        public int TotalLoans { get; set; }
        public string? TillDateBs { get; set; }
        public string? BranchName { get; set; }
        public string? MemberGroupName { get; set; }
        public string? PenaltyType { get; set; }
        public string? PenaltyTypeName { get; set; }
    }
}