
namespace NexgenCosysReport.Dtos.RequestDtos.Loan.OtherReports
{
    public class LoanScheduleRequestDto
    {

        public string AccountNo { get; set; } = string.Empty;


        public long LoanIssueId { get; set; } = -1;


        public bool SameCompanyName { get; set; } = true;

        public bool VisualReport { get; set; } = false;
    }

    public class LoanScheduleMemberInfoDto
    {
        public string? MemberId { get; set; }
        public string? MemberName { get; set; }
        public string? LoanAccountNo { get; set; }
        public string? LoanTypeName { get; set; }
        public decimal? LoanIssueAmount { get; set; }
        public decimal? InterestRate { get; set; }
        public string? LoanIssueOnBs { get; set; }
        public string? MaturityOnBs { get; set; }
        public string? ContactAddress { get; set; }
        public string? ContactNo { get; set; }
        public string? LoanType { get; set; }
        public string? Period { get; set; }
        public string? AccountNo { get; set; }
        public string? AccountStatus { get; set; }
        public int? PaymentDurationTypeId { get; set; }
        public string? PaymentDurationType { get; set; }
    }


    public class LoanScheduleRowDto
    {
        public long? LmtLoanScheduleId { get; set; }
        public long? LmtLoanIssueId { get; set; }
        public int? LoanFrequency { get; set; }
        public string? ScheduleDateOnBs { get; set; }
        public DateTime? ScheduleDateOn { get; set; }
        public decimal? InstallmentAmount { get; set; }
        public decimal? PrincipleAmount { get; set; }
        public decimal? InterestAmount { get; set; }
        public decimal? PrincipleBalanceAmount { get; set; }
        public bool? IsPaid { get; set; }
        public bool? IsActive { get; set; }
        public string? Remarks { get; set; }
    }

    public class LoanScheduleData
    {
        public LoanScheduleMemberInfoDto? MemberInfo { get; set; }
        public List<LoanScheduleRowDto> Rows { get; set; } = [];
        public int TotalRecords { get; set; }
        public decimal TotalInstallmentAmount { get; set; }
        public decimal TotalPrincipleAmount { get; set; }
        public decimal TotalInterestAmount { get; set; }
        public decimal TotalBalanceAmount { get; set; }
    }
}