namespace NexgenCosysReport.Dtos.RequestDtos.Remit
{
    public class RemittanceReceivedRequestDto
    {
        public string? FromDateBs { get; set; }
        public string? ToDateBs { get; set; }
        public string? BranchIds { get; set; }
        public long RemittanceTypeId { get; set; } = -1;
        public string OrderBy { get; set; } = "-1";
        public bool VisualReport { get; set; } = false;
    }

    public class RemittanceReceivedRowDto
    {
        public string? Receiver { get; set; }
        public string? Sender { get; set; }
        public string? IMECodeNo { get; set; }
        public decimal? Amount { get; set; }
        public decimal? Comission { get; set; }
        public string? Date { get; set; }
        public string? CashTeller { get; set; }
        public decimal? Balance { get; set; }
        public string? RemittanceType { get; set; }
    }

    public class RemittanceReceivedData
    {
        public List<RemittanceReceivedRowDto> Rows { get; set; } = [];
        public int TotalRecords { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal TotalComission { get; set; }
        public decimal TotalBalance { get; set; }
        public string? FromDateBs { get; set; }
        public string? ToDateBs { get; set; }
        public string? BranchName { get; set; }
        public string? RemittanceTypeName { get; set; }
        public string? OrderBy { get; set; }
    }
}
