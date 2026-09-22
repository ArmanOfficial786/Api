
namespace NexgenCosysReport.Dtos.RequestDtos.Loan.OtherReports
{
    public class LoanFineInterestPrinciplePaymentSummaryRequestDto
    {

        public long MemberRegistrationId { get; set; } = 0;


        public string? FromDateBs { get; set; }
        public string? ToDateBs { get; set; }

        public string? BranchIds { get; set; }


        public string? CollectionCenterId { get; set; } = "-1";


        public bool EnableCollectionCenter { get; set; } = false;


        public string? CollectorId { get; set; } = "-1";


        public string SelectAllOrOnlyCash { get; set; } = "-1";


        public string OrderBy { get; set; } = "-1";

        public bool VisualReport { get; set; } = false;
    }

    public class LoanFineInterestPrinciplePaymentSummaryRowDto
    {
        public string? MemberId { get; set; }
        public string? MemberIdFirst { get; set; }
        public decimal? MemberIdLast { get; set; }
        public string? FullName { get; set; }
        public string? LoanAccountNo { get; set; }
        public string? LoanTypeName { get; set; }
        public decimal? Fine { get; set; }
        public decimal? Interest { get; set; }
        public decimal? Principal { get; set; }
        public decimal? TotalAmount { get; set; }
        public string? CollectionCenterName { get; set; }
    }

    public class LoanFineInterestPrinciplePaymentSummaryData
    {
        public List<LoanFineInterestPrinciplePaymentSummaryRowDto> Rows { get; set; } = [];
        public int TotalRecords { get; set; }
        public decimal TotalFine { get; set; }
        public decimal TotalInterest { get; set; }
        public decimal TotalPrincipal { get; set; }
        public decimal TotalAmount { get; set; }
        public string? FromDateBs { get; set; }
        public string? ToDateBs { get; set; }
        public string? BranchName { get; set; }
        public string? CollectionCenterName { get; set; }
        public string? CollectorName { get; set; }
        public string? SelectAllOrOnlyCash { get; set; }
        public string? OrderBy { get; set; }
    }
}