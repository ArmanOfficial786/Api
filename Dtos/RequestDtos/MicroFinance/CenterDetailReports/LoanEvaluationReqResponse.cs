// Dtos/RequestDtos/Microfinance/CenterDetailReports/LoanEvaluationRequestDto.cs
namespace NexgenCosysReport.Dtos.RequestDtos.Microfinance.CenterDetailReports
{
    public class LoanEvaluationReqResponse
    {
        public long? MemberRegistrationId { get; set; }
        public string? MemberId { get; set; }
        public bool VisualReport { get; set; } = false;
    }


    public class LoanEvaluationMemberDto
    {
        public string? MemberId { get; set; }
        public string? MemberName { get; set; }
        public string? CenterCode { get; set; }
        public string? CenterName { get; set; }
        public string? GroupCode { get; set; }
        public string? GroupName { get; set; }
        public string? DistrictName { get; set; }
        public string? VDCName { get; set; }
        public string? PermanentAddress { get; set; }
        public string? TemporaryAddress { get; set; }
        public string? ContactNo { get; set; }
    }


    public class LoanEvaluationLoanDetailDto
    {
        public int? LoanTypeId { get; set; }
        public string? LoanType { get; set; }
        public decimal? Saving { get; set; }
        public decimal? Share { get; set; }
        public decimal? LoanIssue { get; set; }
        public decimal? LoanRemaining { get; set; }
        public decimal? LoanDue { get; set; }
        public decimal? Total { get; set; }
    }

    public class LoanEvaluationAssetLedgerDto
    {
        public string? AssetLedger { get; set; }
        public string? LastColumn { get; set; }
        public string? NumberArea { get; set; }
        public decimal? Cost { get; set; }
    }

    public class LoanEvaluationMeetingDto
    {
        public int? MeetingCount { get; set; }
        public int? MeetingAttendedCount { get; set; }
        public decimal? MeetingPercent { get; set; }
        public string? LateForLast2Loan { get; set; }
        public int? LateLoanPaymentForAYear { get; set; }
    }


    public class LoanEvaluationIncomeDto
    {
        public string? IncomeLedger { get; set; }
        public decimal? Amount { get; set; }
    }


    public class LoanEvaluationExpenseDto
    {
        public string? ExpenseLedger { get; set; }
        public decimal? Amount { get; set; }
    }

    public class LoanEvaluationLoanIssueDto
    {
        public string? LoanIssueOnBS { get; set; }
        public decimal? LoanIssueAmount { get; set; }
    }

    public class LoanEvaluationData
    {
        public LoanEvaluationMemberDto? Member { get; set; }
        public List<LoanEvaluationLoanDetailDto> LoanDetails { get; set; } = [];
        public List<LoanEvaluationAssetLedgerDto> AssetLedger { get; set; } = [];
        public LoanEvaluationMeetingDto? Meeting { get; set; }
        public List<LoanEvaluationIncomeDto> Incomes { get; set; } = [];
        public List<LoanEvaluationExpenseDto> Expenses { get; set; } = [];
        public List<LoanEvaluationLoanIssueDto> LoanIssues { get; set; } = [];

        public decimal TotalSaving { get; set; }
        public decimal TotalShare { get; set; }
        public decimal TotalLoanIssue { get; set; }
        public decimal TotalLoanRemaining { get; set; }
        public decimal TotalLoanDue { get; set; }
        public decimal GrandTotal { get; set; }
        public decimal TotalAssetCost { get; set; }
        public decimal TotalIncome { get; set; }
        public decimal TotalExpense { get; set; }
    }
}