namespace NexgenCosysReport.Dtos.RequestDtos.Remit
{
    public class RemitOnlinePaymentDetailRequestDto
    {
        public string? FromDateBs { get; set; }
        public string? ToDateBs { get; set; }
        public string? BranchIds { get; set; }
        public string IsPaid { get; set; } = "A";
        public long RemittanceGroupId { get; set; } = -1;
        public string OrderBy { get; set; } = "-1";
        public bool VisualReport { get; set; } = false;
    }

    public class RemitOnlinePaymentDetailRowDto
    {
        public long? SNo { get; set; }
        public string? Receiver { get; set; }
        public string? ReceiverNo { get; set; }
        public string? Sender { get; set; }
        public string? SenderNo { get; set; }
        public string? PinCode { get; set; }
        public string? RefId { get; set; }
        public string? Date { get; set; }
        public decimal? Amount { get; set; }
        public decimal? Comission { get; set; }
        public string? RemitGroup { get; set; }
        public string? PaymentMode { get; set; }
        public string? Currency { get; set; }
        public string? Bank { get; set; }
        public string? Branch { get; set; }
        public string? Account { get; set; }
        public string? UserName { get; set; }
        public bool? IsPaid { get; set; }
    }

    public class RemitOnlinePaymentDetailData
    {
        public List<RemitOnlinePaymentDetailRowDto> Rows { get; set; } = [];
        public int TotalRecords { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal TotalComission { get; set; }
        public string? FromDateBs { get; set; }
        public string? ToDateBs { get; set; }
        public string? BranchName { get; set; }
        public string? RemittanceGroupName { get; set; }
        public string? IsPaid { get; set; }
        public string? OrderBy { get; set; }
    }
}
