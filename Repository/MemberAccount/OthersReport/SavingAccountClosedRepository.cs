// Repository/MemberAccount/OthersReport/SavingAccountClosedRepository.cs
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
    public class SavingAccountClosedRepository : ISavingAccountClosedRepository
    {
        private readonly AppDbContext _context;
        private readonly IDateConverterService _dateConverter;
        private readonly ILogger<SavingAccountClosedRepository> _logger;

        public SavingAccountClosedRepository(
            AppDbContext context,
            IDateConverterService dateConverter,
            ILogger<SavingAccountClosedRepository> logger)
        {
            _context = context;
            _dateConverter = dateConverter;
            _logger = logger;
        }

        public async Task<SavingAccountClosedData> GetReportDataAsync(SavingAccountClosedRequestDto request)
        {
            try
            {
                var sqlFilterExp = new StringBuilder();

                // Treat any non-positive value as "unfiltered", not just exactly -1.
                if (request.MemberId.HasValue && request.MemberId.Value > 0)
                {
                    sqlFilterExp.Append($" AND c.MemMemberRegistrationId = {request.MemberId.Value}");
                }

                if (request.ReportMode == "DateWise" &&
                    !string.IsNullOrEmpty(request.FromDateBs) && request.FromDateBs != "-1" &&
                    !string.IsNullOrEmpty(request.ToDateBs) && request.ToDateBs != "-1")
                {
                    var fromDateAd = await _dateConverter.NepaliToEnglishAsync(request.FromDateBs);
                    var toDateAd = await _dateConverter.NepaliToEnglishAsync(request.ToDateBs);
                    var fromDateStr = fromDateAd.ToString("yyyy-MM-dd");
                    var toDateStr = toDateAd.ToString("yyyy-MM-dd");
                    sqlFilterExp.Append($" AND c.AccountCloseOn BETWEEN '{fromDateStr}' AND '{toDateStr}'");
                }

                if (!string.IsNullOrEmpty(request.BranchIds) && request.BranchIds != "-1")
                {
                    sqlFilterExp.Append($" AND tc.UsmOfficeId IN ({request.BranchIds})");
                }

                // Member is the primary sort key so rows arrive grouped by member first,
                // then deposit type — matches the report's two-level visual grouping.
                var orderByClause = BuildOrderByClause(request.OrderBy);

                var parameters = new DynamicParameters();
                parameters.Add("@SqlFilterExp", sqlFilterExp.ToString() + orderByClause, DbType.String, size: -1);

                var connectionString = _context.Database.GetConnectionString();
                using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();

                var rows = await connection.QueryAsync<SavingAccountClosedRowDto>(
                    "sp_5_43_GetAccountClose",
                    parameters,
                    commandType: CommandType.StoredProcedure
                );

                var resultList = rows.AsList();

                string? selectedMemberId = null;
                string? selectedMemberName = null;
                if (request.MemberId.HasValue && request.MemberId.Value > 0)
                {
                    var member = await GetMemberDetailsAsync(request.MemberId.Value);
                    if (member != null)
                    {
                        selectedMemberId = member.MemberId;
                        selectedMemberName = member.MemberName;
                    }
                }

                var branchNames = await GetBranchNamesByIdsAsync(request.BranchIds);

                return new SavingAccountClosedData
                {
                    Rows = resultList,
                    TotalRecords = resultList.Count,
                    TotalCloseAmount = resultList.Sum(r => r.CloseAmount ?? 0),
                    FromDateBs = request.FromDateBs,
                    ToDateBs = request.ToDateBs,
                    BranchNames = branchNames,
                    OrderBy = request.OrderBy,
                    ReportMode = request.ReportMode,
                    SelectedMemberId = selectedMemberId,
                    SelectedMemberName = selectedMemberName,
                    TotalClosedAccounts = resultList.Count
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetReportDataAsync for Saving Account Closed Report");
                throw;
            }
        }

        private async Task<string> GetBranchNamesByIdsAsync(string? branchIdsCsv)
        {
            if (string.IsNullOrWhiteSpace(branchIdsCsv) || branchIdsCsv == "-1")
            {
                return "All";
            }

            try
            {
                var ids = branchIdsCsv
                    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .Select(id => long.TryParse(id, out var parsed) ? parsed : (long?)null)
                    .Where(id => id.HasValue)
                    .Select(id => id!.Value)
                    .ToList();

                if (!ids.Any())
                {
                    return "All";
                }

                var connectionString = _context.Database.GetConnectionString();
                using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();

                const string sql = "SELECT OfficeName FROM UsmOffice WHERE UsmOfficeId IN @Ids";
                var names = (await connection.QueryAsync<string>(sql, new { Ids = ids })).ToList();

                return names.Any() ? string.Join(", ", names) : branchIdsCsv;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetBranchNamesByIdsAsync");
                return branchIdsCsv;
            }
        }

        private static string BuildOrderByClause(string orderBy)
        {
            return orderBy switch
            {
                "Member Name" => " ORDER BY m.MemberId, Name",
                "Account No" => " ORDER BY m.MemberId, substring(a.AccountNo, 1,(len(a.AccountNo)-charindex('-', a.AccountNo))-1), a.AccountNo",
                "A‎/‎C Open Date" => " ORDER BY m.MemberId, a.AccountOpenOnBs",
                "A‎/‎C Close Date" => " ORDER BY m.MemberId, c.AccountCloseOnBs",
                "Close Amount" => " ORDER BY m.MemberId, c.AccountCloseAmount DESC",
                "Member Id" => " ORDER BY substring(m.MemberId, 1,(len(m.MemberId)-charindex('-', m.MemberId))-1), m.MemberId",
                _ => " ORDER BY substring(m.MemberId, 1,(len(m.MemberId)-charindex('-', m.MemberId))-1), m.MemberId"
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