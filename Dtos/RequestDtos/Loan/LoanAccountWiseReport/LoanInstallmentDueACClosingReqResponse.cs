
namespace NexgenCosysReport.Dtos.RequestDtos.Loan.OtherReports
{
    public class LoanInstallmentDueACClosingRequestDto
    {

        public string AccountNo { get; set; } = string.Empty;

        public string CalcBySwOrAsOnDate { get; set; } = "AOD";


        public string CalcByPenaltyType { get; set; } = "ACP";


        public string CalcByPrincipleInterest { get; set; } = "PI";


        public string PenaltyAmount { get; set; } = "0.00";

        public string TillDateBs { get; set; } = string.Empty;


        public bool VisualReport { get; set; } = false;
    }

    public class LoanInstallmentDueACClosingMemberInfoDto
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
    }

    public class LoanInstallmentDueACClosingPaymentDto
    {
        public string? PaymentDate { get; set; }
        public string? PaymentMode { get; set; }
        public decimal? PaymentAmount { get; set; }
        public string? ChequeNo { get; set; }
        public string? Narration { get; set; }
    }

    public class LoanInstallmentDueACClosingInstallmentDto
    {
        public int? Sno { get; set; }
        public string? DueDate { get; set; }
        public int? NoOfDays { get; set; }
        public string? PenaltyPercent { get; set; }
        public decimal? Principle { get; set; }
        public decimal? Interest { get; set; }
        public decimal? DueAmount { get; set; }
        public decimal? NetPenalty { get; set; }
        public string? DefaultFrom { get; set; }
        public int? DefaultDays { get; set; }
    }


    public class LoanInstallmentDueACClosingDueDto
    {
        public string? Title { get; set; }
        public decimal? Amount { get; set; }
        public string? Description { get; set; }
    }

    public class LoanInstallmentDueACClosingData
    {
        public LoanInstallmentDueACClosingMemberInfoDto? MemberInfo { get; set; }
        public List<LoanInstallmentDueACClosingPaymentDto> Payments { get; set; } = [];
        public List<LoanInstallmentDueACClosingInstallmentDto> Installments { get; set; } = [];
        public List<LoanInstallmentDueACClosingDueDto> ClosingDues { get; set; } = [];
        public int TotalRecords { get; set; }
        public decimal TotalPrinciple { get; set; }
        public decimal TotalInterest { get; set; }
        public decimal TotalPenalty { get; set; }
        public decimal TotalDueAmount { get; set; }
        public decimal TotalNetPenalty { get; set; }
        public string? TillDateBs { get; set; }
        public string? CalcBySwOrAsOnDate { get; set; }
        public string? CalcByPenaltyType { get; set; }
        public string? CalcByPrincipleInterest { get; set; }
        public string? PenaltyAmount { get; set; }
        public string? LastDueDate { get; set; }
        public string? NextDueDate { get; set; }
    }
}