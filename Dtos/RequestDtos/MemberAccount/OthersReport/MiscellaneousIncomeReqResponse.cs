namespace NexgenCosysReport.Dtos.RequestDtos.MemberAccount.OthersReport
{

    public class MiscellaneousIncomeRequestDto
    {
        public string FromDateBs { get; set; } = string.Empty;
        public string ToDateBs { get; set; } = string.Empty;
        public string BranchIds { get; set; } = "-1";
        public string OrderBy { get; set; } = "Member Id";
        public string ReportType { get; set; } = "Miscellaneous"; // Miscellaneous, Fund
        public string BranchName { get; set; } = "All Branches";
        public long? MemberId { get; set; } = -1;
        public bool VisualReport { get; set; } = false;
    }

    public class MiscellaneousIncomeRowDto
    {
        public string? Date { get; set; }
        public string? MemberId { get; set; }
        public string? MemberName { get; set; }
        public string? AccountNo { get; set; }
        public string? Particulars { get; set; }
        public decimal? Amount { get; set; }
        public string? Operator { get; set; }
        public string? Description { get; set; }
    }

    public class MiscellaneousIncomeData
    {
        public List<MiscellaneousIncomeRowDto> Rows { get; set; } = new();
        public int TotalRecords { get; set; }
        public decimal TotalAmount { get; set; }
        public string? FromDateBs { get; set; }
        public string? ToDateBs { get; set; }
        public string? BranchNames { get; set; }
        public string? OrderBy { get; set; }
        public string? ReportType { get; set; }
        public string? SelectedMemberId { get; set; }
        public string? SelectedMemberName { get; set; }
    }

    public class MiscellaneousIncomeReqResponse
    {
    }
}
