namespace NexgenCosysReport.Dtos.RequestDtos.MemberAccount.InterestExpenseReport
{

    public class FixedDepositInterestTransferRequestDto
    {
        public string FromDateBs { get; set; } = string.Empty;
        public string ToDateBs { get; set; } = string.Empty;
        public string BranchIds { get; set; } = "-1";
        public string OrderBy { get; set; } = "MemberId";
        public string BranchName { get; set; } = "All Branches";
        public bool VisualReport { get; set; } = false;
    }

    public class FixedDepositInterestTransferRowDto
    {
        public string? MemberId { get; set; }
        public string? MemberName { get; set; }
        public string? AccountNo { get; set; }
        public string? DepositTypeName { get; set; }
        public string? InterestDate { get; set; }
        public string? InterestDateBs { get; set; }
        public decimal? InterestRate { get; set; }
        public decimal? InterestAmount { get; set; }
        public decimal? TaxAmount { get; set; }
        public decimal? NetAmount { get; set; }
        public string? Remarks { get; set; }
        public string? Operator { get; set; }
    }

    public class FixedDepositInterestTransferData
    {
        public List<FixedDepositInterestTransferRowDto> Rows { get; set; } = new();
        public int TotalRecords { get; set; }
        public decimal TotalInterestAmount { get; set; }
        public decimal TotalTaxAmount { get; set; }
        public decimal TotalNetAmount { get; set; }
        public string? FromDateBs { get; set; }
        public string? ToDateBs { get; set; }
        public string? BranchNames { get; set; }
        public string? OrderBy { get; set; }
        public int TotalTransactions { get; set; }
    }
    public class FixedDepositInterestTransferReqResponse
    {
    }
}
