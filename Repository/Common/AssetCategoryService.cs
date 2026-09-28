using Microsoft.EntityFrameworkCore;
using NexgenCosysReport.DbContext;
using NexgenCosysReport.Dtos.RequestDtos.Common;
using NexgenCosysReport.Inteface.ServiceInterface.Common;

namespace NexgenCosysReport.Repository.Common
{
    public class AssetCategoryService : IAssetCategory
    {
        private readonly AppDbContext _context;

        public AssetCategoryService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<AssetCategoryResponse>> GetAllByType(char type)
        {
            return await (
                from category in _context.FamAssetsCategories
                join assetType in _context.FamAssetsTypes
                    on category.FamAssetsTypeId equals assetType.FamAssetsTypeId
                where category.IsActive == true
                      && category.Type == type.ToString()
                select new AssetCategoryResponse
                {
                    FamAssetsCategoryId = category.FamAssetsCategoryId,
                    TypeName = assetType.TypeName,
                    CategoryName = category.CategoryName,
                    IsActive = category.IsActive
                }
            ).ToListAsync();
        }
    }
}