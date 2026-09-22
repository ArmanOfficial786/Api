
namespace NexgenCosysReport.Dtos.RequestDtos.Loan.LoanAccountWiseReport
{
    public class LoanIssueDetailsRequestDto
    {

        public string? MemberId { get; set; }


        public string? FromDateBs { get; set; }
        public string? ToDateBs { get; set; }

        public string ReportType { get; set; } = "LIR";

        public string? BranchIds { get; set; }

        public bool VisualReport { get; set; } = false;

        public string? OrderBy { get; set; }

        public bool EnableCollectionCenter { get; set; } = false;


        public string? CollectionCenterIds { get; set; }
    }


    public class LoanIssueDetailsRowDto
    {
        public string? MemberId { get; set; }
        public string? MemberIdFirst { get; set; }
        public decimal? MemberIdLast { get; set; }
        public string? FullName { get; set; }
        public string? LoanAccountNo { get; set; }
        public string? LoanTypeName { get; set; }
        public decimal? LoanIssueAmount { get; set; }
        public string? LoanIssueDate { get; set; }
        public string? Period { get; set; }
        public string? InterestRate { get; set; }
        public string? CollectionCenterName { get; set; }
        public string? TransStatus { get; set; }
    }

    public class LoanIssueDetailsData
    {
        public List<LoanIssueDetailsRowDto> Rows { get; set; } = [];
        public int TotalRecords { get; set; }
        public string? MemberId { get; set; }
        public string? MemberName { get; set; }
        public string? FromDateBs { get; set; }
        public string? ToDateBs { get; set; }
        public string? BranchName { get; set; }
        public string ReportType { get; set; } = "LIR";
        public string? OrderBy { get; set; }
        public bool EnableCollectionCenter { get; set; }
    }
}