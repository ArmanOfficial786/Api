//// Dtos/RequestDtos/Microfinance/MicrofinanceSheetReports/CenterCollectionSheetPrintRequestDto.cs
//namespace NexgenCosysReport.Dtos.RequestDtos.Microfinance.MicrofinanceSheetReports
//{
//    public class CenterCollectionSheetPrintRequestDto
//    {
//        public string TillDateBs { get; set; } = string.Empty;
//        public string? CollectionCenterId { get; set; }
//        public string SheetType { get; set; } = "A"; // A = Saving+Loan, S = Saving, L = Loan, LS = Three-Col
//        public bool VisualReport { get; set; } = false;
//    }

//    // Header (first result set from the SP)
//    public class CenterCollectionSheetHeaderDto
//    {
//        public string? CollectionCenterShortCode { get; set; }
//        public string? CollectionCenterName { get; set; }
//        public string? OfficeName { get; set; }
//        public string? Address { get; set; }
//        public string? ContactNo { get; set; }
//        public string? VDCName { get; set; }
//        public string? AgentName { get; set; }
//    }

//    // Unified sheet row: covers Saving + Loan + Combined layouts
//    public class CenterCollectionSheetRowDto
//    {
//        // Common
//        public string? GroupCode { get; set; }
//        public string? GroupName { get; set; }
//        public long? MemMemberRegistrationId { get; set; }
//        public string? MemberId { get; set; }
//        public string? MemberName { get; set; }

//        // Saving columns
//        public decimal? SavingRemBalance { get; set; }
//        public decimal? SavingPayableDeposit { get; set; }
//        public decimal? SavingPayableWithdrawal { get; set; }
//        public decimal? SavingPayableInt { get; set; }

//        // Loan #1
//        public string? Loan1LoanType { get; set; }
//        public string? Loan1AcNo { get; set; }
//        public decimal? Loan1RemPrinciple { get; set; }
//        public decimal? Loan1PayablePri { get; set; }
//        public decimal? Loan1PayableInt { get; set; }

//        // Loan #2
//        public string? Loan2LoanType { get; set; }
//        public string? Loan2AcNo { get; set; }
//        public decimal? Loan2RemPrinciple { get; set; }
//        public decimal? Loan2PayablePri { get; set; }
//        public decimal? Loan2PayableInt { get; set; }

//        // Loan #3
//        public string? Loan3LoanType { get; set; }
//        public string? Loan3AcNo { get; set; }
//        public decimal? Loan3RemPrinciple { get; set; }
//        public decimal? Loan3PayablePri { get; set; }
//        public decimal? Loan3PayableInt { get; set; }

//        // Loan #4
//        public string? Loan4LoanType { get; set; }
//        public string? Loan4AcNo { get; set; }
//        public decimal? Loan4RemPrinciple { get; set; }
//        public decimal? Loan4PayablePri { get; set; }
//        public decimal? Loan4PayableInt { get; set; }
//    }

//    public class CenterCollectionSheetPrintData
//    {
//        public CenterCollectionSheetHeaderDto? Header { get; set; }
//        public List<CenterCollectionSheetRowDto> Rows { get; set; } = [];
//        public int TotalRecords { get; set; }
//        public decimal TotalSavingRemBalance { get; set; }
//        public decimal TotalSavingPayableDeposit { get; set; }
//        public decimal TotalSavingPayableWithdrawal { get; set; }
//        public decimal TotalSavingPayableInt { get; set; }
//        public decimal TotalLoanRemPrinciple { get; set; }
//        public decimal TotalLoanPayablePri { get; set; }
//        public decimal TotalLoanPayableInt { get; set; }

//        public string? TillDateBs { get; set; }
//        public string? TillDateAd { get; set; }
//        public string? SheetType { get; set; }
//        public string? SheetTypeName { get; set; }
//    }
//}



// Dtos/RequestDtos/Microfinance/MicrofinanceSheetReports/CenterCollectionSheetPrintRequestDto.cs
namespace NexgenCosysReport.Dtos.RequestDtos.Microfinance.MicrofinanceSheetReports
{
    public class CenterCollectionSheetPrintRequestDto
    {
        public string TillDateBs { get; set; } = string.Empty;
        public string? CollectionCenterId { get; set; }
        public string SheetType { get; set; } = "A";
        public bool VisualReport { get; set; } = false;
    }

    public class CenterCollectionSheetHeaderDto
    {
        public string? CollectionCenterShortCode { get; set; }
        public string? CollectionCenterName { get; set; }
        public string? OfficeName { get; set; }
        public string? Address { get; set; }
        public string? ContactNo { get; set; }
        public string? VDCName { get; set; }
        public string? AgentName { get; set; }
    }

    // Matches sp_5_43_GetCenterCollectionSheetPrintReport's #tempMember columns
    // exactly. Previous version only declared a single SavingRemBalance field
    // with no account number — the SP actually returns FOUR saving slots
    // (Saving1AcNo/Saving1Balance ... Saving4AcNo/Saving4Balance), which
    // Dapper silently left unbound. Loan1..Loan4 slots were already correct
    // (Dapper's binding is case-insensitive, so Loan1payablePri vs
    // Loan1PayablePri already matched).
    public class CenterCollectionSheetRowDto
    {
        public string? GroupCode { get; set; }
        public string? GroupName { get; set; }
        public long? MemMemberRegistrationId { get; set; }
        public string? MemberId { get; set; }
        public string? MemberName { get; set; }

        public string? Saving1AcNo { get; set; }
        public decimal? Saving1Balance { get; set; }
        public string? Saving2AcNo { get; set; }
        public decimal? Saving2Balance { get; set; }
        public string? Saving3AcNo { get; set; }
        public decimal? Saving3Balance { get; set; }
        public string? Saving4AcNo { get; set; }
        public decimal? Saving4Balance { get; set; }

        public string? Loan1LoanType { get; set; }
        public string? Loan1AcNo { get; set; }
        public decimal? Loan1RemPrinciple { get; set; }
        public decimal? Loan1PayablePri { get; set; }
        public decimal? Loan1PayableInt { get; set; }

        public string? Loan2LoanType { get; set; }
        public string? Loan2AcNo { get; set; }
        public decimal? Loan2RemPrinciple { get; set; }
        public decimal? Loan2PayablePri { get; set; }
        public decimal? Loan2PayableInt { get; set; }

        public string? Loan3LoanType { get; set; }
        public string? Loan3AcNo { get; set; }
        public decimal? Loan3RemPrinciple { get; set; }
        public decimal? Loan3PayablePri { get; set; }
        public decimal? Loan3PayableInt { get; set; }

        public string? Loan4LoanType { get; set; }
        public string? Loan4AcNo { get; set; }
        public decimal? Loan4RemPrinciple { get; set; }
        public decimal? Loan4PayablePri { get; set; }
        public decimal? Loan4PayableInt { get; set; }
    }

    public class CenterCollectionSheetPrintData
    {
        public CenterCollectionSheetHeaderDto? Header { get; set; }
        public List<CenterCollectionSheetRowDto> Rows { get; set; } = [];
        public int TotalRecords { get; set; }

        public decimal TotalSaving1Balance { get; set; }
        public decimal TotalSaving2Balance { get; set; }
        public decimal TotalSaving3Balance { get; set; }
        public decimal TotalSaving4Balance { get; set; }
        public decimal TotalLoan1RemPrinciple { get; set; }
        public decimal TotalLoan1PayablePri { get; set; }
        public decimal TotalLoan1PayableInt { get; set; }
        public decimal TotalLoan2RemPrinciple { get; set; }
        public decimal TotalLoan2PayablePri { get; set; }
        public decimal TotalLoan2PayableInt { get; set; }
        public decimal TotalLoan3RemPrinciple { get; set; }
        public decimal TotalLoan3PayablePri { get; set; }
        public decimal TotalLoan3PayableInt { get; set; }
        public decimal TotalLoan4RemPrinciple { get; set; }
        public decimal TotalLoan4PayablePri { get; set; }
        public decimal TotalLoan4PayableInt { get; set; }

        public string? TillDateBs { get; set; }
        public string? TillDateAd { get; set; }
        public string? SheetType { get; set; }
        public string? SheetTypeName { get; set; }
    }
}