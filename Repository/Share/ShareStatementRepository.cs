using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using NexgenCosysReport.DbContext;
using NexgenCosysReport.Dtos.RequestDtos.Share;
using NexgenCosysReport.Inteface.ServiceInterface.Share;
using System.Data;
using System.Text;

namespace NexgenCosysReport.Repository.Share
{
    public class ShareStatementRepository : IShareStatement
    {
        private readonly AppDbContext _context;
        private readonly ILogger<ShareStatementRepository> _logger;

        public ShareStatementRepository(
            AppDbContext context,
            ILogger<ShareStatementRepository> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<ShareStatementData> GetReportDataAsync(ShareStatementRequestDto request)
        {
            try
            {
                var connectionString = _context.Database.GetConnectionString();
                using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();

                var sqlFilterExp = new StringBuilder();
                string? shareTypeName = null;

                if (request.MemberId != -1)
                {
                    sqlFilterExp.Append(" And m.MemMemberRegistrationId = ").Append(request.MemberId);
                }

                if (request.ShareTypeId != -1)
                {
                    sqlFilterExp.Append(" And st.ShmShareTypeId = ").Append(request.ShareTypeId);
                    shareTypeName = await connection.QueryFirstOrDefaultAsync<string>(
                        "SELECT ShareTypeName FROM ShmShareType WHERE ShmShareTypeId = @Id",
                        new { Id = request.ShareTypeId });
                }

                var parameters = new DynamicParameters();
                parameters.Add("@SqlFilterExp", sqlFilterExp.ToString(), DbType.String, size: -1);

                var rows = await connection.QueryAsync<ShareStatementRowDto>(
                    "sp_8_14_GetShareStatement",
                    parameters,
                    commandType: CommandType.StoredProcedure,
                    commandTimeout: 120
                );

                var resultList = rows.AsList();
                var first = resultList.FirstOrDefault();

                return new ShareStatementData
                {
                    Rows = resultList,
                    TotalRecords = resultList.Count,
                    TotalPurchasedAmount = resultList.Sum(r => r.PurchasedAmount ?? 0),
                    TotalReturnedAmount = resultList.Sum(r => r.ReturnedAmount ?? 0),
                    ClosingBalance = resultList.LastOrDefault()?.Balance ?? 0,
                    MemberId = first?.MemberId,
                    MemberName = first?.MemberName,
                    Address = first?.Address,
                    PhoneNo = first?.PhoneNo,
                    ShareTypeName = shareTypeName ?? "All"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetReportDataAsync");
                throw;
            }
        }
    }
}