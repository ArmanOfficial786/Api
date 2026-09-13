// Dtos/RequestDtos/Loan/OtherReports/LoanAppraisalRequestDto.cs
namespace NexgenCosysReport.Dtos.RequestDtos.Loan.OtherReports
{
    public class LoanAppraisalRequestDto
    {
        // Member ID (human-readable code, e.g. "M-001") - required
        public string MemberId { get; set; } = string.Empty;

        // Comma-separated office ids from the checkbox list ("-1" or empty = all offices)
        public string? BranchIds { get; set; }

        // Visual report flag
        public bool VisualReport { get; set; } = false;
    }

    // Columns match sp_7_16_LoanAppraisalReport's output SELECT list
    public class LoanAppraisalRowDto
    {
        public string? MemberId { get; set; }
        public string? FullName { get; set; }
        public string? LoanAccountNo { get; set; }
        public string? LoanTypeName { get; set; }
        public string? CollateralName { get; set; }
        public decimal? CollateralAmount { get; set; }
        public decimal? CollateralSanctionAmount { get; set; }
        public string? CollateralQuantity { get; set; }
        public string? CollateralCondition { get; set; }
        public string? CapacityToPay { get; set; }
        public string? LoanerCharacter { get; set; }
        public string? Capital { get; set; }
        public string? ManagerialDecision { get; set; }
        public bool? IsActive { get; set; }
    }

    public class LoanAppraisalData
    {
        public List<LoanAppraisalRowDto> Rows { get; set; } = [];
        public int TotalRecords { get; set; }
        public decimal TotalCollateralAmount { get; set; }
        public decimal TotalCollateralSanctionAmount { get; set; }
        public string? BranchName { get; set; }
        public string? MemberName { get; set; }
        public string? MemberId { get; set; }
    }
}