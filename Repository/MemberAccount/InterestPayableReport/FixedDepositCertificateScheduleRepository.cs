using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using NexgenCosysReport.DbContext;
using NexgenCosysReport.Dtos.RequestDtos.MemberAccount.InterestPayableReport;
using NexgenCosysReport.Inteface.ServiceInterface.Common;
using NexgenCosysReport.Inteface.ServiceInterface.MemberAccount.InterestPayableReport;
using System.Data;

namespace NexgenCosysReport.Repository.MemberAccount.FixedDepositCertificateSchedule
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

        public async Task<FixedDepositCertificateScheduleData> GetCertificateDataAsync(
            FixedDepositCertificateScheduleRequestDto request, long userId)
        {
            try
            {
                _logger.LogInformation(
                    "GetCertificateDataAsync requested by UserId: {UserId} for AccountId: {AccountId}",
                    userId, request.AccountId);

                var certificateDetail = await GetCertificateDetailInternalAsync(request.AccountId);

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
                _logger.LogError(ex, "Error in GetCertificateDataAsync for AccountId: {AccountId}, UserId: {UserId}",
                    request.AccountId, userId);
                throw;
            }
        }

        public async Task<FixedDepositCertificateScheduleData> GetScheduleDataAsync(
            FixedDepositCertificateScheduleRequestDto request, long userId)
        {
            try
            {
                _logger.LogInformation(
                    "GetScheduleDataAsync requested by UserId: {UserId} for AccountId: {AccountId}",
                    userId, request.AccountId);

                var certificateDetail = await GetCertificateDetailInternalAsync(request.AccountId);

                // sp_5_43_GetFixedDepositSchedule expects @SqlFilterAccountId (nvarchar)
                var scheduleParameters = new DynamicParameters();
                scheduleParameters.Add("@SqlFilterAccountId", request.AccountId.ToString(), DbType.String, size: -1);

                var connectionString = _context.Database.GetConnectionString();
                using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();

                var scheduleRows = await connection.QueryAsync<FixedDepositScheduleRowDto>(
                    "sp_5_43_GetFixedDepositSchedule",
                    scheduleParameters,
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
                    TotalInterest = scheduleList.Sum(r => r.Interest ?? 0),
                    TotalTax = scheduleList.Sum(r => r.Tax ?? 0),
                    TotalNetAmount = scheduleList.Sum(r => r.NetAmount ?? 0)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetScheduleDataAsync for AccountId: {AccountId}, UserId: {UserId}",
                    request.AccountId, userId);
                throw;
            }
        }

        /// <summary>
        /// sp_5_43_GetFixedDepositCertificateDetails requires @SqlFilterExpDetails (nvarchar(max)),
        /// which the SP concatenates directly into a dynamic SQL WHERE clause as the account id.
        /// </summary>
        private async Task<FixedDepositCertificateDetailDto?> GetCertificateDetailInternalAsync(long accountId)
        {
            var parameters = new DynamicParameters();
            parameters.Add("@SqlFilterExpDetails", accountId.ToString(), DbType.String, size: -1);

            var connectionString = _context.Database.GetConnectionString();
            using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();

            return await connection.QueryFirstOrDefaultAsync<FixedDepositCertificateDetailDto>(
                "sp_5_43_GetFixedDepositCertificateDetails",
                parameters,
                commandType: CommandType.StoredProcedure
            );
        }
    }
}