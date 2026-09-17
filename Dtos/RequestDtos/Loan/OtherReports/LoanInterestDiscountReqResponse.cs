namespace NexgenCosysReport.Dtos.RequestDtos.Loan.OtherReports
{
    public class LoanInterestDiscountRequestDto
    {
        public string? MemberId { get; set; }
        public long LoanTypeId { get; set; } = -1;
        public string? FromDateBs { get; set; }
        public string? ToDateBs { get; set; }
        public string? BranchIds { get; set; }
        public string OrderBy { get; set; } = "-1";
        public bool VisualReport { get; set; } = false;
    }
    public class LoanInterestDiscountRowDto
    {
        public string? MemberId { get; set; }
        public string? FullName { get; set; }
        public string? LoanAccountNo { get; set; }
        public string? LoanTypeName { get; set; }
        public decimal? CashReceived { get; set; }
        public string? Operator { get; set; }
        public string? TransactionOnBs { get; set; }
    }

    public class LoanInterestDiscountData
    {
        public List<LoanInterestDiscountRowDto> Rows { get; set; } = [];
        public int TotalRecords { get; set; }
        public decimal TotalAmount { get; set; }
        public string? FromDateBs { get; set; }
        public string? ToDateBs { get; set; }
        public string? BranchName { get; set; }
        public string? MemberName { get; set; }
        public string? LoanTypeName { get; set; }
        public string? OrderBy { get; set; }
    }
}