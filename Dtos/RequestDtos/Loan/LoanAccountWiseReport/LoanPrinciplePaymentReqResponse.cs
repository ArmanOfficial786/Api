
namespace NexgenCosysReport.Dtos.RequestDtos.Loan.OtherReports
{
    public class LoanPrinciplePaymentRequestDto
    {

        public string PaymentType { get; set; } = "PP";


        public long MemberRegistrationId { get; set; } = 0;

        public string? FromDateBs { get; set; }
        public string? ToDateBs { get; set; }


        public string? BranchIds { get; set; }


        public string? MemberGroupId { get; set; }


        public string OrderBy { get; set; } = "-1";


        public bool VisualReport { get; set; } = false;
    }


    public class LoanPrinciplePaymentRowDto
    {
        public string? MemberId { get; set; }
        public string? MemberIdFirst { get; set; }
        public decimal? MemberIdLast { get; set; }
        public string? FullName { get; set; }
        public string? LoanAccountNo { get; set; }
        public string? LoanTypeName { get; set; }
        public string? Date { get; set; }
        public decimal? PaidAmount { get; set; }
        public string? UserName { get; set; }
    }

    public class LoanPrinciplePaymentData
    {
        public List<LoanPrinciplePaymentRowDto> Rows { get; set; } = [];
        public int TotalRecords { get; set; }
        public decimal TotalPaidAmount { get; set; }
        public string? FromDateBs { get; set; }
        public string? ToDateBs { get; set; }
        public string? BranchName { get; set; }
        public string? PaymentType { get; set; }
        public string? PaymentTypeName { get; set; }
        public string? MemberGroupName { get; set; }
        public string? OrderBy { get; set; }
    }
}