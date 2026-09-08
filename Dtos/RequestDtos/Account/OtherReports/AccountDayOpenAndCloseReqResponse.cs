namespace NexgenCosysReport.Dtos.RequestDtos.Account.OtherReports
{
    public class AccountDayOpenAndCloseRequestDto
    {
        public string FromDateBs { get; set; } = string.Empty;
        public string ToDateBs { get; set; } = string.Empty;
        public string? branchId { get; set; }
        public string? UserId { get; set; }
        public string OrderBy { get; set; } = "Opened Date";
        public bool VisualReport { get; set; } = false;
    }

    // Column names match the SP's SELECT aliases exactly for Dapper auto-mapping
    public class AccountDayOpenAndCloseRowDto
    {
        public string? OfficeName { get; set; }
        public string? OpenedDateOnBs { get; set; }
        public string? Status { get; set; }
        public string? EveningCounter { get; set; }
        public DateTime? OpenedOn { get; set; }
        public DateTime? ClosedOn { get; set; }
        public string? OpenedBy { get; set; }
        public string? ClosedBy { get; set; }
    }

    public class AccountDayOpenAndCloseData
    {
        public List<AccountDayOpenAndCloseRowDto> Rows { get; set; } = [];
        public int TotalRecords { get; set; }
        public string? FromDateBs { get; set; }
        public string? ToDateBs { get; set; }
        public string? BranchNames { get; set; }
        public string? OrderBy { get; set; }
    }
    public class AccountDayOpenAndCloseReqResponse
    {
    }
}
