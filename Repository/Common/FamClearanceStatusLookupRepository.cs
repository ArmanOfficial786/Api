using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using NexgenCosysReport.DbContext;
using NexgenCosysReport.Dtos.RequestDtos.Common;
using NexgenCosysReport.Inteface.ServiceInterface.Common;

namespace NexgenCosysReport.Repository.Common
{
    public class FamClearanceStatusLookupRepository : IFamClearanceStatusLookup
    {
        private readonly AppDbContext _context;
        private readonly ILogger<FamClearanceStatusLookupRepository> _logger;

        public FamClearanceStatusLookupRepository(
            AppDbContext context,
            ILogger<FamClearanceStatusLookupRepository> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<List<FamClearanceStatusLookupDto>> GetAllAsync()
        {
            try
            {
                var connectionString = _context.Database.GetConnectionString();
                using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();

                var rows = await connection.QueryAsync<FamClearanceStatusLookupDto>(
                    "SELECT FamFixedAssetsClearanceStatusId, Description FROM FamFixedAssetsClearanceStatus ORDER BY Description"
                );

                return rows.AsList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetAllAsync");
                throw;
            }
        }
    }
}