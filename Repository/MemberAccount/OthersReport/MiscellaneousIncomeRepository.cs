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
    public class MiscellaneousIncomeRepository : IMiscellaneousIncomeRepository
    {
        private readonly AppDbContext _context;
        private readonly IDateConverterService _dateConverter;
        private readonly ILogger<MiscellaneousIncomeRepository> _logger;

        public MiscellaneousIncomeRepository(
            AppDbContext context,
            IDateConverterService dateConverter,
            ILogger<MiscellaneousIncomeRepository> logger)
        {
            _context = context;
            _dateConverter = dateConverter;
            _logger = logger;
        }

        public async Task<MiscellaneousIncomeData> GetReportDataAsync(MiscellaneousIncomeRequestDto request)
        {
            try
            {
                var fromDateAd = await _dateConverter.NepaliToEnglishAsync(request.FromDateBs);
                var toDateAd = await _dateConverter.NepaliToEnglishAsync(request.ToDateBs);

                var fromDateStr = fromDateAd.ToString("yyyy-MM-dd");
                var toDateStr = toDateAd.ToString("yyyy-MM-dd");

                var sqlFilterExp = new StringBuilder();

                if (!string.IsNullOrEmpty(request.BranchIds) && request.BranchIds != "-1")
                {
                    sqlFilterExp.Append($" AND t.UsmOfficeId IN ({request.BranchIds})");
                }

                if (request.MemberId.HasValue && request.MemberId.Value != -1)
                {
                    sqlFilterExp.Append($" AND t.MemMemberRegistrationId = {request.MemberId.Value}");
                }

                sqlFilterExp.Append($" AND t.TransactionOn BETWEEN '{fromDateStr}' AND '{toDateStr}'");

                var orderByClause = BuildOrderByClause(request.OrderBy);

                string spName = request.ReportType == "Fund"
                    ? "sp_5_43_GetMiscellaneousFund"
                    : "sp_5_43_GetMiscellaneousIncome";

                var parameters = new DynamicParameters();
                parameters.Add("@SqlFilterExp", sqlFilterExp.ToString() + orderByClause, DbType.String, size: -1);

                var connectionString = _context.Database.GetConnectionString();
                using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();

                // Dynamic query (dictionary rows) so Type/AccountNo can be explicitly
                // remapped to Particulars/Details below — a strict typed query would
                // leave those two properties null since the SP's aliases don't match
                // the DTO's property names.
                var rawRows = await connection.QueryAsync(
                    spName,
                    parameters,
                    commandType: CommandType.StoredProcedure
                );

                var resultList = new List<MiscellaneousIncomeRowDto>();
                foreach (IDictionary<string, object> r in rawRows)
                {
                    resultList.Add(new MiscellaneousIncomeRowDto
                    {
                        Date = GetString(r, "Date"),
                        MemberId = GetString(r, "MemberId"),
                        MemberName = GetString(r, "MemberName"),

                        // Confirmed from sp_5_43_GetMiscellaneousIncome / sp_5_43_GetMiscellaneousFund:
                        // "Type" is the charge/ledger-head label -> our group header (Particulars).
                        Particulars = GetString(r, "Type"),

                        // "AccountNo" is the per-transaction CASE-built description
                        // (e.g. "Loan Issue Deposit (SL-01-17)", "Bank Deposit") -> Details.
                        Details = GetString(r, "AccountNo"),

                        Amount = GetDecimal(r, "Amount"),
                        Operator = GetString(r, "Operator", "OperatorName")
                    });
                }

                string? selectedMemberId = null;
                string? selectedMemberName = null;
                if (request.MemberId.HasValue && request.MemberId.Value != -1)
                {
                    var member = await GetMemberDetailsAsync(request.MemberId.Value);
                    if (member != null)
                    {
                        selectedMemberId = member.MemberId;
                        selectedMemberName = member.MemberName;
                    }
                }

                return new MiscellaneousIncomeData
                {
                    Rows = resultList,
                    TotalRecords = resultList.Count,
                    TotalAmount = resultList.Sum(r => r.Amount ?? 0),
                    FromDateBs = request.FromDateBs,
                    ToDateBs = request.ToDateBs,
                    BranchNames = request.BranchName,
                    OrderBy = request.OrderBy,
                    ReportType = request.ReportType,
                    SelectedMemberId = selectedMemberId,
                    SelectedMemberName = selectedMemberName
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetReportDataAsync for Miscellaneous Income");
                throw;
            }
        }

        // --------------------------------------------------------------
        // Case-insensitive, multi-alias lookups into a Dapper dynamic row.
        // --------------------------------------------------------------
        private static string? GetString(IDictionary<string, object> row, params string[] keys)
        {
            foreach (var key in keys)
            {
                var match = row.Keys.FirstOrDefault(k => string.Equals(k, key, StringComparison.OrdinalIgnoreCase));
                if (match != null && row[match] != null && row[match] != DBNull.Value)
                    return row[match].ToString();
            }
            return null;
        }

        private static decimal? GetDecimal(IDictionary<string, object> row, params string[] keys)
        {
            foreach (var key in keys)
            {
                var match = row.Keys.FirstOrDefault(k => string.Equals(k, key, StringComparison.OrdinalIgnoreCase));
                if (match != null && row[match] != null && row[match] != DBNull.Value &&
                    decimal.TryParse(row[match].ToString(), out var val))
                {
                    return val;
                }
            }
            return null;
        }

        private static string BuildOrderByClause(string orderBy)
        {
            // NOTE: these column names must match the SP's own SELECT aliases
            // (Type, AccountNo, MemberIdFirst, MemberIdLast, MemberName, Date, Amount) —
            // @SqlFilterExp is concatenated directly after the SP's WHERE clause and
            // executed as part of that same SELECT, so ORDER BY sees the SP's aliases,
            // not our C# DTO property names.
            return orderBy switch
            {
                // SP already exposes MemberIdFirst/MemberIdLast as numeric parts — use them directly
                // instead of re-deriving via substring/charindex.
                "Member Id" => " ORDER BY MemberIdFirst, MemberIdLast",
                "Member Name" => " ORDER BY MemberName",
                "Particulars" => " ORDER BY Type",
                "Date" => " ORDER BY Date",
                "Amount" => " ORDER BY Amount DESC",
                _ => " ORDER BY MemberIdFirst, MemberIdLast"
            };
        }

        private async Task<MemberDetailDto?> GetMemberDetailsAsync(long memberRegistrationId)
        {
            try
            {
                const string query = @"
                    SELECT 
                        MemberId,
                        CONCAT(FirstName, ' ', ISNULL(MiddleName, ''), ' ', LastName) AS MemberName
                    FROM MemMemberRegistration 
                    WHERE MemMemberRegistrationId = @MemberId";

                var connectionString = _context.Database.GetConnectionString();
                using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();

                return await connection.QueryFirstOrDefaultAsync<MemberDetailDto>(
                    query,
                    new { MemberId = memberRegistrationId }
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting member details for ID: {MemberId}", memberRegistrationId);
                return null;
            }
        }

        private class MemberDetailDto
        {
            public string? MemberId { get; set; }
            public string? MemberName { get; set; }
        }
    }
}