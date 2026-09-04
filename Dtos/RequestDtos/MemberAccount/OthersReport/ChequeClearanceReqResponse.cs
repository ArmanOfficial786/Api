namespace NexgenCosysReport.Dtos.RequestDtos.MemberAccount.OthersReport
{

    public class ChequeClearanceRequestDto
    {
        public string FromDateBs { get; set; } = string.Empty;
        public string ToDateBs { get; set; } = string.Empty;
        public string ChequeType { get; set; } = "Get All"; // Get All, Receive Cheque, Deleted Cheque, Send Cheque, Clearance Cheque, Rejected Cheque
        public bool VisualReport { get; set; } = false;
    }

    public class ChequeClearanceRowDto
    {
        public long? ChequeClearanceId { get; set; }
        public string? MemberId { get; set; }
        public string? MemberName { get; set; }
        public string? AccountNo { get; set; }
        public long? ChequeNo { get; set; }
        public string? ChequeDate { get; set; }
        public string? ChequeDateBs { get; set; }
        public string? ReceiveDate { get; set; }
        public string? ReceiveDateBs { get; set; }
        public string? SendDate { get; set; }
        public string? SendDateBs { get; set; }
        public string? ClearanceDate { get; set; }
        public string? ClearanceDateBs { get; set; }
        public string? RejectedDate { get; set; }
        public string? RejectedDateBs { get; set; }
        public string? DeletedDate { get; set; }
        public string? DeletedDateBs { get; set; }
        public string? Status { get; set; }
        public string? ReceiveBy { get; set; }
        public string? SendBy { get; set; }
        public string? ClearanceBy { get; set; }
        public string? RejectedBy { get; set; }
        public string? DeletedBy { get; set; }
        public string? Remarks { get; set; }
        public string? BranchName { get; set; }
        public decimal? Amount { get; set; }
        public string? BankName { get; set; }
        public string? ChequeType { get; set; }
    }

    public class ChequeClearanceData
    {
        public List<ChequeClearanceRowDto> Rows { get; set; } = new();
        public int TotalRecords { get; set; }
        public int TotalCheques { get; set; }
        public string? FromDateBs { get; set; }
        public string? ToDateBs { get; set; }
        public string? ChequeType { get; set; }
        public string? ChequeTypeDisplayName { get; set; }
        public int ReceivedCount { get; set; }
        public int SendCount { get; set; }
        public int ClearedCount { get; set; }
        public int RejectedCount { get; set; }
        public int DeletedCount { get; set; }
        public decimal TotalAmount { get; set; }
    }
    public class ChequeClearanceReqResponse
    {
    }
}
