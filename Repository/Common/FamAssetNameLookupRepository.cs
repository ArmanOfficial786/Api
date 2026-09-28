using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using NexgenCosysReport.DbContext;
using NexgenCosysReport.Dtos.RequestDtos.Common;
using NexgenCosysReport.Inteface.ServiceInterface.Common;

namespace NexgenCosysReport.Repository.Common
{
    public class FamAssetLookupRepository : IFamAssetNameLookup
    {
        private readonly AppDbContext _context;
        private readonly ILogger<FamAssetLookupRepository> _logger;

        public FamAssetLookupRepository(
            AppDbContext context,
            ILogger<FamAssetLookupRepository> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<FamAssetNameLookupDto?> GetByIdAsync(long famFixedAssetsDetailId)
        {
            try
            {
                var connectionString = _context.Database.GetConnectionString();

                await using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();

                const string sql = @"
            SELECT
                FamFixedAssetsDetailId,
                FixedAssetsName,
                IsActive
            FROM FamFixedAssetsDetail
            WHERE FamFixedAssetsDetailId = @FamFixedAssetsDetailId";

                return await connection.QuerySingleOrDefaultAsync<FamAssetNameLookupDto>(
                    sql,
                    new { FamFixedAssetsDetailId = famFixedAssetsDetailId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error fetching asset detail for ID: {FamFixedAssetsDetailId}",
                    famFixedAssetsDetailId);
                throw;
            }
        }
    }
}
