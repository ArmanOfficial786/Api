// Repository/MemberAccount/OthersReport/SavingInterestChangeLogRepository.cs
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using NexgenCosysReport.DbContext;
using NexgenCosysReport.Dtos.RequestDtos.MemberAccount.OthersReport;
using NexgenCosysReport.Inteface.ServiceInterface.Common;
using NexgenCosysReport.Inteface.ServiceInterface.MemberAccount.OthersReport;
using System.Data;
using System.Text;

namespace NexgenCosysReport.Repository.MemberAccount.OthersReport
{
    public class SavingInterestChangeLogRepository : ISavingInterestChangeLogRepository
    {
        private readonly AppDbContext _context;
        private readonly IDateConverterService _dateConverter;
        private readonly ILogger<SavingInterestChangeLogRepository> _logger;

        public SavingInterestChangeLogRepository(
            AppDbContext context,
            IDateConverterService dateConverter,
            ILogger<SavingInterestChangeLogRepository> logger)
        {
            _context = context;
            _dateConverter = dateConverter;
            _logger = logger;
        }

        public async Task<SavingInterestChangeLogData> GetReportDataAsync(SavingInterestChangeLogRequestDto request)
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
                sqlFilterExp.Append($" AND Intlog.createdon BETWEEN '{fromDateStr}' AND '{toDateStr}'");

                // Branch filter (for Account No report type)
                if (request.ReportType == "1" && request.OfficeId.HasValue && request.OfficeId.Value != -1)
                {
                    sqlFilterExp.Append($" AND mam.UsmOfficeId = {request.OfficeId.Value}");
                }

                // Account No filter
                if (request.ReportType == "1" && !string.IsNullOrEmpty(request.AccountNo))
                {
                    sqlFilterExp.Append($" AND mam.AccountNo = '{request.AccountNo}'");
                }

                // Deposit Type filter (for Deposit Type report type)
                if (request.ReportType == "2" && request.DepositTypeId.HasValue && request.DepositTypeId.Value != -1)
                {
                    sqlFilterExp.Append($" AND Intlog.SycDepositTypeId = {request.DepositTypeId.Value}");
                }

                // Order by
                sqlFilterExp.Append(" ORDER BY Intlog.createdon");

                var parameters = new DynamicParameters();
                parameters.Add("@SqlFilterExp", sqlFilterExp.ToString(), DbType.String, size: -1);

                var connectionString = _context.Database.GetConnectionString();
                using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();

                var rows = await connection.QueryAsync<SavingInterestChangeLogRowDto>(
                    "sp_5_43_GetMamAccountInterestChangeLog",
                    parameters,
                    commandType: CommandType.StoredProcedure
                );

                var resultList = rows.AsList();

                // Get deposit type name if applicable
                string? depositTypeName = null;
                if (request.ReportType == "2" && request.DepositTypeId.HasValue && request.DepositTypeId.Value != -1)
                {
                    depositTypeName = await GetDepositTypeNameAsync(request.DepositTypeId.Value);
                }

                return new SavingInterestChangeLogData
                {
                    Rows = resultList,
                    TotalRecords = resultList.Count,
                    FromDateBs = request.FromDateBs,
                    ToDateBs = request.ToDateBs,
                    ReportType = request.ReportType,
                    AccountNo = request.AccountNo,
                    DepositTypeName = depositTypeName,
                    OfficeName = request.OfficeName,
                    TotalChanges = resultList.Count
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetReportDataAsync for Saving Interest Change Log Report");
                throw;
            }
        }

        private async Task<string?> GetDepositTypeNameAsync(long depositTypeId)
        {
            try
            {
                const string query = @"
                    SELECT DepositTypeName 
                    FROM SycDepositType 
                    WHERE SycDepositTypeId = @DepositTypeId AND IsActive = 1";

                var connectionString = _context.Database.GetConnectionString();
                using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();

                return await connection.QueryFirstOrDefaultAsync<string>(
                    query,
                    new { DepositTypeId = depositTypeId }
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting deposit type name for ID: {DepositTypeId}", depositTypeId);
                return null;
            }
        }
    }
}