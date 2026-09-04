namespace NexgenCosysReport.Dtos.RequestDtos.MemberAccount.InterestPayableReport
{
    public class FixedDepositCertificateScheduleRequestDto
    {
        public long AccountId { get; set; } = -1;
        public string AccountNo { get; set; } = string.Empty;
        public string MemberId { get; set; } = string.Empty;
        public string MemberName { get; set; } = string.Empty;
        public bool ShowHeader { get; set; } = true;
        public string ReportType { get; set; } = "Certificate"; // Certificate or Schedule
    }

    public class FixedDepositCertificateDetailDto
    {
        public string? MemberId { get; set; }
        public string? MemberName { get; set; }
        public string? InterestRate { get; set; }        // string, e.g. "10.5 %"
        public string? AccountNo { get; set; }
        public string? AccountOpenOnBS { get; set; }      // Bs date, only version SP returns
        public string? MaturityOnBs { get; set; }         // Bs date, only version SP returns
        public string? AccountType { get; set; }          // aliased DepositTypeName in SP
        public string? Address { get; set; }
        public string? PhoneNo { get; set; }
        public string? InterestTransfer { get; set; }
        public string? InterestCalculation { get; set; }
        public decimal? DealAmount { get; set; }          // net balance: deposits - withdrawals
        public long? SycDepositCategoryId { get; set; }
    }

    // Matches #FixedDepositSchedule columns exactly: SN, Interest, Tax, NetAmount,
    // InterestDateOnBs, InterestDateOn, DaysNo, IsGenerated
    public class FixedDepositScheduleRowDto
    {
        public int? SN { get; set; }
        public decimal? Interest { get; set; }
        public decimal? Tax { get; set; }
        public decimal? NetAmount { get; set; }
        public string? InterestDateOnBs { get; set; }
        public DateTime? InterestDateOn { get; set; }
        public int? DaysNo { get; set; }
        public string? IsGenerated { get; set; }
    }

    public class FixedDepositCertificateScheduleData
    {
        public FixedDepositCertificateDetailDto? CertificateDetail { get; set; }
        public List<FixedDepositScheduleRowDto> ScheduleRows { get; set; } = new();
        public int TotalRecords { get; set; }
        public decimal TotalInterest { get; set; }
        public decimal TotalTax { get; set; }
        public decimal TotalNetAmount { get; set; }
        public string? AccountNo { get; set; }
        public string? MemberId { get; set; }
        public string? MemberName { get; set; }
        public bool ShowHeader { get; set; }
        public string? ReportType { get; set; }
    }

    public class FixedDepositCertificateScheduleReqResponse
    {
    }
}