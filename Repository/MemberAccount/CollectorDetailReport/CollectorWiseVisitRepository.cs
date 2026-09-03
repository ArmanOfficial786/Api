// Repository/MemberAccount/CollectorDetailReport/CollectorWiseVisitRepository.cs
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
    public class CollectorWiseVisitRepository : ICollectorWiseVisitRepository
    {
        private readonly AppDbContext _context;
        private readonly IDateConverterService _dateConverter;
        private readonly ILogger<CollectorWiseVisitRepository> _logger;

        public CollectorWiseVisitRepository(
            AppDbContext context,
            IDateConverterService dateConverter,
            ILogger<CollectorWiseVisitRepository> logger)
        {
            _context = context;
            _dateConverter = dateConverter;
            _logger = logger;
        }

        public async Task<CollectorWiseVisitData> GetReportDataAsync(CollectorWiseVisitRequestDto request)
        {
            try
            {
                // Validate required fields
                if (string.IsNullOrEmpty(request.Month))
                    throw new ArgumentException("Month is required");

                if (string.IsNullOrEmpty(request.Year))
                    throw new ArgumentException("Year is required");

                // Build filter expression
                var sqlFilterExp = new StringBuilder();

                // Visit Type filter
                string visitTypeFilter = request.VisitType;
                if (visitTypeFilter == "All")
                {
                    sqlFilterExp.Append(" AND t.AcoTransactionTypeId IN (1,5,6,7)");
                }
                else if (visitTypeFilter == "Deposit")
                {
                    sqlFilterExp.Append(" AND t.AcoTransactionTypeId IN (1)");
                }
                else if (visitTypeFilter == "Loan")
                {
                    sqlFilterExp.Append(" AND t.AcoTransactionTypeId IN (5,6,7)");
                }

                // Collector filter
                if (request.CollectorId != -1)
                {
                    sqlFilterExp.Append($" AND t.HurCollectorId = {request.CollectorId}");
                }

                // Build Order By clause
                var orderByClause = BuildOrderByClause(request.OrderBy);

                // Determine which stored procedure to use
                string spName = request.GenerateBy == "A"
                    ? "sp_5_43_GetCollectorWiseVisitAll"
                    : "sp_5_43_GetCollectorWiseVisit";

                var parameters = new DynamicParameters();
                parameters.Add("@SqlFilterExp", sqlFilterExp.ToString(), DbType.String, size: -1);
                parameters.Add("@SqlFilterExpMonth", request.Month, DbType.String);
                parameters.Add("@SqlFilterExpYear", request.Year, DbType.String);
                parameters.Add("@SqlFilterExpOrder", orderByClause, DbType.String, size: -1);
                parameters.Add("@SqlFilterAmountCount", request.AmountType, DbType.String);

                // Additional parameters for "All Account" mode
                if (request.GenerateBy == "A")
                {
                    parameters.Add("@SqlFilterTillDate", await GetMonthEndDate(request.Month, request.Year), DbType.String);
                    parameters.Add("@SqlFilterCollectorId", request.CollectorId, DbType.Int64);
                    parameters.Add("@SqlFilterType", request.VisitType, DbType.String);
                }

                var connectionString = _context.Database.GetConnectionString();
                using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();

                var rows = await connection.QueryAsync<CollectorWiseVisitRowDto>(
                    spName,
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

                // Get month name
                string monthName = GetMonthName(request.Month);

                return new CollectorWiseVisitData
                {
                    Rows = resultList,
                    TotalRecords = resultList.Count,
                    TotalAmount = resultList.Sum(r => r.TotalAmount ?? 0),
                    TotalVisits = resultList.Sum(r => r.VisitCount ?? 0),
                    Month = request.Month,
                    Year = request.Year,
                    CollectorName = collectorName ?? request.CollectorName,
                    CollectorId = request.CollectorId,
                    VisitType = request.VisitType,
                    OrderBy = request.OrderBy,
                    ReportType = request.ReportType,
                    AmountType = request.AmountType,
                    GenerateBy = request.GenerateBy,
                    MonthName = monthName
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetReportDataAsync for Collector Wise Visit Report");
                throw;
            }
        }

        private static string BuildOrderByClause(string orderBy)
        {
            return orderBy switch
            {
                "Member Id" => " ORDER BY substring(MemberId, 1,(len(MemberId)-charindex('-', MemberId))-1), MemberId",
                "Member Name" => " ORDER BY MemberName",
                "Account No" => " ORDER BY substring(AccountNo, 1,(len(AccountNo)-charindex('-', AccountNo))-1), AccountNo",
                "Total" => " ORDER BY Total DESC",
                _ => " ORDER BY MemberId"
            };
        }

        private static string GetMonthName(string month)
        {
            return month switch
            {
                "01" => "Baishak",
                "02" => "Jestha",
                "03" => "Aashad",
                "04" => "Srawan",
                "05" => "Bhadra",
                "06" => "Aaswin",
                "07" => "Kartik",
                "08" => "Mangsir",
                "09" => "Poush",
                "10" => "Magh",
                "11" => "Falgun",
                "12" => "Chaitra",
                _ => month
            };
        }

        private async Task<string> GetMonthEndDate(string month, string year)
        {
            try
            {
                // Convert Nepali month/year to English date
                var nepaliDate = $"{year}-{month.PadLeft(2, '0')}-01";
                var englishDate = await _dateConverter.NepaliToEnglishAsync(nepaliDate);

                // Get the last day of the month
                var lastDay = DateTime.DaysInMonth(englishDate.Year, englishDate.Month);
                var endDate = new DateTime(englishDate.Year, englishDate.Month, lastDay);

                return endDate.ToString("yyyy-MM-dd");
            }
            catch
            {
                return DateTime.Now.ToString("yyyy-MM-dd");
            }
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