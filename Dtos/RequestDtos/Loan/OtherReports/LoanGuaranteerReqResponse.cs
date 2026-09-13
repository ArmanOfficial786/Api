// Dtos/RequestDtos/Loan/OtherReports/LoanGuaranteerRequestDto.cs
namespace NexgenCosysReport.Dtos.RequestDtos.Loan.OtherReports
{
    public class LoanGuaranteerRequestDto
    {
        // Member ID (human-readable code, e.g. "MR-01-1") - required
        public string MemberId { get; set; } = string.Empty;

        // Comma-separated office ids from the checkbox list ("-1" or empty = all offices)
        public string? BranchIds { get; set; }

        // "MrL.MemberId" | "LoneeFullName" | "LoanAccountNo" | "AccountNo" 
        // | "GuaranteeAmount" | "GuaranteeShareAmount" | "GuaranteeDateOnBs"
        public string OrderBy { get; set; } = "-1";

        // Visual report flag
        public bool VisualReport { get; set; } = false;
    }

    // Columns match sp_7_16_LoanGuaranteerReport's output SELECT list
    public class LoanGuaranteerRowDto
    {
        public string? MemberId { get; set; }
        public string? FullName { get; set; }
        public string? LoanAccountNo { get; set; }
        public string? LoanTypeName { get; set; }
        public string? AccountNo { get; set; }
        public string? ShareTypeName { get; set; }
        public decimal? GuaranteeAmount { get; set; }
        public decimal? GuaranteeShareAmount { get; set; }
        public string? GuaranteeDateOnBs { get; set; }
        public string? LoneeId { get; set; }
        public string? LoneeFullName { get; set; }
    }

    public class LoanGuaranteerData
    {
        public List<LoanGuaranteerRowDto> Rows { get; set; } = [];
        public int TotalRecords { get; set; }
        public decimal TotalGuaranteeAmount { get; set; }
        public decimal TotalGuaranteeShareAmount { get; set; }
        public string? BranchName { get; set; }
        public string? MemberName { get; set; }
        public string? MemberId { get; set; }
        public string? OrderBy { get; set; }
    }
}