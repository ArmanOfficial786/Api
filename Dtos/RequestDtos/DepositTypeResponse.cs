namespace JsSampleReport.Dtos.RequestDtos
{
    public class DepositTypeResponse
    {
        public long DepositeTypeId { get; set; }
        public string? DepositeTypeName { get; set; }
        public bool IsActive { get; set; }
    }
}
