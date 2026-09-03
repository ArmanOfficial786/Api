namespace NexgenCosysReport.Dtos.RequestDtos.MemberAccount.OthersReport
{
    public class SavingInterestChangeLogRequestDto
    {
        public string FromDateBs { get; set; } = string.Empty;
        public string ToDateBs { get; set; } = string.Empty;
        public string ReportType { get; set; } = "1"; // "1" = Account No, "2" = Deposit Type
        public string? AccountNo { get; set; }
        public long? DepositTypeId { get; set; }
        public long? OfficeId { get; set; }
        public string? OfficeName { get; set; }
        public bool VisualReport { get; set; } = false;
    }

    public class SavingInterestChangeLogRowDto
    {
        public string? MemberId { get; set; }
        public string? MemberName { get; set; }
        public string? SavingsType { get; set; } // DepositTypeName from SP
        public string? AccountNo { get; set; }
        public decimal? PreviousInterestRate { get; set; }
        public decimal? CurrentInterestRate { get; set; }
        public DateTime? InterestActivationOn { get; set; }
        public string? InterestActivationOnbs { get; set; } // Nepali date
        public string? CreatedDateBs { get; set; } // For display
        public string? CreatedBy { get; set; } // For display
        public string? Remarks { get; set; }
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
