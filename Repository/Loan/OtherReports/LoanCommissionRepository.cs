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

                // --------------------------------------------------------------
                // Build filter expression matching legacy BLL:
                // 1. Date range (@SqlFilterExp) - Ac.TransactionOnBs between ...
                // 2. Collector filter (@SqlcollectorId) - Ac.HurCollectorId = ...
                // Both are optional and independent, matching the SP's own
                // usage comments. CollectorId is a plain long on the DTO
                // (not nullable), so -1 is the "no filter" sentinel.
                // --------------------------------------------------------------
                if (!string.IsNullOrEmpty(request.FromDateBs) && !string.IsNullOrEmpty(request.ToDateBs))
                {
                    sqlFilterExp.Append(" And Ac.TransactionOnBs between '").Append(request.FromDateBs.Trim())
                                .Append("' And '").Append(request.ToDateBs.Trim()).Append("'");
                }

                if (request.CollectorId != -1)
                {
                    sqlCollectorId.Append(" And Ac.HurCollectorId = ").Append(request.CollectorId);
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

                // --------------------------------------------------------------
                // Collector name comes straight off the SP's own result set
                // (CollectorFullName) - no lookup against any collector table.
                // --------------------------------------------------------------
                var collectorName = resultList.FirstOrDefault()?.CollectorFullName;

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