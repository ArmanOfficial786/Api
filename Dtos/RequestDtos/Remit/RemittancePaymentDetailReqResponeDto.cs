namespace NexgenCosysReport.Dtos.RequestDtos.Remit
{
    public class RemittancePaymentDetailRequestDto
    {
        public string? FromDateBs { get; set; }
        public string? ToDateBs { get; set; }
        public string? BranchIds { get; set; }
        public long RemittanceTypeId { get; set; } = -1;
        public string RemittanceServiceType { get; set; } = "-1";
        public string OrderBy { get; set; } = "-1";
        public bool VisualReport { get; set; } = false;
    }

    public class RemittancePaymentDetailRowDto
    {
        public long? RemittanceDetailId { get; set; }
        public string? Receiver { get; set; }
        public string? Sender { get; set; }
        public string? IMECodeNo { get; set; }
        public decimal? Amount { get; set; }
        public string? Date { get; set; }
        public DateTime? DateOn { get; set; }
        public string? CashTeller { get; set; }
        public string? RemittanceType { get; set; }
        public string? PaymentBy { get; set; }
    }

    public class RemittancePaymentDetailData
    {
        public List<RemittancePaymentDetailRowDto> Rows { get; set; } = [];
        public int TotalRecords { get; set; }
        public decimal TotalAmount { get; set; }
        public string? FromDateBs { get; set; }
        public string? ToDateBs { get; set; }
        public string? BranchName { get; set; }
        public string? RemittanceTypeName { get; set; }
        public string? RemittanceServiceType { get; set; }
        public string? OrderBy { get; set; }
    }
}
