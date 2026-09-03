// Repository/MemberAccount/InterestExpenseReport/InterestAndTaxTypeWiseRepository.cs
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using NexgenCosysReport.DbContext;
using NexgenCosysReport.Dtos.RequestDtos.MemberAccount.InterestExpenseReport;
using NexgenCosysReport.Inteface.ServiceInterface.Common;
using NexgenCosysReport.Inteface.ServiceInterface.MemberAccount.InterestExpenseReportInterface;
using System.Data;
using System.Text;

namespace NexgenCosysReport.Repository.MemberAccount.InterestExpenseReport
{
    public class InterestAndTaxTypeWiseRepository : IInterestAndTaxTypeWiseRepository
    {
        private readonly AppDbContext _context;
        private readonly IDateConverterService _dateConverter;
        private readonly ILogger<InterestAndTaxTypeWiseRepository> _logger;

        public InterestAndTaxTypeWiseRepository(
            AppDbContext context,
            IDateConverterService dateConverter,
            ILogger<InterestAndTaxTypeWiseRepository> logger)
        {
            _context = context;
            _dateConverter = dateConverter;
            _logger = logger;
        }

        public async Task<InterestAndTaxTypeWiseData> GetReportDataAsync(InterestAndTaxTypeWiseRequestDto request)
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
                sqlFilterExp.Append($" AND At.TransactionOn BETWEEN '{fromDateStr}' AND '{toDateStr}'");

                // Branch filter
                if (!string.IsNullOrEmpty(request.BranchIds) && request.BranchIds != "-1")
                {
                    sqlFilterExp.Append($" AND At.UsmOfficeId IN ({request.BranchIds})");
                }

                // Build Order By clause
                var orderByClause = BuildOrderByClause(request.OrderBy);

                // Determine which stored procedure to use based on report view
                string spName = request.ReportView switch
                {
                    "2" => "sp_5_43_GetInterestAndTaxPercentTypeWise",
                    "3" => "sp_5_43_GetInterestAndTaxPercentTypeWiseAll",
                    _ => "sp_5_43_GetInterestAndTaxTypeWise"
                };

                var parameters = new DynamicParameters();
                parameters.Add("@SqlFilterExp", sqlFilterExp.ToString(), DbType.String, size: -1);
                parameters.Add("@SqlFilterExpOrder", orderByClause, DbType.String, size: -1);

                var connectionString = _context.Database.GetConnectionString();
                using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();

                var rows = await connection.QueryAsync<InterestAndTaxTypeWiseRowDto>(
                    spName,
                    parameters,
                    commandType: CommandType.StoredProcedure
                );

                var resultList = rows.AsList();

                // Get unique deposit types count
                var totalDepositTypes = resultList.Select(r => r.DepositTypeName).Distinct().Count();

                return new InterestAndTaxTypeWiseData
                {
                    Rows = resultList,
                    TotalRecords = resultList.Count,
                    TotalInterest = resultList.Sum(r => r.Interest ?? 0),
                    TotalTax = resultList.Sum(r => r.Tax ?? 0),
                    TotalNetAmount = resultList.Sum(r => r.NetAmount ?? 0),
                    FromDateBs = request.FromDateBs,
                    ToDateBs = request.ToDateBs,
                    BranchNames = request.BranchName,
                    OrderBy = request.OrderBy,
                    ReportView = request.ReportView,
                    TotalDepositTypes = totalDepositTypes
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetReportDataAsync for Interest and Tax Type Wise Report");
                throw;
            }
        }

        private static string BuildOrderByClause(string orderBy)
        {
            return orderBy switch
            {
                "Date" => " ORDER BY Date",
                "Interest" => " ORDER BY Interest DESC",
                "Tax" => " ORDER BY Tax DESC",
                "PercentTax" => " ORDER BY PercentTax DESC",
                _ => " ORDER BY Date"
            };
        }
    }
}