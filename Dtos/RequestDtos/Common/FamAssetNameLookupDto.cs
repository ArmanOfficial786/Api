namespace NexgenCosysReport.Dtos.RequestDtos.Common
{
    public class FamAssetNameLookupDto
    {
        public long FamFixedAssetsDetailId { get; set; }
        public string? FixedAssetsName { get; set; }
        public bool? IsActive { get; set; }
    }
}
