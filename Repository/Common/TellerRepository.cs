// Repository/Common/TellerRepository.cs
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using NexgenCosysReport.DbContext;
using NexgenCosysReport.Dtos.RequestDtos.Common;
using NexgenCosysReport.Inteface.ServiceInterface.Common;

namespace NexgenCosysAPI.Repository.Common
{
    public class TellerRepository : ITeller
    {
        private readonly AppDbContext _context;
        private readonly IDateConverterService _dateConverter;
        private readonly ILogger<TellerRepository> _logger;

        public TellerRepository(AppDbContext context, IDateConverterService dateConverter, ILogger<TellerRepository> logger)
        {
            _context = context;
            _dateConverter = dateConverter;
            _logger = logger;
        }

        public async Task<List<TellerLookupResponse>> GetTellersAsync(DateTime fromDate, DateTime toDate, long userId)
        {
            try
            {
                var connectionString = _context.Database.GetConnectionString();
                using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();

                // SQL query mimicking the BLL method GetBetweenDateCollection
                // Includes only transaction types that are considered "collection"
                // The source list from BLL: { 8, 11, 18, 26, 1, 14, 17, 25, 27, 30, 5, 6, 7, 20, 21, 22, 12, 13, 19, 24, 33, 34, 35, 37, 38, 39, 40, 41, 43, 47, 50, 44, 45, 46, 49, 48, 53, 54, 56, 58, 60, 62, 50, 64, 73, 69, 67, 70 }
                // We'll use that list in the query.

                var sql = @"
                    SELECT DISTINCT 
                        u.UsmUserId AS Id,
                        u.FullName AS Name
                    FROM AcoTransaction t
                    INNER JOIN UsmUser u ON t.CreatedBy = u.UsmUserId
                    INNER JOIN UsmRelationUserToOffice r ON u.UsmOfficeId = r.UsmOfficeId
                    WHERE r.UsmUserId = @UserId
                        AND t.TransactionOn >= @FromDate
                        AND t.TransactionOn <= @ToDate
                        AND t.IsActive = 1
                        AND t.AcoTransactionTypeId IN (
                            8,11,18,26,1,14,17,25,27,30,5,6,7,20,21,22,12,13,19,24,33,34,35,37,38,39,40,41,43,47,50,44,45,46,49,48,53,54,56,58,60,62,50,64,73,69,67,70
                        )
                    ORDER BY u.FullName";

                var result = await connection.QueryAsync<TellerLookupResponse>(sql, new
                {
                    UserId = userId,
                    FromDate = fromDate,
                    ToDate = toDate
                });

                return result.AsList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetTellersAsync");
                throw;
            }
        }
    }
}