// Dtos/RequestDtos/Loan/LoanAnalysisReport/LoanDetailsRequestDto.cs
namespace NexgenCosysReport.Dtos.RequestDtos.Loan.LoanAnalysisReport
{
    public class LoanDetailsRequestDto
    {
        public string FromDateBs { get; set; } = string.Empty;
        public string ToDateBs { get; set; } = string.Empty;
        public string? BranchIds { get; set; }
        public string? LoanTypeId { get; set; } = "-1";
        public string? Status { get; set; }
        public string? CollectionCenterId { get; set; } = "-1";
        public bool EnableCollectionCenter { get; set; } = false;
        public string? CollectorId { get; set; } = "-1";
        public string OrderBy { get; set; } = "-1";
        public string ReportType { get; set; } = "D";
        public bool VisualReport { get; set; } = false;
    }

    public class LoanDetailsRowDto
    {
        public string? MemberId { get; set; }
        public string? MemberIdFirst { get; set; }
        public decimal? MemberIdLast { get; set; }
        public string? FullName { get; set; }
        public string? LoanAccountNo { get; set; }
        public string? LoanTypeName { get; set; }
        public decimal? DisburseAmount { get; set; }
        public decimal? RepaidTill { get; set; }
        public decimal? Repaid { get; set; }
        public decimal? BalanceAmount { get; set; }
        public string? CollectionCenterName { get; set; }
        public string? PaymentMode { get; set; }
    }

    public class LoanDetailsData
    {
        public List<LoanDetailsRowDto> Rows { get; set; } = [];
        public int TotalRecords { get; set; }
        public decimal TotalDisburseAmount { get; set; }
        public decimal TotalRepaidTill { get; set; }
        public decimal TotalRepaid { get; set; }
        public decimal TotalBalanceAmount { get; set; }
        public string? FromDateBs { get; set; }
        public string? ToDateBs { get; set; }
        public string? BranchName { get; set; }
        public string? LoanTypeName { get; set; }
        public string? StatusName { get; set; }
        public string? CollectionCenterName { get; set; }
        public string? CollectorName { get; set; }
        public string? OrderBy { get; set; }
        public string? ReportType { get; set; }
    }
}