//namespace NexgenCosysReport.Dtos.RequestDtos.MemberAccount.OthersReport
//{

//    public class LoanPaymentThroughSavingRequestDto
//    {
//        public string FromDateBs { get; set; } = string.Empty;
//        public string ToDateBs { get; set; } = string.Empty;
//        public string? BranchIds { get; set; }
//        public string OrderBy { get; set; } = "Member Id";
//        public string ReportView { get; set; } = "Vertical";
//        public bool VisualReport { get; set; } = false;
//    }


//    public class LoanPaymentThroughSavingRowDto
//    {
//        public string? Date { get; set; }
//        public string? MemberId { get; set; }
//        public string? MemberName { get; set; }
//        public string? AccountNo { get; set; }
//        public string? Type { get; set; }
//        public decimal? NetAmount { get; set; }
//        public string? Operator { get; set; }
//        public string? Details { get; set; }
//    }

//    public class LoanPaymentThroughSavingData
//    {
//        public List<LoanPaymentThroughSavingRowDto> Rows { get; set; } = [];
//        public int TotalRecords { get; set; }
//        public decimal TotalNetAmount { get; set; }
//        public string? FromDateBs { get; set; }
//        public string? ToDateBs { get; set; }
//        public string? BranchNames { get; set; }
//        public string? BranchIds { get; set; }
//        public string? OrderBy { get; set; }
//        public string? ReportView { get; set; }
//    }

//    public class LoanPaymentThroughSavingReqResponse
//    {
//    }
//}







namespace NexgenCosysReport.Dtos.RequestDtos.MemberAccount.OthersReport
{
    public class LoanPaymentThroughSavingRequestDto
    {
        public string FromDateBs { get; set; } = string.Empty;
        public string ToDateBs { get; set; } = string.Empty;
        public string? BranchIds { get; set; }
        public string OrderBy { get; set; } = "Member Id";
        public string ReportView { get; set; } = "Vertical";
        public bool VisualReport { get; set; } = false;
    }

    public class LoanPaymentThroughSavingRowDto
    {
        public string? Date { get; set; }
        public string? MemberId { get; set; }
        public string? MemberName { get; set; }

        // Vertical view fields
        public string? AccountNo { get; set; }
        public string? Type { get; set; }

        // Horizontal view fields (image: Saving | Loan | Fine | Interest | Principle | Net Amount)
        public string? SavingAccountNo { get; set; }   // "Saving" column
        public string? LoanAccountNo { get; set; }      // "Loan" column
        public decimal? Fine { get; set; }
        public decimal? Interest { get; set; }
        public decimal? Principal { get; set; }

        // Shared
        public decimal? NetAmount { get; set; }
        public string? Operator { get; set; }
        public string? Details { get; set; }
    }

    public class LoanPaymentThroughSavingData
    {
        public List<LoanPaymentThroughSavingRowDto> Rows { get; set; } = [];
        public int TotalRecords { get; set; }
        public decimal TotalNetAmount { get; set; }
        public string? FromDateBs { get; set; }
        public string? ToDateBs { get; set; }
        public string? BranchNames { get; set; }
        public string? BranchIds { get; set; }
        public string? OrderBy { get; set; }
        public string? ReportView { get; set; }
    }

    public class LoanPaymentThroughSavingReqResponse
    {
    }
}