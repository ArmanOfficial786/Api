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
        public string? AccountNo { get; set; }
        public string? MemberId { get; set; }
        public string? MemberName { get; set; }
        public string? FatherName { get; set; }
        public string? GrandFatherName { get; set; }
        public string? SpouseName { get; set; }
        public string? Address { get; set; }
        public string? PhoneNo { get; set; }
        public string? DepositTypeName { get; set; }
        public string? AccountOpenDate { get; set; }
        public string? AccountOpenDateBs { get; set; }
        public string? MaturityDate { get; set; }
        public string? MaturityDateBs { get; set; }
        public decimal? DepositAmount { get; set; }
        public decimal? InterestRate { get; set; }
        public decimal? MaturityAmount { get; set; }
        public string? Remarks { get; set; }
    }

    public class FixedDepositScheduleRowDto
    {
        public int? InstallmentNo { get; set; }
        public string? PaymentDate { get; set; }
        public string? PaymentDateBs { get; set; }
        public decimal? PrincipalAmount { get; set; }
        public decimal? InterestAmount { get; set; }
        public decimal? TotalAmount { get; set; }
        public string? Remarks { get; set; }
    }

    public class FixedDepositCertificateScheduleData
    {
        public FixedDepositCertificateDetailDto? CertificateDetail { get; set; }
        public List<FixedDepositScheduleRowDto> ScheduleRows { get; set; } = new();
        public int TotalRecords { get; set; }
        public decimal TotalPrincipal { get; set; }
        public decimal TotalInterest { get; set; }
        public decimal TotalAmount { get; set; }
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
