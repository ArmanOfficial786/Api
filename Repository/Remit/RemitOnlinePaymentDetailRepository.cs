using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using NexgenCosysReport.DbContext;
using NexgenCosysReport.Dtos.RequestDtos.Remit;
using NexgenCosysReport.Inteface.ServiceInterface.Common;
using NexgenCosysReport.Inteface.ServiceInterface.Remit;
using System.Data;

namespace NexgenCosysReport.Repository.Remit
{
    public class RemitOnlinePaymentDetailRepository : IRemitOnlinePaymentDetailRepository
    {
        private readonly AppDbContext _context;
        private readonly IDateConverterService _dateConverter;
        private readonly ILogger<RemitOnlinePaymentDetailRepository> _logger;

        public RemitOnlinePaymentDetailRepository(
            AppDbContext context,
            IDateConverterService dateConverter,
            ILogger<RemitOnlinePaymentDetailRepository> logger)
        {
            _context = context;
            _dateConverter = dateConverter;
            _logger = logger;
        }

        private static string SanitizeBranchIds(string? branchIds)
        {
            if (string.IsNullOrWhiteSpace(branchIds) || branchIds == "-1" || branchIds == "string")
                return string.Empty;

            var validIds = branchIds
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Where(id => long.TryParse(id, out _));

            return string.Join(",", validIds);
        }

        private static string BuildSqlOrderBy(RemitOnlinePaymentDetailRequestDto request)
        {
            if (string.IsNullOrEmpty(request.OrderBy) || request.OrderBy == "-1")
                return string.Empty;

            return request.OrderBy.Trim() switch
            {
                "Receiver" => "Receiver",
                "Sender" => "Sender",
                "Amount" => "Amount",
                "Date" => "Date",
                "UserName" => "UserName",
                _ => string.Empty
            };
        }

        public async Task<RemitOnlinePaymentDetailData> GetReportDataAsync(RemitOnlinePaymentDetailRequestDto request)
        {
            try
            {
                var branchIds = SanitizeBranchIds(request.BranchIds);

                var connectionString = _context.Database.GetConnectionString();
                using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();

                var fromDateAd = string.Empty;
                var toDateAd = string.Empty;

                if (!string.IsNullOrEmpty(request.FromDateBs))
                {
                    var fromDate = await _dateConverter.NepaliToEnglishAsync(request.FromDateBs);
                    fromDateAd = fromDate.ToString("yyyy-MM-dd");
                }

                if (!string.IsNullOrEmpty(request.ToDateBs))
                {
                    var toDate = await _dateConverter.NepaliToEnglishAsync(request.ToDateBs);
                    toDateAd = toDate.ToString("yyyy-MM-dd");
                }

                string? remittanceGroupName = null;
                if (request.RemittanceGroupId != -1)
                {
                    remittanceGroupName = await connection.QueryFirstOrDefaultAsync<string>(
                        "SELECT ProviderName FROM RemRemittanceGroup WHERE RemRemittanceGroupId = @Id",
                        new { Id = request.RemittanceGroupId });
                }

                var parameters = new DynamicParameters();
                parameters.Add("@officeId", branchIds, DbType.String, size: 225);
                parameters.Add("@IsPaid", request.IsPaid ?? string.Empty, DbType.String, size: 225);
                parameters.Add("@fromDate", fromDateAd, DbType.String, size: 225);
                parameters.Add("@toDate", toDateAd, DbType.String, size: 225);
                parameters.Add("@remitGroupId", request.RemittanceGroupId.ToString(), DbType.String, size: 225);
                parameters.Add("@orderBy", BuildSqlOrderBy(request), DbType.String, size: 225);

                var rows = await connection.QueryAsync<RemitOnlinePaymentDetailRowDto>(
                    "sp_9_154_GetOnlineRemittanceDetailReport",
                    parameters,
                    commandType: CommandType.StoredProcedure,
                    commandTimeout: 120
                );

                var resultList = rows.AsList();

                string branchName = "All";
                if (!string.IsNullOrEmpty(branchIds))
                {
                    var names = await connection.QueryAsync<string>(
                        $"SELECT OfficeName FROM UsmOffice WHERE UsmOfficeId IN ({branchIds})");
                    var nameList = names.Where(n => !string.IsNullOrWhiteSpace(n)).ToList();
                    branchName = nameList.Count > 0 ? string.Join(", ", nameList) : "All";
                }

                return new RemitOnlinePaymentDetailData
                {
                    Rows = resultList,
                    TotalRecords = resultList.Count,
                    TotalAmount = resultList.Sum(r => r.Amount ?? 0),
                    TotalComission = resultList.Sum(r => r.Comission ?? 0),
                    FromDateBs = request.FromDateBs,
                    ToDateBs = request.ToDateBs,
                    BranchName = branchName,
                    RemittanceGroupName = remittanceGroupName ?? "All",
                    IsPaid = request.IsPaid,
                    OrderBy = request.OrderBy
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