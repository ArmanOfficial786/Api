namespace NexgenCosysReport.Dtos.RequestDtos.Loan.OtherReports
{
    public class LoanCommissionRequestDto
    {
        public string? FromDateBs { get; set; }
        public string? ToDateBs { get; set; }
        public long CollectorId { get; set; } = -1;
        public bool VisualReport { get; set; } = false;
    }

    // Columns match sp_7_16_LoanCommissionReport's output SELECT list
    public class LoanCommissionRowDto
    {
        public string? MemberId { get; set; }
        public string? FullName { get; set; }
        public string? LoanAccountNo { get; set; }
        public string? CollectorFullName { get; set; }
        public decimal? TotalInterestAmount { get; set; }
        public decimal? CommissionAmount { get; set; }
        public string? DateOnBs { get; set; }
    }

    public class LoanCommissionData
    {
        public List<LoanCommissionRowDto> Rows { get; set; } = [];
        public int TotalRecords { get; set; }
        public decimal TotalInterestAmount { get; set; }
        public decimal TotalCommissionAmount { get; set; }
        public string? FromDateBs { get; set; }
        public string? ToDateBs { get; set; }
        public string? CollectorName { get; set; }
    }
}