namespace NexgenCosysReport.Dtos.RequestDtos.MemberAccount.OthersReport
{

    public class SavingInterestChangeLogRequestDto
    {
        public string FromDateBs { get; set; } = string.Empty;
        public string ToDateBs { get; set; } = string.Empty;
        public string ReportType { get; set; } = "1"; // 1=Account No, 2=Deposit Type
        public string? AccountNo { get; set; }
        public long? AccountOpeningId { get; set; } = -1;
        public long? DepositTypeId { get; set; } = -1;
        public long? OfficeId { get; set; } = -1;
        public string OfficeName { get; set; } = "All";
        public bool VisualReport { get; set; } = false;
    }

    public class SavingInterestChangeLogRowDto
    {
        public string? AccountNo { get; set; }
        public string? MemberId { get; set; }
        public string? MemberName { get; set; }
        public string? DepositTypeName { get; set; }
        public decimal? OldInterestRate { get; set; }
        public decimal? NewInterestRate { get; set; }
        public string? ChangedDate { get; set; }
        public string? ChangedDateBs { get; set; }
        public string? ChangedBy { get; set; }
        public string? Reason { get; set; }
        public string? OfficeName { get; set; }
    }

    public class SavingInterestChangeLogData
    {
        public List<SavingInterestChangeLogRowDto> Rows { get; set; } = new();
        public int TotalRecords { get; set; }
        public string? FromDateBs { get; set; }
        public string? ToDateBs { get; set; }
        public string? ReportType { get; set; }
        public string? AccountNo { get; set; }
        public string? DepositTypeName { get; set; }
        public string? OfficeName { get; set; }
        public int TotalChanges { get; set; }
    }

    public class SavingInterestChangeLogReqResponse
    {
    }
}
