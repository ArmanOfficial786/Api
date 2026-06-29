//// Dtos/RequestDtos/Member/MemberDetailsSummaryDtos.cs
//namespace NexgenCosysReport.Dtos.RequestDtos.Member
//{


//    // Main response DTO for the report (matches repository return type)
//    public class MemberDetailsSummarySpResponse
//    {
//        public MemberDetailInfoDto? MemberInfo { get; set; }
//        public List<ShareAccountDto> ShareAccounts { get; set; } = new();
//        public List<SavingAccountDto> SavingAccounts { get; set; } = new();
//        public List<LoanIssueDto> LoanIssues { get; set; } = new();
//        public List<GroupGuaranteeDto> GroupGuarantees { get; set; } = new();
//        public int TotalShareRecords { get; set; }
//        public int TotalSavingRecords { get; set; }
//        public int TotalLoanRecords { get; set; }
//        public int TotalGuaranteeRecords { get; set; }
//    }

//    // DTOs for each section – adjust property names to match SP output
//    public class MemberDetailInfoDto
//    {
//        public string? MemberId { get; set; }
//        public string? Name { get; set; }
//        public string? DateOfBirth { get; set; }   // Nepali date string
//        public string? Sex { get; set; }
//        public string? Caste { get; set; }
//        public string? Religion { get; set; }
//        public string? EmailId { get; set; }
//        public string? MobileNo { get; set; }
//        public string? PhoneNo { get; set; }
//        public string? Nationality { get; set; }
//        public string? Occupation { get; set; }
//        public string? CitizenshipNo { get; set; }
//        public string? PassportNo { get; set; }
//        public string? GrandFatherName { get; set; }
//        public string? FatherName { get; set; }
//        public string? MotherName { get; set; }
//        public string? Zone { get; set; }
//        public string? District { get; set; }
//        public string? VDC { get; set; }
//        public string? PermanentAddress { get; set; }
//        public string? RegistrationDate { get; set; } // Nepali date
//    }

//    public class ShareAccountDto
//    {
//        public string? AccountNo { get; set; }
//        public string? ShareType { get; set; }
//        public decimal ShareAmount { get; set; }
//        public string? IssueDate { get; set; } // Nepali date
//        // add others if needed
//    }

//    public class SavingAccountDto
//    {
//        public string? AccountNo { get; set; }
//        public string? AccountType { get; set; }
//        public decimal Balance { get; set; }
//        public string? OpeningDate { get; set; } // Nepali date
//    }

//    public class LoanIssueDto
//    {
//        public string? LoanAccountNo { get; set; }
//        public string? LoanType { get; set; }
//        public decimal LoanAmount { get; set; }
//        public string? IssueDate { get; set; } // Nepali date
//    }

//    public class GroupGuaranteeDto
//    {
//        public string? GroupName { get; set; }
//        public decimal GuaranteeAmount { get; set; }
//        public string? GuaranteeDate { get; set; } // Nepali date
//    }
//}


// Dtos/RequestDtos/Member/MemberDetailsSummaryDtos.cs
namespace NexgenCosysReport.Dtos.RequestDtos.Member
{

    public class MemberDetailsSummaryRequest
    {
        public long MemberRegistrationId { get; set; }
        public string? FromDate { get; set; }
        public string? ToDate { get; set; }
        public string? OrderBy { get; set; }
        public bool VisualReport { get; set; } = false;
    }
    // ---------- Main Response ----------
    public class MemberDetailsSummarySpResponse
    {
        public MemberDetailInfoDto? MemberInfo { get; set; }
        public List<ShareAccountDto> ShareAccounts { get; set; } = new();
        public List<SavingAccountDto> SavingAccounts { get; set; } = new();
        public List<LoanIssueDto> LoanIssues { get; set; } = new();
        public List<GroupGuaranteeDto> GroupGuarantees { get; set; } = new();
        public int TotalShareRecords { get; set; }
        public int TotalSavingRecords { get; set; }
        public int TotalLoanRecords { get; set; }
        public int TotalGuaranteeRecords { get; set; }
    }

    // ---------- Member Info ----------
    public class MemberDetailInfoDto
    {
        public string? MemberId { get; set; }
        public string? Name { get; set; }
        public string? Sex { get; set; }
        public string? DateOfBirth { get; set; }
        public string? RegistrationDate { get; set; }
        public string? MobileNo { get; set; }
        public string? PermanentAddress { get; set; }
        public string? CitizenshipNo { get; set; }
        public string? Caste { get; set; }
        public string? Religion { get; set; }
        public string? EmailId { get; set; }
        public string? PhoneNo { get; set; }
        public string? Nationality { get; set; }
        public string? Occupation { get; set; }
        public string? PassportNo { get; set; }
        public string? GrandFatherName { get; set; }
        public string? FatherName { get; set; }
        public string? MotherName { get; set; }
        public string? Zone { get; set; }
        public string? District { get; set; }
        public string? VDC { get; set; }
    }

    // ---------- Share Accounts ----------
    public class ShareAccountDto
    {
        public string? ShareHolder { get; set; }
        public decimal PreviousShare { get; set; }
        public decimal SharePurchase { get; set; }
        public decimal ShareReturn { get; set; }
        public decimal TotalShare { get; set; }     // used for total
        public decimal ShareAmount { get; set; }    // added for compatibility with view
        // If the SP returns 'ShareAmount' but not 'TotalShare', you can map it via alias.
        // We'll keep both for flexibility.
    }

    // ---------- Saving Accounts ----------
    public class SavingAccountDto
    {
        public string? AccountNo { get; set; }
        public string? AccountType { get; set; }
        public string? Status { get; set; }
        public decimal? InterestRate { get; set; }   // nullable so we can use ?.ToString()
        public string? OpeningDate { get; set; }
        public decimal OpeningBalance { get; set; }
        public decimal DepositAmount { get; set; }
        public decimal WithdrawAmount { get; set; }
        public decimal Balance { get; set; }
    }

    // ---------- Loan Issues ----------
    public class LoanIssueDto
    {
        public string? LoanAccountNo { get; set; }
        public string? IssueDate { get; set; }
        public string? MaturityDate { get; set; }
        public decimal PrincipleDue { get; set; }
        public decimal InterestDue { get; set; }
        public decimal PenaltyDue { get; set; }
        public decimal TotalDue { get; set; }
        public decimal LoanAmount { get; set; }
        public decimal PrincipalPaid { get; set; }
        public decimal InterestPaid { get; set; }
        public decimal PenaltyPaid { get; set; }
    }

    // ---------- Group Guarantees ----------
    public class GroupGuaranteeDto
    {
        public string? AccountNo { get; set; }
        public string? LoanType { get; set; }
        public string? SavingAccountNo { get; set; }
        public decimal SavingAmount { get; set; }
        public decimal ShareAmount { get; set; }
        public string? GuaranteeDate { get; set; }
        public string? LoneyId { get; set; }
        public string? LoneyName { get; set; }
        public decimal LoanIssueAmount { get; set; }
        public decimal GuaranteeAmount { get; set; }  // added for view compatibility
    }
}