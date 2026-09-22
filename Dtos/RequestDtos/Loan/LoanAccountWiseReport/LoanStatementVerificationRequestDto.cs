
namespace NexgenCosysReport.Dtos.RequestDtos.Loan.OtherReports
{
    public class LoanStatementVerificationRequestDto
    {

        public long MemberRegistrationId { get; set; } = -1;


        public string? FromDateBs { get; set; }
        public string? ToDateBs { get; set; }

        public string? BranchIds { get; set; }


        public long CollectorId { get; set; } = -1;

        public string OrderBy { get; set; } = "-1";


        public bool VisualReport { get; set; } = false;
    }

    public class LoanStatementVerificationRowDto
    {
        public string? MemberId { get; set; }
        public string? MemberName { get; set; }
        public string? LoanAccountNo { get; set; }
        public string? PassBookVerifiedTill { get; set; }
        public string? VerifiedDate { get; set; }
        public DateTime? VerifiedTime { get; set; }
        public string? VerifiedBy { get; set; }
    }

    public class LoanStatementVerificationData
    {
        public List<LoanStatementVerificationRowDto> Rows { get; set; } = [];
        public int TotalRecords { get; set; }
        public string? FromDateBs { get; set; }
        public string? ToDateBs { get; set; }
        public string? BranchName { get; set; }
        public string? MemberName { get; set; }
        public string? CollectorName { get; set; }
        public string? OrderBy { get; set; }
    }
}