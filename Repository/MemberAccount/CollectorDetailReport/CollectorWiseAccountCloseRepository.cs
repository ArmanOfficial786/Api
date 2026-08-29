// Repository/MemberAccount/CollectorDetailReport/CollectorWiseAccountCloseRepository.cs
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using NexgenCosysReport.DbContext;
using NexgenCosysReport.Dtos.RequestDtos.MemberAccount.CollectorDetailReport;
using NexgenCosysReport.Inteface.ServiceInterface.Common;
using NexgenCosysReport.Inteface.ServiceInterface.MemberAccount.CollectorDetailReport;
using System.Data;
using System.Text;

namespace NexgenCosysReport.Repository.MemberAccount.CollectorDetailReport
{
    public class CollectorWiseAccountCloseRepository : ICollectorWiseAccountCloseRepository
    {
        private readonly AppDbContext _context;
        private readonly IDateConverterService _dateConverter;
        private readonly ILogger<CollectorWiseAccountCloseRepository> _logger;

        public CollectorWiseAccountCloseRepository(
            AppDbContext context,
            IDateConverterService dateConverter,
            ILogger<CollectorWiseAccountCloseRepository> logger)
        {
            _context = context;
            _dateConverter = dateConverter;
            _logger = logger;
        }

        public async Task<CollectorWiseAccountCloseData> GetReportDataAsync(CollectorWiseAccountCloseRequestDto request)
        {
            try
            {
                // Convert Nepali dates to English
                var fromDateAd = await _dateConverter.NepaliToEnglishAsync(request.FromDateBs);
                var toDateAd = await _dateConverter.NepaliToEnglishAsync(request.ToDateBs);
                var fromDateStr = fromDateAd.ToString("yyyy-MM-dd");
                var toDateStr = toDateAd.ToString("yyyy-MM-dd");

                // Build filter expression
                var sqlFilterExp = new StringBuilder();

                // Date filter
                sqlFilterExp.Append($" AND c.AccountCloseOn BETWEEN '{fromDateStr}' AND '{toDateStr}'");

                // Collector filter
                if (request.CollectorId != -1)
                {
                    sqlFilterExp.Append($" AND tc.HurCollectorId = {request.CollectorId}");
                }

                // Build Order By clause
                var orderByClause = BuildOrderByClause(request.OrderBy);

                // Combine filter and order by
                var fullFilter = sqlFilterExp.ToString() + orderByClause;

                var parameters = new DynamicParameters();
                parameters.Add("@SqlFilterExp", fullFilter, DbType.String, size: -1);

                var connectionString = _context.Database.GetConnectionString();
                using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();

                var rows = await connection.QueryAsync<CollectorWiseAccountCloseRowDto>(
                    "sp_5_43_GetCollectorWiseAccountClose",
                    parameters,
                    commandType: CommandType.StoredProcedure
                );

                var resultList = rows.AsList();

                // Get collector name
                string? collectorName = null;
                if (request.CollectorId != -1)
                {
                    collectorName = await GetCollectorNameAsync(request.CollectorId);
                }

                return new CollectorWiseAccountCloseData
                {
                    Rows = resultList,
                    TotalRecords = resultList.Count,
                    TotalCloseAmount = resultList.Sum(r => r.CloseAmount ?? 0),
                    FromDateBs = request.FromDateBs,
                    ToDateBs = request.ToDateBs,
                    OrderBy = request.OrderBy,
                    CollectorName = collectorName ?? request.CollectorName,
                    CollectorId = request.CollectorId,
                    TotalClosedAccounts = resultList.Count
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetReportDataAsync for Collector Wise Account Close Report");
                throw;
            }
        }

        private static string BuildOrderByClause(string orderBy)
        {
            return orderBy switch
            {
                "Member Id" => " ORDER BY substring(m.MemberId, 1,(len(m.MemberId)-charindex('-', m.MemberId))-1), m.MemberId",
                "Member Name" => " ORDER BY Name",
                "Account No" => " ORDER BY substring(a.AccountNo, 1,(len(a.AccountNo)-charindex('-', a.AccountNo))-1), a.AccountNo",
                "A‎/‎C Open Date" => " ORDER BY a.AccountOpenOnBs",
                "A‎/‎C Close Date" => " ORDER BY c.AccountCloseOnBs",
                "Close Amount" => " ORDER BY c.AccountCloseAmount DESC",
                _ => " ORDER BY m.MemberId"
            };
        }

        private async Task<string?> GetCollectorNameAsync(long collectorId)
        {
            try
            {
                const string query = @"
                    SELECT CollectorFullName 
                    FROM HurCollector 
                    WHERE HurCollectorId = @CollectorId AND IsActive = 1";

                var connectionString = _context.Database.GetConnectionString();
                using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();

                return await connection.QueryFirstOrDefaultAsync<string>(
                    query,
                    new { CollectorId = collectorId }
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting collector name for ID: {CollectorId}", collectorId);
                return null;
            }
        }
    }
}