// Dtos/RequestDtos/Loan/OtherReports/MiscellaneousIncomeRequestDto.cs
namespace NexgenCosysReport.Dtos.RequestDtos.Loan.OtherReports
{
    public class MiscellaneousIncomeRequestDto
    {
        // Member ID (human-readable code, e.g. "M-001") - optional
        public string? MemberId { get; set; }

        // Comma-separated office ids from the checkbox list ("-1" or empty = all offices)
        public string? BranchIds { get; set; }

        // Member Group filter (empty/-1 = all groups)
        public string? MemberGroupId { get; set; }

        // Order By: "AccountNo" | "MemberId" | "FullName" | "Amount"
        public string OrderBy { get; set; } = "AccountNo";

        // Visual report flag
        public bool VisualReport { get; set; } = false;
    }

    // Columns match sp_7_16_MiscellaneousIncomeReport's output SELECT list
    public class MiscellaneousIncomeRowDto
    {
        public string? MemberId { get; set; }
        public string? MemberIdFirst { get; set; }
        public decimal? MemberIdLast { get; set; }
        public string? LoanAccountNoFirst { get; set; }
        public decimal? LoanAccountNoLast { get; set; }
        public string? MemberName { get; set; }
        public string? LoanAccountNo { get; set; }
        public string? TypeName { get; set; }
        public string? Type { get; set; }
        public decimal? Amount { get; set; }
        public string? Date { get; set; }
    }

    public class MiscellaneousIncomeData
    {
        public List<MiscellaneousIncomeRowDto> Rows { get; set; } = [];
        public int TotalRecords { get; set; }
        public decimal TotalAmount { get; set; }
        public string? BranchName { get; set; }
        public string? MemberName { get; set; }
        public string? MemberGroupName { get; set; }
        public string? OrderBy { get; set; }
    }
}