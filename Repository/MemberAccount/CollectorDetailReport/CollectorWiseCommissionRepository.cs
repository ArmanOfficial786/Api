// Repository/MemberAccount/CollectorDetailReport/CollectorWiseCommissionRepository.cs
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using NexgenCosysReport.DbContext;
using NexgenCosysReport.Dtos.RequestDtos.MemberAccount.CollectorDetailReport;
using NexgenCosysReport.Inteface.ServiceInterface.Common;
using NexgenCosysReport.Inteface.ServiceInterface.MemberAccount.CollectorDetRepInterface;
using System.Data;
using System.Text;

namespace NexgenCosysReport.Repository.MemberAccount.CollectorDetailReport
{
    public class CollectorWiseCommissionRepository : ICollectorWiseCommissionRepository
    {
        private readonly AppDbContext _context;
        private readonly IDateConverterService _dateConverter;
        private readonly ILogger<CollectorWiseCommissionRepository> _logger;

        public CollectorWiseCommissionRepository(
            AppDbContext context,
            IDateConverterService dateConverter,
            ILogger<CollectorWiseCommissionRepository> logger)
        {
            _context = context;
            _dateConverter = dateConverter;
            _logger = logger;
        }

        public async Task<CollectorWiseCommissionData> GetReportDataAsync(CollectorWiseCommissionRequestDto request)
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
                sqlFilterExp.Append($" AND t.TransactionOn BETWEEN '{fromDateStr}' AND '{toDateStr}'");

                // Collector filter
                if (request.CollectorId != -1)
                {
                    sqlFilterExp.Append($" AND t.HurCollectorId = {request.CollectorId}");
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

                var rows = await connection.QueryAsync<CollectorWiseCommissionRowDto>(
                    "sp_5_43_GetCollectorWiseCommission",
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

                return new CollectorWiseCommissionData
                {
                    Rows = resultList,
                    TotalRecords = resultList.Count,
                    TotalCollectedAmount = resultList.Sum(r => r.CollectedAmount ?? 0),
                    TotalCommissionAmount = resultList.Sum(r => r.CommissionAmount ?? 0),
                    FromDateBs = request.FromDateBs,
                    ToDateBs = request.ToDateBs,
                    OrderBy = request.OrderBy,
                    CollectorName = collectorName ?? request.CollectorName,
                    CollectorId = request.CollectorId
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetReportDataAsync for Collector Wise Commission Report");
                throw;
            }
        }

        private static string BuildOrderByClause(string orderBy)
        {
            return orderBy switch
            {
                "Member Id" => " ORDER BY substring(MemberId, 1,(len(MemberId)-charindex('-', MemberId))-1), MemberId",
                "Member Name" => " ORDER BY MemberName",
                "Type" => " ORDER BY Type",
                "Account No" => " ORDER BY substring(AccountNo, 1,(len(AccountNo)-charindex('-', AccountNo))-1), AccountNo",
                "Collected Amount" => " ORDER BY CollectedAmount DESC",
                "Commission Amount" => " ORDER BY CommissionAmount DESC",
                "Date" => " ORDER BY Date",
                _ => " ORDER BY MemberId"
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