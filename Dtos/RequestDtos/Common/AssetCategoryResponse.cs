namespace NexgenCosysReport.Dtos.RequestDtos.Common
{
    public class AssetCategoryResponse
    {
        public long FamAssetsCategoryId { get; set; }
        public string? TypeName { get; set; }
        public string? CategoryName { get; set; }
        public bool IsActive { get; set; }
    }
}
