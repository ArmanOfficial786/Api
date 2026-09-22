namespace NexgenCosysReport.Dtos.RequestDtos.Loan.OtherReports
{
    public class LoanPenaltyDiscountRequestDto
    {
        public string? MemberId { get; set; }
        public long LoanTypeId { get; set; } = -1;
        public string? FromDateBs { get; set; }
        public string? ToDateBs { get; set; }
        public string? BranchIds { get; set; }
        public bool VisualReport { get; set; }
        public string OrderBy { get; set; } = "-1";
    }

    public class LoanPenaltyDiscountRowDto
    {
        public string? MemberId { get; set; }
        public string? FullName { get; set; }
        public string? LoanAccountNo { get; set; }
        public string? LoanTypeName { get; set; }
        public decimal? CashReceived { get; set; }
        public string? Operator { get; set; }
        public string? TransactionOnBs { get; set; }
    }

    public class LoanPenaltyDiscountData
    {
        public List<LoanPenaltyDiscountRowDto> Rows { get; set; } = [];
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