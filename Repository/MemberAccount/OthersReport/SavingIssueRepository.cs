//// Repository/MemberAccount/OthersReport/SavingIssueRepository.cs
//using Dapper;
//using Microsoft.Data.SqlClient;
//using Microsoft.EntityFrameworkCore;
//using NexgenCosysReport.DbContext;
//using NexgenCosysReport.Dtos.RequestDtos.MemberAccount.OthersReport;
//using NexgenCosysReport.Inteface.ServiceInterface.Common;
//using NexgenCosysReport.Inteface.ServiceInterface.MemberAccount.OthersReport;
//using System.Data;
//using System.Text;

//namespace NexgenCosysReport.Repository.MemberAccount.OthersReport
//{
//    public class SavingIssueRepository : ISavingIssueRepository
//    {
//        private readonly AppDbContext _context;
//        private readonly IDateConverterService _dateConverter;
//        private readonly ILogger<SavingIssueRepository> _logger;

//        public SavingIssueRepository(
//            AppDbContext context,
//            IDateConverterService dateConverter,
//            ILogger<SavingIssueRepository> logger)
//        {
//            _context = context;
//            _dateConverter = dateConverter;
//            _logger = logger;
//        }

//        public async Task<SavingIssueData> GetReportDataAsync(SavingIssueRequestDto request)
//        {
//            try
//            {
//                // Build filter expression
//                var sqlFilterExp = new StringBuilder();

//                // Deposit Type filter
//                if (request.DepositTypeId.HasValue && request.DepositTypeId.Value != -1)
//                {
//                    sqlFilterExp.Append($" AND d.SycDepositTypeId = {request.DepositTypeId.Value}");
//                }

//                // Date filter - for DateWise mode
//                if (request.ReportMode == "DateWise" &&
//                    !string.IsNullOrEmpty(request.FromDateBs) && request.FromDateBs != "-1" &&
//                    !string.IsNullOrEmpty(request.ToDateBs) && request.ToDateBs != "-1")
//                {
//                    var fromDateAd = await _dateConverter.NepaliToEnglishAsync(request.FromDateBs);
//                    var toDateAd = await _dateConverter.NepaliToEnglishAsync(request.ToDateBs);
//                    var fromDateStr = fromDateAd.ToString("yyyy-MM-dd");
//                    var toDateStr = toDateAd.ToString("yyyy-MM-dd");
//                    sqlFilterExp.Append($" AND a.AccountOpenOn BETWEEN '{fromDateStr}' AND '{toDateStr}'");
//                }

//                // Branch filter
//                if (!string.IsNullOrEmpty(request.BranchIds) && request.BranchIds != "-1")
//                {
//                    sqlFilterExp.Append($" AND o.UsmOfficeId IN ({request.BranchIds})");
//                }

//                // Member Group filter
//                if (request.MemberGroupId.HasValue && request.MemberGroupId.Value != -1)
//                {
//                    sqlFilterExp.Append($" AND m.SycMemberGroupId = {request.MemberGroupId.Value}");
//                }

//                // Collector filter
//                if (request.CollectorId.HasValue && request.CollectorId.Value != -1)
//                {
//                    sqlFilterExp.Append($" AND a.HurCollectorId = {request.CollectorId.Value}");
//                }

//                // Build Order By clause
//                var orderByClause = BuildOrderByClause(request.OrderBy);

//                var parameters = new DynamicParameters();
//                parameters.Add("@SqlFilterExp", sqlFilterExp.ToString() + orderByClause, DbType.String, size: -1);

//                var connectionString = _context.Database.GetConnectionString();
//                using var connection = new SqlConnection(connectionString);
//                await connection.OpenAsync();

//                var rows = await connection.QueryAsync<SavingIssueRowDto>(
//                    "sp_5_43_GetSavingIssue",
//                    parameters,
//                    commandType: CommandType.StoredProcedure
//                );

//                var resultList = rows.AsList();

//                // Get additional details if filters are applied
//                string? depositTypeName = null;
//                string? collectorName = null;
//                string? memberGroupName = null;

//                if (request.DepositTypeId.HasValue && request.DepositTypeId.Value != -1)
//                {
//                    depositTypeName = await GetDepositTypeNameAsync(request.DepositTypeId.Value);
//                }
//                if (request.CollectorId.HasValue && request.CollectorId.Value != -1)
//                {
//                    collectorName = await GetCollectorNameAsync(request.CollectorId.Value);
//                }
//                if (request.MemberGroupId.HasValue && request.MemberGroupId.Value != -1)
//                {
//                    memberGroupName = await GetMemberGroupNameAsync(request.MemberGroupId.Value);
//                }

//                return new SavingIssueData
//                {
//                    Rows = resultList,
//                    TotalRecords = resultList.Count,
//                    TotalOpeningBalance = resultList.Sum(r => r.OpeningBalance ?? 0),
//                    FromDateBs = request.FromDateBs,
//                    ToDateBs = request.ToDateBs,
//                    BranchNames = request.BranchName,
//                    OrderBy = request.OrderBy,
//                    ReportMode = request.ReportMode,
//                    DepositTypeName = depositTypeName,
//                    CollectorName = collectorName,
//                    MemberGroupName = memberGroupName,
//                    TotalAccounts = resultList.Count
//                };
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, "Error in GetReportDataAsync for Saving Issue Report");
//                throw;
//            }
//        }

//        private static string BuildOrderByClause(string orderBy)
//        {
//            return orderBy switch
//            {
//                "Member Id" => " ORDER BY substring(m.MemberId, 1,(len(m.MemberId)-charindex('-', m.MemberId))-1), m.MemberId",
//                "Member Name" => " ORDER BY Name",
//                "Account No" => " ORDER BY substring(a.AccountNo, 1,(len(a.AccountNo)-charindex('-', a.AccountNo))-1), a.AccountNo",
//                "A‎/‎C Open Date" => " ORDER BY a.AccountOpenOnBs",
//                "Interest Rate" => " ORDER BY a.InterestRate DESC",
//                "Sms Category" => " ORDER BY a.SycSmsCategoryId",
//                _ => " ORDER BY m.MemberId"
//            };
//        }

//        private async Task<string?> GetDepositTypeNameAsync(long depositTypeId)
//        {
//            try
//            {
//                const string query = @"
//                    SELECT DepositTypeName 
//                    FROM SycDepositType 
//                    WHERE SycDepositTypeId = @DepositTypeId AND IsActive = 1";

//                var connectionString = _context.Database.GetConnectionString();
//                using var connection = new SqlConnection(connectionString);
//                await connection.OpenAsync();

//                return await connection.QueryFirstOrDefaultAsync<string>(
//                    query,
//                    new { DepositTypeId = depositTypeId }
//                );
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, "Error getting deposit type name for ID: {DepositTypeId}", depositTypeId);
//                return null;
//            }
//        }

//        private async Task<string?> GetCollectorNameAsync(long collectorId)
//        {
//            try
//            {
//                const string query = @"
//                    SELECT CollectorFullName 
//                    FROM HurCollector 
//                    WHERE HurCollectorId = @CollectorId AND IsActive = 1";

//                var connectionString = _context.Database.GetConnectionString();
//                using var connection = new SqlConnection(connectionString);
//                await connection.OpenAsync();

//                return await connection.QueryFirstOrDefaultAsync<string>(
//                    query,
//                    new { CollectorId = collectorId }
//                );
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, "Error getting collector name for ID: {CollectorId}", collectorId);
//                return null;
//            }
//        }

//        private async Task<string?> GetMemberGroupNameAsync(long memberGroupId)
//        {
//            try
//            {
//                const string query = @"
//                    SELECT Name 
//                    FROM SycMemberGroup 
//                    WHERE SycMemberGroupId = @MemberGroupId AND IsActive = 1";

//                var connectionString = _context.Database.GetConnectionString();
//                using var connection = new SqlConnection(connectionString);
//                await connection.OpenAsync();

//                return await connection.QueryFirstOrDefaultAsync<string>(
//                    query,
//                    new { MemberGroupId = memberGroupId }
//                );
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, "Error getting member group name for ID: {MemberGroupId}", memberGroupId);
//                return null;
//            }
//        }
//    }
//}






// Repository/MemberAccount/OthersReport/SavingIssueRepository.cs
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
    public class SavingIssueRepository : ISavingIssueRepository
    {
        private readonly AppDbContext _context;
        private readonly IDateConverterService _dateConverter;
        private readonly ILogger<SavingIssueRepository> _logger;

        public SavingIssueRepository(
            AppDbContext context,
            IDateConverterService dateConverter,
            ILogger<SavingIssueRepository> logger)
        {
            _context = context;
            _dateConverter = dateConverter;
            _logger = logger;
        }

        public async Task<SavingIssueData> GetReportDataAsync(SavingIssueRequestDto request)
        {
            try
            {
                var sqlFilterExp = new StringBuilder();

                // Root-cause fix: treat any non-positive value (not just exactly -1) as
                // "unfiltered". JSON payloads that omit these fields, or send 0, must not
                // silently turn into a real filter on group/deposit-type/collector 0.
                if (request.DepositTypeId.HasValue && request.DepositTypeId.Value > 0)
                {
                    sqlFilterExp.Append($" AND d.SycDepositTypeId = {request.DepositTypeId.Value}");
                }

                if (request.ReportMode == "DateWise" &&
                    !string.IsNullOrEmpty(request.FromDateBs) && request.FromDateBs != "-1" &&
                    !string.IsNullOrEmpty(request.ToDateBs) && request.ToDateBs != "-1")
                {
                    var fromDateAd = await _dateConverter.NepaliToEnglishAsync(request.FromDateBs);
                    var toDateAd = await _dateConverter.NepaliToEnglishAsync(request.ToDateBs);
                    var fromDateStr = fromDateAd.ToString("yyyy-MM-dd");
                    var toDateStr = toDateAd.ToString("yyyy-MM-dd");
                    sqlFilterExp.Append($" AND a.AccountOpenOn BETWEEN '{fromDateStr}' AND '{toDateStr}'");
                }

                if (!string.IsNullOrEmpty(request.BranchIds) && request.BranchIds != "-1")
                {
                    sqlFilterExp.Append($" AND o.UsmOfficeId IN ({request.BranchIds})");
                }

                if (request.MemberGroupId.HasValue && request.MemberGroupId.Value > 0)
                {
                    sqlFilterExp.Append($" AND m.SycMemberGroupId = {request.MemberGroupId.Value}");
                }

                if (request.CollectorId.HasValue && request.CollectorId.Value > 0)
                {
                    sqlFilterExp.Append($" AND a.HurCollectorId = {request.CollectorId.Value}");
                }

                // DepositTypeName is the primary sort key so rows arrive grouped by
                // deposit type first (view's GroupBy preserves first-seen order, does
                // not sort), matching the report's visual grouping.
                var orderByClause = BuildOrderByClause(request.OrderBy);

                var parameters = new DynamicParameters();
                parameters.Add("@SqlFilterExp", sqlFilterExp.ToString() + orderByClause, DbType.String, size: -1);

                var connectionString = _context.Database.GetConnectionString();
                using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();

                var rows = await connection.QueryAsync<SavingIssueRowDto>(
                    "sp_5_43_GetSavingIssue",
                    parameters,
                    commandType: CommandType.StoredProcedure
                );

                var resultList = rows.AsList();

                string? depositTypeName = null;
                string? collectorName = null;
                string? memberGroupName = null;

                if (request.DepositTypeId.HasValue && request.DepositTypeId.Value > 0)
                {
                    depositTypeName = await GetDepositTypeNameAsync(request.DepositTypeId.Value);
                }
                if (request.CollectorId.HasValue && request.CollectorId.Value > 0)
                {
                    collectorName = await GetCollectorNameAsync(request.CollectorId.Value);
                }
                if (request.MemberGroupId.HasValue && request.MemberGroupId.Value > 0)
                {
                    memberGroupName = await GetMemberGroupNameAsync(request.MemberGroupId.Value);
                }

                var branchNames = await GetBranchNamesByIdsAsync(request.BranchIds);

                return new SavingIssueData
                {
                    Rows = resultList,
                    TotalRecords = resultList.Count,
                    TotalOpeningBalance = resultList.Sum(r => r.OpeningBalance ?? 0),
                    FromDateBs = request.FromDateBs,
                    ToDateBs = request.ToDateBs,
                    BranchNames = branchNames,
                    OrderBy = request.OrderBy,
                    ReportMode = request.ReportMode,
                    DepositTypeName = depositTypeName,
                    CollectorName = collectorName,
                    MemberGroupName = memberGroupName,
                    TotalAccounts = resultList.Count
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetReportDataAsync for Saving Issue Report");
                throw;
            }
        }

        /// <summary>
        /// Resolves comma-separated UsmOfficeId values into comma-separated office names.
        /// Same pattern used for DataEditedReport and SavingAccountDeleted.
        /// </summary>
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
                "Member Id" => " ORDER BY d.DepositTypeName, substring(m.MemberId, 1,(len(m.MemberId)-charindex('-', m.MemberId))-1), m.MemberId",
                "Member Name" => " ORDER BY d.DepositTypeName, Name",
                "Account No" => " ORDER BY d.DepositTypeName, substring(a.AccountNo, 1,(len(a.AccountNo)-charindex('-', a.AccountNo))-1), a.AccountNo",
                "A‎/‎C Open Date" => " ORDER BY d.DepositTypeName, a.AccountOpenOnBs",
                "Interest Rate" => " ORDER BY d.DepositTypeName, a.InterestRate DESC",
                "Sms Category" => " ORDER BY d.DepositTypeName, a.SycSmsCategoryId",
                _ => " ORDER BY d.DepositTypeName, substring(m.MemberId, 1,(len(m.MemberId)-charindex('-', m.MemberId))-1), m.MemberId"
            };
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

        private async Task<string?> GetCollectorNameAsync(long collectorId)
        {
            try
            {
                const string query = @"
                    SELECT CollectorFullName 
                    FROM HurCollector 
                    WHERE HurCollectorId = @CollectorId AND IsActive = 1";

                var connectionString = _context.Database.GetConnectionString();
                using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();

                return await connection.QueryFirstOrDefaultAsync<string>(
                    query,
                    new { CollectorId = collectorId }
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting collector name for ID: {CollectorId}", collectorId);
                return null;
            }
        }

        private async Task<string?> GetMemberGroupNameAsync(long memberGroupId)
        {
            try
            {
                const string query = @"
                    SELECT Name 
                    FROM SycMemberGroup 
                    WHERE SycMemberGroupId = @MemberGroupId AND IsActive = 1";

                var connectionString = _context.Database.GetConnectionString();
                using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();

                return await connection.QueryFirstOrDefaultAsync<string>(
                    query,
                    new { MemberGroupId = memberGroupId }
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting member group name for ID: {MemberGroupId}", memberGroupId);
                return null;
            }
        }
    }
}