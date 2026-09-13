// Repository/Loan/OtherReports/LoanCommissionRepository.cs
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using NexgenCosysReport.DbContext;
using NexgenCosysReport.Dtos.RequestDtos.Loan.OtherReports;
using NexgenCosysReport.Inteface.ServiceInterface.Common;
using NexgenCosysReport.Inteface.ServiceInterface.Loan.OtherReports;
using System.Data;
using System.Text;

namespace NexgenCosysReport.Repository.Loan.OtherReports
{
    public class LoanCommissionRepository : ILoanCommissionRepository
    {
        private readonly AppDbContext _context;
        private readonly IDateConverterService _dateConverter;
        private readonly ILogger<LoanCommissionRepository> _logger;

        public LoanCommissionRepository(
            AppDbContext context,
            IDateConverterService dateConverter,
            ILogger<LoanCommissionRepository> logger)
        {
            _context = context;
            _dateConverter = dateConverter;
            _logger = logger;
        }

        public async Task<LoanCommissionData> GetReportDataAsync(LoanCommissionRequestDto request)
        {
            try
            {
                var connectionString = _context.Database.GetConnectionString();
                using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();

                var sqlFilterExp = new StringBuilder();
                var sqlCollectorId = new StringBuilder();
                string? collectorName = null;

                // --------------------------------------------------------------
                // Build filter expression matching legacy BLL:
                // 1. Date range filter (@SqlFilterExp)
                // 2. Collector filter (@SqlcollectorId)
                // --------------------------------------------------------------
                if (!string.IsNullOrEmpty(request.FromDateBs) && !string.IsNullOrEmpty(request.ToDateBs)
                    && request.FromDateBs != "-1" && request.ToDateBs != "-1")
                {
                    var fromDateAd = await _dateConverter.NepaliToEnglishAsync(request.FromDateBs);
                    var toDateAd = await _dateConverter.NepaliToEnglishAsync(request.ToDateBs);

                    var fromDateStr = fromDateAd.ToString("yyyy-MM-dd");
                    var toDateStr = toDateAd.ToString("yyyy-MM-dd");

                    sqlFilterExp.Append(" And Ac.TransactionOn between '").Append(fromDateStr)
                                .Append("' And '").Append(toDateStr).Append("'");
                }

                if (request.CollectorId != -1)
                {
                    sqlCollectorId.Append(" And Ac.HurCollectorId = ").Append(request.CollectorId);

                    // Get collector name for display
                    collectorName = await connection.QueryFirstOrDefaultAsync<string>(
                        "SELECT CollectorFullName FROM HurCollector WHERE HurCollectorId = @Id",
                        new { Id = request.CollectorId });
                }

                var parameters = new DynamicParameters();
                parameters.Add("@SqlFilterExp", sqlFilterExp.ToString(), DbType.String, size: -1);
                parameters.Add("@SqlcollectorId", sqlCollectorId.ToString(), DbType.String, size: -1);

                var rows = await connection.QueryAsync<LoanCommissionRowDto>(
                    "sp_7_16_LoanCommissionReport",
                    parameters,
                    commandType: CommandType.StoredProcedure,
                    commandTimeout: 120
                );

                var resultList = rows.AsList();

                return new LoanCommissionData
                {
                    Rows = resultList,
                    TotalRecords = resultList.Count,
                    TotalInterestAmount = resultList.Sum(r => r.TotalInterestAmount ?? 0),
                    TotalCommissionAmount = resultList.Sum(r => r.CommissionAmount ?? 0),
                    FromDateBs = request.FromDateBs,
                    ToDateBs = request.ToDateBs,
                    CollectorName = collectorName
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