// Repositories/MemberAccount/FixedDepositCertificateSchedule/FixedDepositCertificateScheduleRepository.cs
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using NexgenCosysReport.DbContext;
using NexgenCosysReport.Dtos.RequestDtos.MemberAccount.InterestPayableReport;
using NexgenCosysReport.Inteface.ServiceInterface.Common;
using NexgenCosysReport.Inteface.ServiceInterface.MemberAccount.InterestPayableReport;
using System.Data;

namespace NexgenCosysReport.Repositories.MemberAccount.FixedDepositCertificateSchedule
{
    public class FixedDepositCertificateScheduleRepository : IFixedDepositCertificateScheduleRepository
    {
        private readonly AppDbContext _context;
        private readonly IDateConverterService _dateConverter;
        private readonly ILogger<FixedDepositCertificateScheduleRepository> _logger;

        public FixedDepositCertificateScheduleRepository(
            AppDbContext context,
            IDateConverterService dateConverter,
            ILogger<FixedDepositCertificateScheduleRepository> logger)
        {
            _context = context;
            _dateConverter = dateConverter;
            _logger = logger;
        }

        public async Task<List<FixedDepositAccountListDto>> GetFixedDepositAccountsAsync(long userId)
        {
            try
            {
                var sql = @"EXEC sp_5_43_GetFixedDepositAccounts @UserId";

                var parameters = new DynamicParameters();
                parameters.Add("@UserId", userId, DbType.Int64);

                var connectionString = _context.Database.GetConnectionString();
                using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();

                var result = await connection.QueryAsync<FixedDepositAccountListDto>(
                    sql,
                    parameters,
                    commandType: CommandType.Text
                );

                return result.AsList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetFixedDepositAccountsAsync");
                throw;
            }
        }

        public async Task<FixedDepositCertificateScheduleData> GetCertificateDataAsync(FixedDepositCertificateScheduleRequestDto request)
        {
            try
            {
                var parameters = new DynamicParameters();
                parameters.Add("@AccountId", request.AccountId, DbType.Int64);

                var connectionString = _context.Database.GetConnectionString();
                using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();

                // Get certificate details
                var certificateDetail = await connection.QueryFirstOrDefaultAsync<FixedDepositCertificateDetailDto>(
                    "sp_5_43_GetFixedDepositCertificateDetails",
                    parameters,
                    commandType: CommandType.StoredProcedure
                );

                return new FixedDepositCertificateScheduleData
                {
                    CertificateDetail = certificateDetail,
                    AccountNo = certificateDetail?.AccountNo ?? request.AccountNo,
                    MemberId = certificateDetail?.MemberId ?? request.MemberId,
                    MemberName = certificateDetail?.MemberName ?? request.MemberName,
                    ShowHeader = request.ShowHeader,
                    ReportType = request.ReportType,
                    TotalRecords = certificateDetail != null ? 1 : 0
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetCertificateDataAsync for AccountId: {AccountId}", request.AccountId);
                throw;
            }
        }

        public async Task<FixedDepositCertificateScheduleData> GetScheduleDataAsync(FixedDepositCertificateScheduleRequestDto request)
        {
            try
            {
                var parameters = new DynamicParameters();
                parameters.Add("@AccountId", request.AccountId, DbType.Int64);

                var connectionString = _context.Database.GetConnectionString();
                using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();

                // Get certificate details
                var certificateDetail = await connection.QueryFirstOrDefaultAsync<FixedDepositCertificateDetailDto>(
                    "sp_5_43_GetFixedDepositCertificateDetails",
                    parameters,
                    commandType: CommandType.StoredProcedure
                );

                // Get schedule rows
                var scheduleRows = await connection.QueryAsync<FixedDepositScheduleRowDto>(
                    "sp_5_43_GetFixedDepositSchedule",
                    parameters,
                    commandType: CommandType.StoredProcedure
                );

                var scheduleList = scheduleRows.AsList();

                return new FixedDepositCertificateScheduleData
                {
                    CertificateDetail = certificateDetail,
                    ScheduleRows = scheduleList,
                    AccountNo = certificateDetail?.AccountNo ?? request.AccountNo,
                    MemberId = certificateDetail?.MemberId ?? request.MemberId,
                    MemberName = certificateDetail?.MemberName ?? request.MemberName,
                    ShowHeader = request.ShowHeader,
                    ReportType = request.ReportType,
                    TotalRecords = scheduleList.Count,
                    TotalPrincipal = scheduleList.Sum(r => r.PrincipalAmount ?? 0),
                    TotalInterest = scheduleList.Sum(r => r.InterestAmount ?? 0),
                    TotalAmount = scheduleList.Sum(r => r.TotalAmount ?? 0)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetScheduleDataAsync for AccountId: {AccountId}", request.AccountId);
                throw;
            }
        }
    }
}