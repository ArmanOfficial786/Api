using System.ComponentModel.DataAnnotations;

namespace NexgenCosysReport.Dtos.RequestDtos.Common
{
    public class LoanAccountLookUpRequest
    {
        [Range(1, int.MaxValue)]
        public int Page { get; set; } = 1;

        public long? LoanIssueId { get; set; }
        public string? AccountNo { get; set; }
        public string? MemberId { get; set; }
        public string? MemberName { get; set; }
        public string? LoanStatus { get; set; }

        // Sorting (AccountNo | MemberId | MemberName | LoanStatus | LoanIssueId)
        public string SortColumn { get; set; } = "AccountNo";
        public string SortDirection { get; set; } = "DESC";
    }

    public class LoanAccountLookUpDtos
    {
        public long LoanIssueId { get; set; }
        public string? AccountNo { get; set; }
        public string? MemberId { get; set; }
        public string? MemberName { get; set; }
        public string? LoanStatus { get; set; }
        public int TotalCount { get; set; }
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
    }

    public class LoanAccountSelectedDto
    {
        public long LoanIssueId { get; set; }
        public string? AccountNo { get; set; }
        public string? MemberId { get; set; }
        public string? MemberName { get; set; }
        public string? LoanStatus { get; set; }
    }
}
