namespace NexgenCosysReport.Dtos.RequestDtos.MemberAccount.OthersReport
{
    public class DataEditedReportRequestDto
    {
        public string FromDateBs { get; set; } = string.Empty;
        public string ToDateBs { get; set; } = string.Empty;
        public string? BranchIds { get; set; }
        public long? EntryBy { get; set; }
        public long? EditedBy { get; set; }
        public long? MemberRegistrationId { get; set; }
        public string OrderBy { get; set; } = string.Empty;
        public bool SameCompanyName { get; set; }
        public bool VisualReport { get; set; }
    }

    public class DataEditedRowDto
    {
        public string? Date { get; set; }
        public string? MemberId { get; set; }
        public string? MemberName { get; set; }
        public string? AccountNo { get; set; }
        public string? Description { get; set; }
        public decimal? ActualAmount { get; set; }
        public string? EditedDate { get; set; }
        public string? EditedBy { get; set; }
        public string? EntryBy { get; set; }
    }

    public class DataEditedReportData
    {
        public List<DataEditedRowDto> Rows { get; set; } = new();
        public int TotalRecords { get; set; }
        public decimal TotalActualAmount { get; set; }
        public string FromDateBs { get; set; } = string.Empty;
        public string ToDateBs { get; set; } = string.Empty;
        public string? BranchName { get; set; }
        public string? EntryByName { get; set; }
        public string? EditedByName { get; set; }
        public string OrderBy { get; set; } = string.Empty;
    }

    public class DataEditedReqResponse
    {
    }
}
