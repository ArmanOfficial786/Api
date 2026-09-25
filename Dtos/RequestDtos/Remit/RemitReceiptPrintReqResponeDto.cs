namespace NexgenCosysReport.Dtos.RequestDtos.Remit
{

    public class RemitReceiptPrintRequestDto
    {
        public string? FromDateBs { get; set; }
        public string? ToDateBs { get; set; }
        public string? BranchIds { get; set; }
        public string IsReceived { get; set; } = "0";
        public long RemittanceDetailId { get; set; } = -1;
        public bool VisualReport { get; set; } = false;
    }

    public class RemitReceiptPrintRowDto
    {
        public long? RemRemittanceDetailsId { get; set; }
        public string? IMECodeNo { get; set; }
        public string? SenderName { get; set; }
        public string? Name { get; set; }
        public string? Country { get; set; }
        public string? ReceiverName { get; set; }
        public string? ReceiverAddress { get; set; }
        public string? TransactionOnBs { get; set; }
        public decimal? TransactionAmount { get; set; }
        public bool? IsReceived { get; set; }
        public string? EntryBy { get; set; }
    }

    public class RemitReceiptPrintData
    {
        public List<RemitReceiptPrintRowDto> Rows { get; set; } = [];
        public int TotalRecords { get; set; }
        public decimal TransactionAmount { get; set; }
        public string? AmountInWords { get; set; }
        public string? FromDateBs { get; set; }
        public string? ToDateBs { get; set; }
        public string? BranchName { get; set; }
        public string? IsReceived { get; set; }
    }
}
