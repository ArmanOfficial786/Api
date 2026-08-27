//// Repository/Common/TellerExpenseRepository.cs
//using Dapper;
//using Microsoft.Data.SqlClient;
//using Microsoft.EntityFrameworkCore;
//using NexgenCosysReport.DbContext;
//using NexgenCosysReport.Dtos.RequestDtos.Common;
//using NexgenCosysReport.Inteface.ServiceInterface.Common;

//namespace NexgenCosysReport.Repository.Common
//{
//    public class TellerExpensedropdownRepository : ITellerExpenseDropdown
//    {
//        private readonly AppDbContext _context;
//        private readonly IDateConverterService _dateConverter;
//        private readonly ILogger<TellerExpensedropdownRepository> _logger;

//        public TellerExpensedropdownRepository(AppDbContext context, IDateConverterService dateConverter, ILogger<TellerExpensedropdownRepository> logger)
//        {
//            _context = context;
//            _dateConverter = dateConverter;
//            _logger = logger;
//        }

//        public async Task<List<TellerLookupResponse>> GetTellersAsync(DateTime fromDate, DateTime toDate, long userId)
//        {
//            try
//            {
//                var connectionString = _context.Database.GetConnectionString();
//                using var connection = new SqlConnection(connectionString);
//                await connection.OpenAsync();

//                // Expense transaction type IDs from BLL method GetBetweenDateExpense
//                // Source list: 2, 23, 15, 16, 28, 29, 31, 9, 10, 24, 36, 51, 52, 55, 57, 59, 61, 67, 70, 65, 68, 71
//                var expenseTypeIds = new[]
//                {
//                    2, 23, 15, 16, 28, 29, 31, 9, 10, 24, 36, 51, 52, 55, 57, 59, 61, 67, 70, 65, 68, 71
//                };

//                var sql = @"
//                    SELECT DISTINCT 
//                        u.UsmUserId AS Id,
//                        u.FullName AS Name
//                    FROM AcoTransaction t
//                    INNER JOIN UsmUser u ON t.CreatedBy = u.UsmUserId
//                    INNER JOIN UsmRelationUserToOffice r ON u.UsmOfficeId = r.UsmOfficeId
//                    WHERE r.UsmUserId = @UserId
//                        AND t.TransactionOn >= @FromDate
//                        AND t.TransactionOn <= @ToDate
//                        AND t.IsActive = 1
//                        AND t.AcoTransactionTypeId IN @ExpenseTypeIds
//                    ORDER BY u.FullName";

//                var result = await connection.QueryAsync<TellerLookupResponse>(sql, new
//                {
//                    UserId = userId,
//                    FromDate = fromDate,

//                });

//                return result.AsList();
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, "Error in GetTellersAsync (expense)");
//                throw;
//            }
//        }
//    }
//}


// Repository/Common/TellerExpenseRepository.cs
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using NexgenCosysReport.DbContext;
using NexgenCosysReport.Dtos.RequestDtos.Common;
using NexgenCosysReport.Inteface.ServiceInterface.Common;

namespace NexgenCosysReport.Repository.Common
{
    public class TellerExpensedropdownRepository : ITellerExpenseDropdown
    {
        private readonly AppDbContext _context;
        private readonly IDateConverterService _dateConverter;
        private readonly ILogger<TellerExpensedropdownRepository> _logger;

        public TellerExpensedropdownRepository(AppDbContext context, IDateConverterService dateConverter, ILogger<TellerExpensedropdownRepository> logger)
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

                // Expense transaction type IDs from BLL method GetBetweenDateExpense
                // Source list: 2, 23, 15, 16, 28, 29, 31, 9, 10, 24, 36, 51, 52, 55, 57, 59, 61, 67, 70, 65, 68, 71
                var expenseTypeIds = new[]
                {
                    2, 23, 15, 16, 28, 29, 31, 9, 10, 24, 36, 51, 52, 55, 57, 59, 61, 67, 70, 65, 68, 71
                };

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
                        AND t.AcoTransactionTypeId IN @ExpenseTypeIds
                    ORDER BY u.FullName";

                var result = await connection.QueryAsync<TellerLookupResponse>(sql, new
                {
                    UserId = userId,
                    FromDate = fromDate,
                    ToDate = toDate,
                    ExpenseTypeIds = expenseTypeIds,
                });

                return result.AsList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetTellersAsync (expense)");
                throw;
            }
        }
    }
}