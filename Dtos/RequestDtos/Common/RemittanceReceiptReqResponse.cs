namespace NexgenCosysReport.Dtos.RequestDtos.Common
{
    public class RemittanceReceiptRequest
    {
        public bool? IsReceived { get; set; }
        public long? CreatedBy { get; set; }
        public string? FromDate { get; set; }
        public string? ToDate { get; set; }
        public List<long>? OfficeIds { get; set; }
    }
    public class RemittanceReceiptResponse
    {
        public long RemittanceReceiptId { get; set; }
        public string? RemittanceReceiptName { get; set; }
    }
}
