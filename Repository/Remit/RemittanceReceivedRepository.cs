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
    public class RemittanceReceivedRepository : IRemittanceReceivedRepository
    {
        private readonly AppDbContext _context;
        private readonly IDateConverterService _dateConverter;
        private readonly ILogger<RemittanceReceivedRepository> _logger;

        public RemittanceReceivedRepository(
            AppDbContext context,
            IDateConverterService dateConverter,
            ILogger<RemittanceReceivedRepository> logger)
        {
            _context = context;
            _dateConverter = dateConverter;
            _logger = logger;
        }

        private static string BuildSqlOrderBy(RemittanceReceivedRequestDto request)
        {
            if (string.IsNullOrEmpty(request.OrderBy) || request.OrderBy == "-1")
                return string.Empty;

            return request.OrderBy.Trim() switch
            {
                "Receiver" => " order by Receiver ",
                "Sender" => " order by Sender ",
                "Amount" => " order by Amount ",
                "Comission" => " order by Comission ",
                "Balance" => " order by Balance ",
                "TransactionOn" => " order by TransactionOn",
                "CashTeller" => " order by CashTeller",
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

        public async Task<RemittanceReceivedData> GetReportDataAsync(RemittanceReceivedRequestDto request)
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
                    sqlFilterExp.Append(" And o.UsmOfficeId In ( ").Append(branchIds).Append(" )");
                }

                if (request.RemittanceTypeId != -1)
                {
                    sqlFilterExp.Append(" And r.RemRemittanceTypeId = ").Append(request.RemittanceTypeId);

                    remittanceTypeName = await connection.QueryFirstOrDefaultAsync<string>(
                        "SELECT RemittanceTypeName FROM RemRemittanceType WHERE RemRemittanceTypeId = @Id",
                        new { Id = request.RemittanceTypeId });
                }

                sqlFilterExp.Append(BuildSqlOrderBy(request));

                var parameters = new DynamicParameters();
                parameters.Add("@SqlFilterExp", sqlFilterExp.ToString(), DbType.String, size: -1);

                var rows = await connection.QueryAsync<RemittanceReceivedRowDto>(
                    "sp_9_63_GetRemittanceReceived",
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

                return new RemittanceReceivedData
                {
                    Rows = resultList,
                    TotalRecords = resultList.Count,
                    TotalAmount = resultList.Sum(r => r.Amount ?? 0),
                    TotalComission = resultList.Sum(r => r.Comission ?? 0),
                    TotalBalance = resultList.Sum(r => r.Balance ?? 0),
                    FromDateBs = request.FromDateBs,
                    ToDateBs = request.ToDateBs,
                    BranchName = branchName,
                    RemittanceTypeName = remittanceTypeName ?? "All",
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