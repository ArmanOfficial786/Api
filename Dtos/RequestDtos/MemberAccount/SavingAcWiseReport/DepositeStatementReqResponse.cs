//namespace NexgenCosysReport.Dtos.RequestDtos.MemberAccount
//{

//    public class DepositStatementRequestDto
//    {
//        public string AccountNo { get; set; } = string.Empty;
//        public string FromDateBs { get; set; } = string.Empty;
//        public string ToDateBs { get; set; } = string.Empty;
//        public bool EnableInterest { get; set; } = false;
//        public bool EnableBillNumber { get; set; } = false;
//        public bool EntryBy { get; set; } = false;
//        public bool ValueDate { get; set; } = true;
//        public bool SameCompanyName { get; set; } = true;
//        public string Language { get; set; } = "English";
//        public bool CustomNarration { get; set; } = false;
//        public bool VisualReport { get; set; } = false;
//        public bool ViewInterest { get; set; } = false;
//        public bool NepaliDate { get; set; } = true;
//        public bool EnglishDate { get; set; } = false;
//    }
//    public class DepositStatementData
//    {
//        public List<DepositStatementRowDto> Rows { get; set; } = new();
//        public DepositStatementMemberDetailDto? MemberDetail { get; set; }
//        public decimal OpeningBalance { get; set; }
//        public decimal ClosingBalance { get; set; }
//        public decimal InterestAmount { get; set; }
//        public decimal TaxAmount { get; set; }
//        public string? AccountNo { get; set; }
//        public string? FromDateBs { get; set; }
//        public string? ToDateBs { get; set; }
//        public bool HasVerification { get; set; }
//        public string? VerifiedTillBs { get; set; }
//        public long? OfficeId { get; set; }
//        public string? OfficeName { get; set; }
//        public int TotalRecords { get; set; }
//        public long? MamAccountOpeningId { get; set; }
//    }

//    public class DepositStatementRowDto
//    {
//        public string? Date { get; set; }
//        public string? Particulars { get; set; }
//        public string? BillNo { get; set; }
//        public decimal? Deposit { get; set; }
//        public decimal? Withdraw { get; set; }
//        public decimal Balance { get; set; }
//        public string? EntryBy { get; set; }
//        public string? ValueDate { get; set; }
//    }

//    public class DepositStatementMemberDetailDto
//    {
//        public string? MemberId { get; set; }
//        public string? MemberName { get; set; }
//        public string? AccountNo { get; set; }
//        public string? DepositTypeName { get; set; }
//        public string? Address { get; set; }
//        public string? MobileNo { get; set; }
//        public string? AccountOpenDate { get; set; }
//        public string? OfficeName { get; set; }
//    }
//    public class DepositeStatementReqResponse
//    {
//    }
//}





// Dtos/RequestDtos/MemberAccount/DepositStatementRequestDto.cs
namespace NexgenCosysReport.Dtos.RequestDtos.MemberAccount.SavingAcWiseReport
{
    public class DepositStatementRequestDto
    {
        public string AccountNo { get; set; } = string.Empty;
        public string FromDateBs { get; set; } = string.Empty;
        public string ToDateBs { get; set; } = string.Empty;
        public bool EnableInterest { get; set; } = false;
        public bool EnableBillNumber { get; set; } = false;
        public bool EntryBy { get; set; } = false;
        public bool ValueDate { get; set; } = true;
        public bool SameCompanyName { get; set; } = true;
        public string Language { get; set; } = "English";
        public bool CustomNarration { get; set; } = false;
        public bool VisualReport { get; set; } = false;
        public bool ViewInterest { get; set; } = false;
        public bool NepaliDate { get; set; } = true;
        public bool EnglishDate { get; set; } = false;
    }

    public class DepositStatementData
    {
        public List<DepositStatementRowDto> Rows { get; set; } = new();
        public DepositStatementMemberDetailDto? MemberDetail { get; set; }
        public decimal OpeningBalance { get; set; }
        public decimal ClosingBalance { get; set; }
        public decimal InterestAmount { get; set; }
        public decimal TaxAmount { get; set; }
        public string? AccountNo { get; set; }
        public string? FromDateBs { get; set; }
        public string? ToDateBs { get; set; }
        public bool HasVerification { get; set; }
        public string? VerifiedTillBs { get; set; }
        public long? OfficeId { get; set; }
        public string? OfficeName { get; set; }
        public int TotalRecords { get; set; }
        public long? MamAccountOpeningId { get; set; }
    }

    public class DepositStatementRowDto
    {
        // sn is generated in the view (S.No), not bound from SQL
        public string? Date { get; set; }        // <- bind from SP's "DateEng"
        public string? ValueDate { get; set; }    // <- bind from SP's "ValueDate" / "ValueDateBs"
        public string? Particulars { get; set; }  // <- bind from SP's "Description"
        public string? BillNo { get; set; }
        public decimal? Deposit { get; set; }
        public decimal? Withdraw { get; set; }    // <- bind from SP's "Withdrawl"
        public decimal Balance { get; set; }
        public string? EntryBy { get; set; }
    }

    public class DepositStatementMemberDetailDto
    {
        public string? MemberId { get; set; }
        public string? MemberName { get; set; }
        public string? AccountNo { get; set; }
        public string? DepositTypeName { get; set; }   // A/C Type
        public string? Address { get; set; }           // <- bind from SP's "MemberAddress"
        public string? MobileNo { get; set; }           // <- bind from SP's "PhoneNo"
        public string? AccountOpenDate { get; set; }    // <- bind from SP's "AccountOpenOnBs"
        public string? OfficeName { get; set; }
        public decimal? InterestRate { get; set; }
    }

    public class DepositeStatementReqResponse
    {
    }
}