// Repository/MemberAccount/CollectorDetailReport/CollectorWiseCommissionSummaryRepository.cs
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
    public class CollectorWiseCommissionSummaryRepository : ICollectorWiseCommissionSummaryRepository
    {
        private readonly AppDbContext _context;
        private readonly IDateConverterService _dateConverter;
        private readonly ILogger<CollectorWiseCommissionSummaryRepository> _logger;

        public CollectorWiseCommissionSummaryRepository(
            AppDbContext context,
            IDateConverterService dateConverter,
            ILogger<CollectorWiseCommissionSummaryRepository> logger)
        {
            _context = context;
            _dateConverter = dateConverter;
            _logger = logger;
        }

        public async Task<CollectorWiseCommissionSummaryData> GetReportDataAsync(CollectorWiseCommissionSummaryRequestDto request)
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

                // Branch filter
                if (!string.IsNullOrEmpty(request.BranchIds) && request.BranchIds != "-1")
                {
                    sqlFilterExp.Append($" AND t.UsmOfficeId IN ({request.BranchIds})");
                }

                // Build Order By clause
                var orderByClause = BuildOrderByClause(request.OrderBy);

                // Combine filter and order by
                var fullFilter = sqlFilterExp.ToString() + orderByClause;

                var parameters = new DynamicParameters();
                parameters.Add("@SqlFilterExp", sqlFilterExp.ToString(), DbType.String, size: -1);
                parameters.Add("@SqlFilterExpOrderBy", orderByClause, DbType.String, size: -1);

                var connectionString = _context.Database.GetConnectionString();
                using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();

                var rows = await connection.QueryAsync<CollectorWiseCommissionSummaryRowDto>(
                    "sp_5_43_GetCollectorWiseCommissionSummary",
                    parameters,
                    commandType: CommandType.StoredProcedure
                );

                var resultList = rows.AsList();

                // Get unique collectors count
                var totalCollectors = resultList.Select(r => r.Collector).Distinct().Count();

                return new CollectorWiseCommissionSummaryData
                {
                    Rows = resultList,
                    TotalRecords = resultList.Count,
                    TotalCollectedAmount = resultList.Sum(r => r.CollectedAmount ?? 0),
                    TotalCommissionAmount = resultList.Sum(r => r.CommissionAmount ?? 0),
                    FromDateBs = request.FromDateBs,
                    ToDateBs = request.ToDateBs,
                    BranchNames = request.BranchName,
                    OrderBy = request.OrderBy,
                    TotalCollectors = totalCollectors
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetReportDataAsync for Collector Wise Commission Summary Report");
                throw;
            }
        }

        private static string BuildOrderByClause(string orderBy)
        {
            return orderBy switch
            {
                "Type" => " ORDER BY Type",
                "Collected Amount" => " ORDER BY CollectedAmount DESC",
                "Commission Amount" => " ORDER BY CommissionAmount DESC",
                "Collector" => " ORDER BY Collector",
                _ => " ORDER BY Type"
            };
        }
    }
}