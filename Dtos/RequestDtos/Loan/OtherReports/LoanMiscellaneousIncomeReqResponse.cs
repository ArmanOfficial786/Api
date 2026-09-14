// Dtos/RequestDtos/Loan/OtherReports/MiscellaneousIncomeRequestDto.cs
namespace NexgenCosysReport.Dtos.RequestDtos.Loan.OtherReports
{
    public class LoanMiscellaneousIncomeRequestDto
    {

        public string? MemberId { get; set; }
        public string? BranchIds { get; set; }
        public string? MemberGroupId { get; set; }
        public string OrderBy { get; set; } = "AccountNo";
        public bool VisualReport { get; set; } = false;
    }

    // Columns match sp_7_16_MiscellaneousIncomeReport's output SELECT list
    public class LoanMiscellaneousIncomeRowDto
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

    public class LoanMiscellaneousIncomeData
    {
        public List<LoanMiscellaneousIncomeRowDto> Rows { get; set; } = [];
        public int TotalRecords { get; set; }
        public decimal TotalAmount { get; set; }
        public string? BranchName { get; set; }
        public string? MemberName { get; set; }
        public string? MemberGroupName { get; set; }
        public string? OrderBy { get; set; }
    }
}