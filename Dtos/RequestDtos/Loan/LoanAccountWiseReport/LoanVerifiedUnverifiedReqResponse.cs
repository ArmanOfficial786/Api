
namespace NexgenCosysReport.Dtos.RequestDtos.Loan.LoanAccountWiseReport
{
    public class LoanVerifiedUnverifiedRequestDto
    {

        public string? MemberRegistrationId { get; set; }

        public string ReportType { get; set; } = "V";

        public string? FromDateBs { get; set; }
        public string? ToDateBs { get; set; }


        public string? BranchIds { get; set; }


        public string OrderBy { get; set; } = "-1";


        public bool VisualReport { get; set; } = false;
    }


    public class LoanVerifiedUnverifiedRowDto
    {
        public string? MemberId { get; set; }
        public string? MemberIdFirst { get; set; }
        public decimal? MemberIdLast { get; set; }
        public string? MemberName { get; set; }
        public string? LoanTypeName { get; set; }
        public string? LoanAccountNo { get; set; }
        public decimal? LoanIssueAmount { get; set; }
        public string? InterestRate { get; set; }
        public string? LoanIssueOn { get; set; }
        public string? MaturityOn { get; set; }
        public string? Period { get; set; }
        public string? VerifiedOnBS { get; set; }
        public string? VerifiedBy { get; set; }
    }

    public class LoanVerifiedUnverifiedData
    {
        public List<LoanVerifiedUnverifiedRowDto> Rows { get; set; } = [];
        public int TotalRecords { get; set; }
        public decimal TotalLoanIssueAmount { get; set; }
        public string? FromDateBs { get; set; }
        public string? ToDateBs { get; set; }
        public string? BranchName { get; set; }
        public string? ReportType { get; set; }
        public string? ReportTypeName { get; set; }
        public string? OrderBy { get; set; }
    }
}