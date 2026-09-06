// Dtos/RequestDtos/Account/OtherReports/AccountYearClosingRequestDto.cs
namespace NexgenCosysReport.Dtos.RequestDtos.Account.OtherReports
{
    public class AccountYearClosingRequestDto
    {
        public string? BranchIds { get; set; }
        public string OrderBy { get; set; } = "Branch Name";
        public bool VisualReport { get; set; } = false;
    }

    public class AccountYearClosingRowDto
    {
        public string? AccountYear { get; set; }
        public string? ClosedOnBs { get; set; }
        public string? VoucherNo { get; set; }
        public string? BranchName { get; set; }
        public string? ClosedBy { get; set; }
        public string? Status { get; set; }
    }

    public class AccountYearClosingData
    {
        public List<AccountYearClosingRowDto> Rows { get; set; } = new List<AccountYearClosingRowDto>();
        public int TotalRecords { get; set; }
        public string? BranchNames { get; set; }
        public string? OrderBy { get; set; }
    }
}