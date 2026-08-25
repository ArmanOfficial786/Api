using Dapper;
using NexgenCosysReport.DbContext;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using NexgenCosysReport.Dtos.RequestDtos.MemberAccount;
using NexgenCosysReport.Inteface.ServiceInterface.MemberAccount;
using System.Data;

namespace NexgenCosysReport.Repository.MemberAccount
{
    public class SMSCategoryRepository : ISMSCategory
    {
        private readonly AppDbContext _context;
        private readonly ILogger<SMSCategoryRepository> _logger;

        private const string NoFilter = "-1";
        private const int ReportCommandTimeoutSeconds = 180;

        public SMSCategoryRepository(
            AppDbContext context,
            ILogger<SMSCategoryRepository> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<SMSCategoryData> GetSMSCategory(SMSCategoryRequest request)
        {
            var connectionString = _context.Database.GetConnectionString();
            using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();

            string branchFilter = NormalizeId(request.BranchId);
            string smsCategoryFilter = NormalizeId(request.SmsCategoryId);
            string sqlFilterExp = BuildFilterExpression(branchFilter, smsCategoryFilter, request.OrderBy);

            _logger.LogDebug("SMSCategory filter -> {Exp}", sqlFilterExp);

            var parameters = new DynamicParameters();
            parameters.Add("@SqlFilterExp", sqlFilterExp);

            var command = new CommandDefinition(
                "sp_5_43_GetSMSCategory",
                parameters,
                commandType: CommandType.StoredProcedure,
                commandTimeout: ReportCommandTimeoutSeconds);

            var dynamicRows = (await connection.QueryAsync(command)).ToList();

            if (dynamicRows.Count > 0)
            {
                var firstRow = (IDictionary<string, object>)dynamicRows[0];
                _logger.LogDebug("sp_5_43_GetSMSCategory returned columns: {Columns}", string.Join(", ", firstRow.Keys));
            }

            var list = dynamicRows.Select(MapRow).ToList();

            return new SMSCategoryData
            {
                Rows = list,
                TotalRecords = list.Count
            };
        }

        private static SMSCategoryRowDto MapRow(dynamic row)
        {
            var dict = (IDictionary<string, object>)row;

            return new SMSCategoryRowDto
            {
                MemberId = GetString(dict, "MemberId", "MemMemberId"),
                MemberName = GetString(dict, "MemberName", "FullName", "Name"),
                AccountNo = GetString(dict, "AccountNo", "AccNo"),


                DepositTypeName = GetString(dict, "SavingType", "DepositTypeName", "SavingsType", "DepositType"),

                DateOfAccountOpen = GetString(dict, "Date", "DateOfAccountOpen", "AccountOpenDate", "AccountOpenOnBs"),

                SMSCriteria = GetString(dict, "SMSCriteria", "SmsCriteria"),
                SMSMessage = GetString(dict, "SMSMessage", "SmsMessage"),
                Balance = GetDecimal(dict, "Balance")
            };
        }

        private static string? GetString(IDictionary<string, object> row, params string[] candidateKeys)
        {
            foreach (var key in candidateKeys)
            {
                // Dictionary from Dapper's dynamic rows preserves the SP's exact
                // casing, so match case-insensitively here.
                var match = row.Keys.FirstOrDefault(k => string.Equals(k, key, StringComparison.OrdinalIgnoreCase));
                if (match == null) continue;

                var value = row[match];
                if (value == null || value == DBNull.Value) continue;

                var str = value.ToString();
                if (!string.IsNullOrWhiteSpace(str))
                    return str;
            }
            return null;
        }

        private static decimal GetDecimal(IDictionary<string, object> row, params string[] candidateKeys)
        {
            foreach (var key in candidateKeys)
            {
                var match = row.Keys.FirstOrDefault(k => string.Equals(k, key, StringComparison.OrdinalIgnoreCase));
                if (match == null) continue;

                var value = row[match];
                if (value == null || value == DBNull.Value) continue;

                if (decimal.TryParse(value.ToString(), out var result))
                    return result;
            }
            return 0m;
        }

        private static string NormalizeId(string? value)
        {
            if (string.IsNullOrWhiteSpace(value)) return NoFilter;
            var trimmed = value.Trim();
            return (trimmed == "0" || trimmed == NoFilter) ? NoFilter : trimmed;
        }

        private string BuildFilterExpression(string branchFilter, string smsCategoryFilter, string orderBy)
        {
            var filters = new List<string>();

            if (smsCategoryFilter != NoFilter)
                filters.Add($" And a.SycSmsCategoryId = {smsCategoryFilter}");

            if (branchFilter != NoFilter)
                filters.Add($" And a.UsmOfficeId in ({branchFilter})");


            filters.Add(MapOrderBy(orderBy));

            return string.Join(" ", filters);
        }

        private string MapOrderBy(string orderBy)
        {
            return orderBy switch
            {
                "Savings Type" => " order by d.DepositTypeName",
                "Member ID" => " order by substring(m.MemberId, 1,(len(m.MemberId)-charindex('-', m.MemberId))-1), m.MemberId",
                "Member Name" => " order by MemberName",
                "Account No" => " order by substring(a.AccountNo, 1,(len(a.AccountNo)-charindex('-', a.AccountNo))-1), a.AccountNo",
                "Account Open Date" => " order by Date",
                "SMS Criteria" => " order by SMSCriteria",
                _ => " order by substring(m.MemberId, 1,(len(m.MemberId)-charindex('-', m.MemberId))-1), m.MemberId"
            };
        }
    }
}
