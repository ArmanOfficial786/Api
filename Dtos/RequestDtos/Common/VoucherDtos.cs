namespace NexgenCosysReport.Dtos.RequestDtos.Common
{
    public class VoucherListRequest
    {
        public string FromDate { get; set; } = string.Empty;
        public string ToDate { get; set; } = string.Empty;
        public List<long>? BranchIds { get; set; }
    }

    public class VoucherOptionResponse
    {
        public long AcoVoucherId { get; set; }
        public string VoucherNo { get; set; } = string.Empty;
    }
    public class VoucherDtos
    {
    }
}
