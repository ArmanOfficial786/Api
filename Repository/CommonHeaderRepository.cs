using Dapper;
using NexgenCosysReport.Dtos.ReportDtos;
using NexgenCosysReport.Inteface.ServiceInterface;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace NexgenCosysReport.Repository
{
    public class CommonHeaderRepository : ICommonHeaderRepository
    {
        private readonly AppDbContext _context;

        public CommonHeaderRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<CommonHeader>> GetCommonHeaders(string branchId = "")
        {
            var connectionString = _context.Database.GetConnectionString();
            using (var connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();

                string spName;
                var parameters = new DynamicParameters();

                if (!string.IsNullOrWhiteSpace(branchId) && branchId != "-1")
                {
                    // ? Branch-specific ? use sp_2_1_GetOfficeProfile with UsmOfficeId filter
                    spName = "sp_2_1_GetOfficeProfile";
                    parameters.Add("@SqlFilterExp", $" And s.UsmOfficeId={branchId}", DbType.String, size: -1);
                }
                else
                {
                    // ? No branch ? use sp_2_1_GetCompanyProfile with empty filter
                    spName = "sp_2_1_GetCompanyProfile";
                    parameters.Add("@SqlFilterExp", "", DbType.String);
                }

                var results = await connection.QueryAsync<CommonHeader>(
                    spName,
                    parameters,
                    commandType: CommandType.StoredProcedure
                );

                return results.ToList();
            }
        }
    }
}
