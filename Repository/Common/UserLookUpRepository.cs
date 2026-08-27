// Repository/Common/UserLookupRepository.cs
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using NexgenCosysReport.DbContext;
using NexgenCosysReport.Dtos.RequestDtos.Common;
using NexgenCosysReport.Inteface.ServiceInterface.Common;

namespace NexgenCosysReport.Repository.Common
{
    public class UserLookupRepository : IUserLookUp
    {
        private readonly AppDbContext _context;
        private readonly ILogger<UserLookupRepository> _logger;

        public UserLookupRepository(AppDbContext context, ILogger<UserLookupRepository> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<List<UserLookupResponse>> GetActiveUsersAsync(long loggedInUserId)
        {
            try
            {
                var connectionString = _context.Database.GetConnectionString();
                using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();

                // ── Mirrors CUsmUser.GetAllActiveByRoleId exactly:
                // join UsmRelationUserToOfficeLogin -> UsmUser on UsmOfficeId,
                // filtered to the offices the CURRENT logged-in user can log
                // into, active users only, ordered by FullName. ──────────────
                const string sql = @"
                    SELECT DISTINCT
                        u.UsmUserId AS Id,
                        u.FullName AS FullName
                    FROM UsmRelationUserToOfficeLogin r
                    INNER JOIN UsmUser u ON r.UsmOfficeId = u.UsmOfficeId
                    WHERE r.UsmUserId = @UserId
                        AND u.IsActive = 1
                    ORDER BY u.FullName";

                var result = await connection.QueryAsync<UserLookupResponse>(sql, new
                {
                    UserId = loggedInUserId,
                });

                return result.AsList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetActiveUsersAsync");
                throw;
            }
        }
    }
}