namespace NexgenCosysReport.Dtos.RequestDtos.Remit
{
    public class RemitReconcileReportRequestDto
    {
        public string? FromDateBs { get; set; }
        public string? ToDateBs { get; set; }
        public long RemittanceGroupId { get; set; }
        public string ReportType { get; set; } = "A";
        public string? SessionId { get; set; }
        public bool VisualReport { get; set; } = false;
    }

    public class RemitReconcileReportRowDto
    {
        public string? Code { get; set; }
        public string? AgentName { get; set; }
        public string? AgentBranch { get; set; }
        public string? TransactionMode { get; set; }
        public string? PinNo { get; set; }
        public string? SenderName { get; set; }
        public string? SenderAddress { get; set; }
        public string? SenderCity { get; set; }
        public string? SenderMobile { get; set; }
        public string? SenderCountry { get; set; }
        public string? ReceiverName { get; set; }
        public string? ReceiverAddress { get; set; }
        public string? ReceiverCity { get; set; }
        public string? ReceiverCountry { get; set; }
        public string? ReceiverMobile { get; set; }
        public string? PayoutAmount { get; set; }
        public string? PayoutCcy { get; set; }
        public string? PayoutCommission { get; set; }
        public string? TransactionDate { get; set; }
        public string? Status { get; set; }
        public string? PaidDate { get; set; }
        public string? PayoutAgent { get; set; }
        public string? PaymentType { get; set; }
        public string? CancelDate { get; set; }
        public string? Message { get; set; }
        public string? AgentTransactionRefId { get; set; }
        public string? PayoutBranchId { get; set; }
        public string? PaidUserId { get; set; }
        public string? TransactionType { get; set; }
    }

    public class RemitReconcileReportData
    {
        public List<RemitReconcileReportRowDto> Rows { get; set; } = [];
        public int TotalRecords { get; set; }
        public int TotalPaid { get; set; }
        public int TotalCancelled { get; set; }
        public decimal TotalPayoutAmount { get; set; }
        public decimal TotalPayoutCommission { get; set; }
        public string? FromDateBs { get; set; }
        public string? ToDateBs { get; set; }
        public string? BranchName { get; set; }
        public string? RemittanceGroupName { get; set; }
        public string? ReportType { get; set; }
    }

    public class RemitReconcileMiddlewareRequestDto
    {
        public string accessCode { get; set; } = string.Empty;
        public string userName { get; set; } = string.Empty;
        public string sessionId { get; set; } = string.Empty;
        public string fromDate { get; set; } = string.Empty;
        public string fromTime { get; set; } = string.Empty;
        public string toDate { get; set; } = string.Empty;
        public string toTime { get; set; } = string.Empty;
        public string reportType { get; set; } = string.Empty;
        public string signature { get; set; } = string.Empty;
    }

    public class RemitReconcileMiddlewareResponseDto
    {
        public List<RemitReconcileReportRowDto> transactionReports { get; set; } = [];
    }

    public class RemitReconcileMiddlewareWrapperDto
    {
        public int StatusCode { get; set; }
        public string? Message { get; set; }
        public bool IsValid { get; set; }
        public RemitReconcileMiddlewareResponseDto? Properties { get; set; }
    }
}
