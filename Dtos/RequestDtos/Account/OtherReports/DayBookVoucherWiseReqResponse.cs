namespace NexgenCosysReport.Dtos.RequestDtos.Account.OtherReports
{


    public class DayBookVoucherWiseRequestDto
    {
        public string FromDateBs { get; set; } = string.Empty;
        public string ToDateBs { get; set; } = string.Empty;
        public string? BranchId { get; set; }
        public string OrderBy { get; set; } = "-1";
        public bool VisualReport { get; set; }

    }

    public class DayBookVoucherWiseRowDto
    {
        public string? VoucherNo { get; set; }
        public string? VoucherDate { get; set; }
        public string? Narration { get; set; }
        public decimal? Amount { get; set; }
        public string? Type { get; set; }
    }

    public class DayBookVoucherWiseData
    {
        public List<DayBookVoucherWiseRowDto> Rows { get; set; } = [];
        public int TotalRecords { get; set; }
        public decimal TotalAmount { get; set; }
        public string? FromDateBs { get; set; }
        public string? ToDateBs { get; set; }
        public string? BranchName { get; set; }
        public string? OrderBy { get; set; }
    }
    public class DayBookVoucherWiseReqResponse
    {
    }
}
