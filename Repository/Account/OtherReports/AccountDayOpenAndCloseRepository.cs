//// Repositories/Implementations/AccountOperation/AccountDayOpenAndCloseRepository.cs
//using Dapper;
//using Microsoft.Data.SqlClient;
//using Microsoft.EntityFrameworkCore;
//using NexgenCosysReport.DbContext;
//using NexgenCosysReport.Dtos.RequestDtos.Account.OtherReports;
//using NexgenCosysReport.Inteface.ServiceInterface.Account.OtherReports;
//using NexgenCosysReport.Inteface.ServiceInterface.Common;
//using System.Data;
//using System.Text;

//namespace NexgenCosysReport.Repository.AccountOperation.OthersReport
//{
//    public class AccountDayOpenAndCloseRepository : IAccountDayOpenAndClose
//    {
//        private readonly AppDbContext _context;
//        private readonly IDateConverterService _dateConverter;
//        private readonly ILogger<AccountDayOpenAndCloseRepository> _logger;

//        public AccountDayOpenAndCloseRepository(
//            AppDbContext context,
//            IDateConverterService dateConverter,
//            ILogger<AccountDayOpenAndCloseRepository> logger)
//        {
//            _context = context;
//            _dateConverter = dateConverter;
//            _logger = logger;
//        }

//        // --------------------------------------------------------------
//        // @SqlFilterExp
//        // Appended inside SP as: WHERE 1=1 + @SqlFilterExp
//        // Uses c.OpenedDateOn, c.UsmOfficeId, c.CreatedBy (aliased "c")
//        // Also carries the ORDER BY — legacy SP has a single-param
//        // signature, same pattern as TellerToTellerCashTransfer.
//        // --------------------------------------------------------------
//        private async Task<string> BuildSqlFilterExp(AccountDayOpenAndCloseRequestDto request)
//        {
//            var filter = new StringBuilder();

//            if (!string.IsNullOrEmpty(request.FromDateBs) && !string.IsNullOrEmpty(request.ToDateBs)
//                && request.FromDateBs != "-1" && request.ToDateBs != "-1")
//            {
//                var fromDateAd = await _dateConverter.NepaliToEnglishAsync(request.FromDateBs);
//                var toDateAd = await _dateConverter.NepaliToEnglishAsync(request.ToDateBs);

//                // ISO format avoids SQL Server regional/language ambiguity for string->datetime literals
//                var fromDateStr = fromDateAd.ToString("yyyy-MM-dd");
//                var toDateStr = toDateAd.ToString("yyyy-MM-dd");

//                filter.Append(" And c.OpenedDateOn between '")
//                      .Append(fromDateStr).Append("' And '")
//                      .Append(toDateStr).Append("' ");
//            }

//            if (!string.IsNullOrEmpty(request.OfficeId) &&
//                request.OfficeId != "-1" &&
//                request.OfficeId != "string" &&
//                long.TryParse(request.OfficeId, out var officeId))
//            {
//                filter.Append(" And c.UsmOfficeId = ").Append(officeId);
//            }

//            if (!string.IsNullOrEmpty(request.UserId) &&
//                request.UserId != "-1" &&
//                request.UserId != "string" &&
//                long.TryParse(request.UserId, out var userId))
//            {
//                filter.Append(" And c.CreatedBy = ").Append(userId);
//            }

//            filter.Append(BuildSqlOrderBy(request));

//            return filter.ToString();
//        }

//        // --------------------------------------------------------------
//        // ORDER BY — column names match SP's final SELECT aliases
//        // --------------------------------------------------------------
//        private static string BuildSqlOrderBy(AccountDayOpenAndCloseRequestDto request)
//        {
//            if (string.IsNullOrEmpty(request.OrderBy) ||
//                request.OrderBy == "-1" ||
//                request.OrderBy == "string")
//            {
//                return " Order By OpenedDateOnBs"; // default — matches legacy BLL
//            }

//            return request.OrderBy.Trim().ToLower() switch
//            {
//                "branch name" => " Order By OfficeName",
//                "opened date" => " Order By OpenedDateOnBs",
//                "opened by" => " Order By OpenedBy",
//                "opened on" => " Order By OpenedOn",
//                "closed by" => " Order By ClosedBy",
//                "closed on" => " Order By ClosedOn",
//                "status" => " Order By Status",
//                _ => " Order By OpenedDateOnBs"
//            };
//        }

//        public async Task<AccountDayOpenAndCloseData> GetReportDataAsync(AccountDayOpenAndCloseRequestDto request)
//        {
//            try
//            {
//                var sqlFilterExp = await BuildSqlFilterExp(request);

//                var connectionString = _context.Database.GetConnectionString();
//                using var connection = new SqlConnection(connectionString);
//                await connection.OpenAsync();

//                // ---- SP only declares @SqlFilterExp ----
//                var parameters = new DynamicParameters();
//                parameters.Add("@SqlFilterExp", sqlFilterExp, DbType.String, size: -1);

//                var rows = await connection.QueryAsync<AccountDayOpenAndCloseRowDto>(
//                    "sp_6_56_GetAccountDayOpenAndClose",
//                    parameters,
//                    commandType: CommandType.StoredProcedure,
//                    commandTimeout: 120
//                );

//                var resultList = rows.AsList();

//                var branchNames = resultList
//                    .Select(r => r.OfficeName)
//                    .Where(n => !string.IsNullOrWhiteSpace(n))
//                    .Distinct()
//                    .ToList();

//                return new AccountDayOpenAndCloseData
//                {
//                    Rows = resultList,
//                    TotalRecords = resultList.Count,
//                    FromDateBs = request.FromDateBs,
//                    ToDateBs = request.ToDateBs,
//                    BranchNames = branchNames.Count > 0 ? string.Join(", ", branchNames) : "All Branches",
//                    OrderBy = request.OrderBy
//                };
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, "Error in GetReportDataAsync");
//                throw;
//            }
//        }
//    }
//}



// Repositories/Implementations/AccountOperation/AccountDayOpenAndCloseRepository.cs
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using NexgenCosysReport.DbContext;
using NexgenCosysReport.Dtos.RequestDtos.Account.OtherReports;
using NexgenCosysReport.Inteface.ServiceInterface.Account.OtherReports;
using NexgenCosysReport.Inteface.ServiceInterface.Common;
using System.Data;
using System.Text;

namespace NexgenCosysReport.Repository.AccountOperation.OthersReport
{
    public class AccountDayOpenAndCloseRepository : IAccountDayOpenAndClose
    {
        private readonly AppDbContext _context;
        private readonly IDateConverterService _dateConverter;
        private readonly ILogger<AccountDayOpenAndCloseRepository> _logger;

        public AccountDayOpenAndCloseRepository(
            AppDbContext context,
            IDateConverterService dateConverter,
            ILogger<AccountDayOpenAndCloseRepository> logger)
        {
            _context = context;
            _dateConverter = dateConverter;
            _logger = logger;
        }

        private async Task<string> BuildSqlFilterExp(AccountDayOpenAndCloseRequestDto request)
        {
            var filter = new StringBuilder();

            if (!string.IsNullOrEmpty(request.FromDateBs) && !string.IsNullOrEmpty(request.ToDateBs)
                && request.FromDateBs != "-1" && request.ToDateBs != "-1")
            {
                var fromDateAd = await _dateConverter.NepaliToEnglishAsync(request.FromDateBs);
                var toDateAd = await _dateConverter.NepaliToEnglishAsync(request.ToDateBs);

                var fromDateStr = fromDateAd.ToString("yyyy-MM-dd");
                var toDateStr = toDateAd.ToString("yyyy-MM-dd");

                filter.Append(" And c.OpenedDateOn between '")
                      .Append(fromDateStr).Append("' And '")
                      .Append(toDateStr).Append("' ");
            }

            if (!string.IsNullOrEmpty(request.branchId) &&
                request.branchId != "-1" &&
                request.branchId != "string" &&
                long.TryParse(request.branchId, out var officeId))
            {
                filter.Append(" And c.UsmOfficeId = ").Append(officeId);
            }

            if (!string.IsNullOrEmpty(request.UserId) &&
                request.UserId != "-1" &&
                request.UserId != "string" &&
                long.TryParse(request.UserId, out var userId))
            {
                filter.Append(" And c.CreatedBy = ").Append(userId);
            }

            filter.Append(BuildSqlOrderBy(request));

            return filter.ToString();
        }

        // Every branch always leads with "Order By OfficeName" so rows from the same
        // branch arrive contiguous — required for the view's GroupBy (which preserves
        // first-seen order, does not sort) to group correctly by branch, matching the
        // report's visual grouping.
        private static string BuildSqlOrderBy(AccountDayOpenAndCloseRequestDto request)
        {
            if (string.IsNullOrEmpty(request.OrderBy) ||
                request.OrderBy == "-1" ||
                request.OrderBy == "string")
            {
                return " Order By OfficeName, OpenedDateOnBs";
            }

            return request.OrderBy.Trim().ToLower() switch
            {
                "branch name" => " Order By OfficeName",
                "opened date" => " Order By OfficeName, OpenedDateOnBs",
                "opened by" => " Order By OfficeName, OpenedBy",
                "opened on" => " Order By OfficeName, OpenedOn",
                "closed by" => " Order By OfficeName, ClosedBy",
                "closed on" => " Order By OfficeName, ClosedOn",
                "status" => " Order By OfficeName, Status",
                _ => " Order By OfficeName, OpenedDateOnBs"
            };
        }

        public async Task<AccountDayOpenAndCloseData> GetReportDataAsync(AccountDayOpenAndCloseRequestDto request)
        {
            try
            {
                var sqlFilterExp = await BuildSqlFilterExp(request);

                var connectionString = _context.Database.GetConnectionString();
                using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();

                var parameters = new DynamicParameters();
                parameters.Add("@SqlFilterExp", sqlFilterExp, DbType.String, size: -1);

                var rows = await connection.QueryAsync<AccountDayOpenAndCloseRowDto>(
                    "sp_6_56_GetAccountDayOpenAndClose",
                    parameters,
                    commandType: CommandType.StoredProcedure,
                    commandTimeout: 120
                );

                var resultList = rows.AsList();

                var branchNames = resultList
                    .Select(r => r.OfficeName)
                    .Where(n => !string.IsNullOrWhiteSpace(n))
                    .Distinct()
                    .ToList();

                return new AccountDayOpenAndCloseData
                {
                    Rows = resultList,
                    TotalRecords = resultList.Count,
                    FromDateBs = request.FromDateBs,
                    ToDateBs = request.ToDateBs,
                    BranchNames = branchNames.Count > 0 ? string.Join(", ", branchNames) : "All Branches",
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