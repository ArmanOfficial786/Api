using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using NexgenCosysReport.DbContext;
using NexgenCosysReport.Dtos.RequestDtos.Remit;
using NexgenCosysReport.Inteface.ServiceInterface.Common;
using NexgenCosysReport.Inteface.ServiceInterface.Remit;
using System.Data;
using System.Text;

namespace NexgenCosysReport.Repository.Remit
{
    public class RemittancePaymentDetailRepository : IRemittancePaymentDetailRepository
    {
        private readonly AppDbContext _context;
        private readonly IDateConverterService _dateConverter;
        private readonly ILogger<RemittancePaymentDetailRepository> _logger;

        public RemittancePaymentDetailRepository(
            AppDbContext context,
            IDateConverterService dateConverter,
            ILogger<RemittancePaymentDetailRepository> logger)
        {
            _context = context;
            _dateConverter = dateConverter;
            _logger = logger;
        }

        private static string BuildSqlOrderBy(RemittancePaymentDetailRequestDto request)
        {
            if (string.IsNullOrEmpty(request.OrderBy) || request.OrderBy == "-1")
                return string.Empty;

            return request.OrderBy.Trim() switch
            {
                "Receiver" => " order by A.Receiver ",
                "Amount" => " order by A.Amount ",
                "PaymentBy" => " order by A.PaymentBy ",
                "TransactionOn" => " order by A.TransactionOn",
                "CashTeller" => " order by A.CashTeller",
                _ => string.Empty
            };
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

        public async Task<RemittancePaymentDetailData> GetReportDataAsync(RemittancePaymentDetailRequestDto request)
        {
            try
            {
                var branchIds = SanitizeBranchIds(request.BranchIds);

                var connectionString = _context.Database.GetConnectionString();
                using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();

                var sqlFilterExp = new StringBuilder();
                string? remittanceTypeName = null;

                if (!string.IsNullOrEmpty(request.FromDateBs) && !string.IsNullOrEmpty(request.ToDateBs))
                {
                    var fromDateAd = await _dateConverter.NepaliToEnglishAsync(request.FromDateBs);
                    var toDateAd = await _dateConverter.NepaliToEnglishAsync(request.ToDateBs);

                    sqlFilterExp.Append(" And r.TransactionOn between '")
                                .Append(fromDateAd.ToString("yyyy-MM-dd"))
                                .Append("' And '")
                                .Append(toDateAd.ToString("yyyy-MM-dd"))
                                .Append("' ");
                }

                if (!string.IsNullOrEmpty(branchIds))
                {
                    sqlFilterExp.Append(" And o.UsmOfficeId in ( ").Append(branchIds).Append(" )");
                }

                if (request.RemittanceTypeId != -1)
                {
                    sqlFilterExp.Append(" And rt.RemRemittanceTypeId = ").Append(request.RemittanceTypeId);

                    remittanceTypeName = await connection.QueryFirstOrDefaultAsync<string>(
                        "SELECT RemittanceTypeName FROM RemRemittanceType WHERE RemRemittanceTypeId = @Id",
                        new { Id = request.RemittanceTypeId });
                }

                if (request.RemittanceServiceType == "O")
                {
                    sqlFilterExp.Append("and r.IsOnlineRemit =1");
                }
                else if (request.RemittanceServiceType == "F")
                {
                    sqlFilterExp.Append("and r.IsOnlineRemit =0");
                }

                var parameters = new DynamicParameters();
                parameters.Add("@SqlFilterExp", sqlFilterExp.ToString(), DbType.String, size: -1);
                parameters.Add("@SqlOrderByExp", BuildSqlOrderBy(request), DbType.String, size: -1);

                var rows = await connection.QueryAsync<RemittancePaymentDetailRowDto>(
                    "sp_9_64_GetRemittancePaymentDetails",
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

                return new RemittancePaymentDetailData
                {
                    Rows = resultList,
                    TotalRecords = resultList.Count,
                    TotalAmount = resultList.Sum(r => r.Amount ?? 0),
                    FromDateBs = request.FromDateBs,
                    ToDateBs = request.ToDateBs,
                    BranchName = branchName,
                    RemittanceTypeName = remittanceTypeName ?? "All",
                    RemittanceServiceType = request.RemittanceServiceType,
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