
namespace NexgenCosysReport.Dtos.RequestDtos.Loan.LoanAccountWiseReport
{
    public class LoanDeletedRequestDto
    {

        public string ReportType { get; set; } = "All";

        public string? FromDateBs { get; set; }
        public string? ToDateBs { get; set; }

        public string? BranchIds { get; set; }

        public bool VisualReport { get; set; } = false;
        public string? MemberGroupId { get; set; }

        public string? OrderBy { get; set; }
    }


    public class LoanDeletedRowDto
    {
        public string? MemberId { get; set; }
        public string? FullName { get; set; }
        public string? LoanAccountNo { get; set; }
        public string? LoanTypeName { get; set; }
        public decimal? LoanIssueAmount { get; set; }
        public string? LoanIssueDate { get; set; }
        public string? Period { get; set; }
        public string? InterestRate { get; set; }
        public string? ModifyDate { get; set; }
        public DateTime? DeletedDateOn { get; set; }
    }

    public class LoanDeletedData
    {
        public List<LoanDeletedRowDto> Rows { get; set; } = [];
        public int TotalRecords { get; set; }
        public string ReportType { get; set; } = "All";
        public string? FromDateBs { get; set; }
        public string? ToDateBs { get; set; }
        public string? BranchName { get; set; }
        public string? MemberGroupName { get; set; }
        public string? OrderBy { get; set; }
    }
}