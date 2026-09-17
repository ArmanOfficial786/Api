
namespace NexgenCosysReport.Dtos.RequestDtos.Loan.OtherReports
{
    public class LoanGuaranteerRequestDto
    {
        public string MemberId { get; set; } = string.Empty;
        public string? BranchIds { get; set; }
        public string OrderBy { get; set; } = "-1";
        public bool VisualReport { get; set; } = false;
    }


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