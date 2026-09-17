using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using NexgenCosysReport.DbContext;
using NexgenCosysReport.Dtos.RequestDtos.Common;
using NexgenCosysReport.Inteface.ServiceInterface.Common;

namespace NexgenCosysReport.Repository.Common
{
    public class PaymentDurationTypeRepository : IPaymentDurationType
    {
        private readonly AppDbContext _context;
        private readonly ILogger<PaymentDurationTypeRepository> _logger;

        public PaymentDurationTypeRepository(AppDbContext context, ILogger<PaymentDurationTypeRepository> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<List<PaymentDurationTypeResponse>> GetAllAsync()
        {
            try
            {
                var connectionString = _context.Database.GetConnectionString();
                using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();

                var sql = @"
                     SELECT 
                        LmtPaymentDurationTypeId,
                        PaymentDurationType
                      
                    FROM LmtPaymentDurationType
                    ORDER BY LmtPaymentDurationTypeId";

                var result = await connection.QueryAsync<PaymentDurationTypeResponse>(sql);

                return result.AsList();
            }
            catch (Exception ex)
            {

                throw new Exception("Error occurred while fetching payment duration types.", ex);
            }
        }
    }
}