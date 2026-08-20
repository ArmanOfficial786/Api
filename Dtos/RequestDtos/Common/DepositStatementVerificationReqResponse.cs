namespace NexgenCosysReport.Dtos.RequestDtos.Common
{
    public class DepositStatementVerifyRequestDto
    {
        public long MamAccountOpeningId { get; set; }
        public string AccountNo { get; set; } = string.Empty;
        public string VerifiedFromDateOnBs { get; set; } = string.Empty;
        public string VerifiedToDateOnBs { get; set; } = string.Empty;
    }

    public class VerificationStatusDto
    {
        public bool HasVerification { get; set; }
        public string? VerifiedTillBs { get; set; }
        public string? VerifiedDateBs { get; set; }
        public string? VerifiedBy { get; set; }
        public string? Message { get; set; }
    }

    public class DepositStatementVerificationDto
    {
        public long MamDepositStatementVerificationId { get; set; }
        public string? VerifiedFromDateOnBs { get; set; }
        public string? VerifiedToDateOnBs { get; set; }
        public DateTime CreatedOn { get; set; }
        public string? VerifiedDateBs { get; set; }
        public string? VerifiedBy { get; set; }
    }


    public class DepositStatementVerificationReqResponse
    {
    }
}
