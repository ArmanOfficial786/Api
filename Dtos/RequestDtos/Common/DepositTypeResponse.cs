namespace NexgenCosysReport.Dtos.RequestDtos.Common
{
    public class DepositTypeResponse
    {
        public long DepositeTypeId { get; set; }
        public string? DepositeTypeName { get; set; }
        public bool IsActive { get; set; }
    }
}
