namespace NexgenCosysReport.Dtos.RequestDtos.MemberAccount.OthersReport
{
    public class TellerWiseCollectionRequestDto
    {
        public string FromDateBs { get; set; } = string.Empty;
        public string ToDateBs { get; set; } = string.Empty;
        public long? TellerId { get; set; }
        public string OrderBy { get; set; } = "Account No";
        public bool SameCompanyName { get; set; } = true;
        public bool VisualReport { get; set; } = false;
    }
    public class TellerWiseCollectionRowDto
    {
        public string? MemberName { get; set; }
        public string? TellerName { get; set; }
        public string? MemberId { get; set; }
        public string? Date { get; set; }
        public string? BillNo { get; set; }
        public string? Details { get; set; }
        public decimal? ShareAmount { get; set; }
        public decimal? SavingAmount { get; set; }
        public decimal? LoanPrinciple { get; set; }
        public decimal? LoanInterest { get; set; }
        public decimal? LoanPenalty { get; set; }
        public decimal? MiscellaneousAmount { get; set; }

        // Not from SQL - computed for display
        public decimal RowTotal =>
            (ShareAmount ?? 0) + (SavingAmount ?? 0) + (LoanPrinciple ?? 0) +
            (LoanInterest ?? 0) + (LoanPenalty ?? 0) + (MiscellaneousAmount ?? 0);
    }
    public class TellerWiseCollectionData
    {
        public List<TellerWiseCollectionRowDto> Rows { get; set; } = new();
        public int TotalRecords { get; set; }

        public decimal TotalShareAmount { get; set; }
        public decimal TotalSavingAmount { get; set; }
        public decimal TotalLoanPrinciple { get; set; }
        public decimal TotalLoanInterest { get; set; }
        public decimal TotalLoanPenalty { get; set; }
        public decimal TotalMiscellaneousAmount { get; set; }
        public decimal TotalAmount { get; set; } // grand total (sum of all columns)

        public string FromDateBs { get; set; } = string.Empty;
        public string ToDateBs { get; set; } = string.Empty;
        public long? TellerId { get; set; }
        public string? TellerName { get; set; }
        public string OrderBy { get; set; } = string.Empty;
    }

    public class TellerWiseCollectionReqResponse
    {
    }
}
