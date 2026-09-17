//// Repository/Loan/OtherReports/LoanDefaulterDueSummaryRepository.cs
//using Dapper;
//using Microsoft.Data.SqlClient;
//using Microsoft.EntityFrameworkCore;
//using NexgenCosysReport.DbContext;
//using NexgenCosysReport.Dtos.RequestDtos.Loan.OtherReports;
//using NexgenCosysReport.Inteface.ServiceInterface.Common;
//using NexgenCosysReport.Inteface.ServiceInterface.Loan.OtherReports;
//using System.Data;
//using System.Text;

//namespace NexgenCosysReport.Repository.Loan.OtherReports
//{
//    public class LoanDefaulterDueSummaryRepository : ILoanDefaulterDueSummaryRepository
//    {
//        private readonly AppDbContext _context;
//        private readonly IDateConverterService _dateConverter;
//        private readonly ILogger<LoanDefaulterDueSummaryRepository> _logger;

//        public LoanDefaulterDueSummaryRepository(
//            AppDbContext context,
//            IDateConverterService dateConverter,
//            ILogger<LoanDefaulterDueSummaryRepository> logger)
//        {
//            _context = context;
//            _dateConverter = dateConverter;
//            _logger = logger;
//        }

//        // --------------------------------------------------------------
//        // @SqlFilterExpOrderby — column names match the SP's final SELECT
//        // Preserves the legacy substring-based natural sort for
//        // MemberId / LoanAccountNo (strips a trailing "-N" suffix before
//        // sorting) exactly as in the BLL.
//        // --------------------------------------------------------------
//        private static string BuildSqlOrderBy(LoanDefaulterDueSummaryRequestDto request)
//        {
//            if (string.IsNullOrEmpty(request.OrderBy) || request.OrderBy == "-1")
//                return string.Empty;

//            return request.OrderBy.Trim() switch
//            {
//                "MemberId" => " substring(MemberId, 1,(len(MemberId)-charindex('-', MemberId))-1), MemberId ",
//                "FullName" => "FullName",
//                "LoanAccountNo" => " substring(LoanAccountNo, 1,(len(LoanAccountNo)-charindex('-', LoanAccountNo))-1), LoanAccountNo ",
//                "LoanTypeName" => " LoanTypeName",
//                "PrincipleAmount" => "  PrincipleAmount DESC",
//                "InterestAmount" => " InterestAmount",
//                "InstallmentAmount" => " InstallmentAmount",
//                "DateOnBS" => " DateOnBS",
//                _ => string.Empty
//            };
//        }

//        // --------------------------------------------------------------
//        // Guards against injection through the comma-separated branch id
//        // list, same pattern used across the other reports.
//        // --------------------------------------------------------------
//        private static string SanitizeBranchIds(string? branchIds)
//        {
//            if (string.IsNullOrWhiteSpace(branchIds) || branchIds == "-1" || branchIds == "string")
//                return string.Empty;

//            var validIds = branchIds
//                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
//                .Where(id => long.TryParse(id, out _));

//            return string.Join(",", validIds);
//        }

//        public async Task<LoanDefaulterDueSummaryData> GetReportDataAsync(LoanDefaulterDueSummaryRequestDto request)
//        {
//            try
//            {
//                if (string.IsNullOrEmpty(request.TillDate) || request.TillDate == "-1")
//                {
//                    throw new ArgumentException("TillDate is required.");
//                }

//                var branchIds = SanitizeBranchIds(request.BranchIds);

//                var connectionString = _context.Database.GetConnectionString();
//                using var connection = new SqlConnection(connectionString);
//                await connection.OpenAsync();

//                // Convert BS till date to AD
//                var tillDateAd = await _dateConverter.NepaliToEnglishAsync(request.TillDate);
//                var tillDateStr = tillDateAd.ToString("yyyy-MM-dd");

//                var sqlFilterExp = new StringBuilder();

//                // --------------------------------------------------------------
//                // Build filter expression
//                // --------------------------------------------------------------
//                if (!string.IsNullOrEmpty(branchIds))
//                {
//                    sqlFilterExp.Append(" And LS.UsmOfficeId in(").Append(branchIds).Append(")");
//                }

//                if (!string.IsNullOrEmpty(request.CollectionCenterId) && request.CollectionCenterId != "-1")
//                {
//                    sqlFilterExp.Append(" And LS.SycCollectionCenterId in(").Append(request.CollectionCenterId).Append(")");
//                }

//                if (!string.IsNullOrEmpty(request.CollectorId) && request.CollectorId != "-1")
//                {
//                    sqlFilterExp.Append(" and LS.HurCollectorId = ").Append(request.CollectorId);
//                }

//                var sqlFilterExpOrderby = new StringBuilder();
//                var orderByClause = BuildSqlOrderBy(request);
//                if (!string.IsNullOrEmpty(orderByClause))
//                {
//                    sqlFilterExpOrderby.Append(" order by ").Append(orderByClause);
//                }

//                // The SP accepts @sqltiidate as a quoted string literal
//                var sqltiidate = $"'{tillDateStr}'";

//                var parameters = new DynamicParameters();
//                parameters.Add("@SqlFilterExp", sqlFilterExp.ToString(), DbType.String, size: -1);
//                parameters.Add("@SqlFilterExpOrderby", sqlFilterExpOrderby.ToString(), DbType.String, size: -1);
//                parameters.Add("@sqltiidate", sqltiidate, DbType.String, size: -1);

//                // Choose the SP based on report type
//                var spName = request.ReportType == "LDTPR"
//                    ? "sp_7_16_LoanDefaulterDueSummaryTobePaid"
//                    : "sp_7_16_LoanDefaulterDueSummary";

//                var rows = await connection.QueryAsync<LoanDefaulterDueSummaryRowDto>(
//                    spName,
//                    parameters,
//                    commandType: CommandType.StoredProcedure,
//                    commandTimeout: 120
//                );

//                var resultList = rows.AsList();

//                // Get branch names for display
//                string branchName = "All";
//                if (!string.IsNullOrEmpty(branchIds))
//                {
//                    var names = await connection.QueryAsync<string>(
//                        $"SELECT OfficeName FROM UsmOffice WHERE UsmOfficeId IN ({branchIds})");
//                    var nameList = names.Where(n => !string.IsNullOrWhiteSpace(n)).ToList();
//                    branchName = nameList.Count > 0 ? string.Join(", ", nameList) : "All";
//                }

//                // Get collection center name for display
//                string? collectionCenterName = null;
//                if (!string.IsNullOrEmpty(request.CollectionCenterId) && request.CollectionCenterId != "-1")
//                {
//                    collectionCenterName = await connection.QueryFirstOrDefaultAsync<string>(
//                        $"SELECT STRING_AGG(CollectionCenterName, ', ') FROM SycCollectionCenter WHERE SycCollectionCenterId IN ({request.CollectionCenterId})");
//                }

//                // Get collector name for display
//                string? collectorName = null;
//                if (!string.IsNullOrEmpty(request.CollectorId) && request.CollectorId != "-1")
//                {
//                    collectorName = await connection.QueryFirstOrDefaultAsync<string>(
//                        "SELECT CollectorFullName FROM HurCollector WHERE HurCollectorId = @Id",
//                        new { Id = request.CollectorId });
//                }

//                return new LoanDefaulterDueSummaryData
//                {
//                    Rows = resultList,
//                    TotalRecords = resultList.Count,
//                    TotalLoanIssueAmount = resultList.Sum(r => r.LoanIssueAmount ?? 0),
//                    TotalInterest = resultList.Sum(r => r.Interest ?? 0),
//                    TotalPrincipleAmount = resultList.Sum(r => r.PrincipleAmount ?? 0),
//                    TotalInstallmentAmount = resultList.Sum(r => r.InstallamentAmount ?? 0),
//                    TillDate = request.TillDate,
//                    BranchName = branchName,
//                    CollectionCenterName = collectionCenterName,
//                    CollectorName = collectorName,
//                    ReportType = request.ReportType,
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






//// Repository/Loan/OtherReports/LoanDefaulterDueSummaryRepository.cs
//using Dapper;
//using Microsoft.Data.SqlClient;
//using Microsoft.EntityFrameworkCore;
//using NexgenCosysReport.DbContext;
//using NexgenCosysReport.Dtos.RequestDtos.Loan.OtherReports;
//using NexgenCosysReport.Inteface.ServiceInterface.Common;
//using NexgenCosysReport.Inteface.ServiceInterface.Loan.OtherReports;
//using System.Data;
//using System.Text;

//namespace NexgenCosysReport.Repository.Loan.OtherReports
//{
//    public class LoanDefaulterDueSummaryRepository : ILoanDefaulterDueSummaryRepository
//    {
//        private readonly AppDbContext _context;
//        private readonly IDateConverterService _dateConverter;
//        private readonly ILogger<LoanDefaulterDueSummaryRepository> _logger;

//        public LoanDefaulterDueSummaryRepository(
//            AppDbContext context,
//            IDateConverterService dateConverter,
//            ILogger<LoanDefaulterDueSummaryRepository> logger)
//        {
//            _context = context;
//            _dateConverter = dateConverter;
//            _logger = logger;
//        }

//        private static string BuildSqlOrderBy(LoanDefaulterDueSummaryRequestDto request)
//        {
//            if (string.IsNullOrEmpty(request.OrderBy) || request.OrderBy == "-1")
//                return string.Empty;

//            return request.OrderBy.Trim() switch
//            {
//                "MemberId" => " substring(MemberId, 1,(len(MemberId)-charindex('-', MemberId))-1), MemberId ",
//                "FullName" => "FullName",
//                "LoanAccountNo" => " substring(LoanAccountNo, 1,(len(LoanAccountNo)-charindex('-', LoanAccountNo))-1), LoanAccountNo ",
//                "LoanTypeName" => " LoanTypeName",
//                "PrincipleAmount" => "  PrincipleAmount DESC",
//                "InterestAmount" => " InterestAmount",
//                "InstallmentAmount" => " InstallmentAmount",
//                "DateOnBS" => " DateOnBS",
//                _ => string.Empty
//            };
//        }

//        private static string SanitizeBranchIds(string? branchIds)
//        {
//            if (string.IsNullOrWhiteSpace(branchIds) || branchIds == "-1" || branchIds == "string")
//                return string.Empty;

//            var validIds = branchIds
//                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
//                .Where(id => long.TryParse(id, out _));

//            return string.Join(",", validIds);
//        }

//        public async Task<LoanDefaulterDueSummaryData> GetReportDataAsync(LoanDefaulterDueSummaryRequestDto request)
//        {
//            try
//            {
//                if (string.IsNullOrEmpty(request.TillDate) || request.TillDate == "-1")
//                {
//                    throw new ArgumentException("TillDate is required.");
//                }

//                var branchIds = SanitizeBranchIds(request.BranchIds);

//                var connectionString = _context.Database.GetConnectionString();
//                using var connection = new SqlConnection(connectionString);
//                await connection.OpenAsync();

//                var tillDateAd = await _dateConverter.NepaliToEnglishAsync(request.TillDate);
//                var tillDateStr = tillDateAd.ToString("yyyy-MM-dd");

//                var sqlFilterExp = new StringBuilder();

//                if (!string.IsNullOrEmpty(branchIds))
//                {
//                    sqlFilterExp.Append(" And LS.UsmOfficeId in(").Append(branchIds).Append(")");
//                }

//                if (!string.IsNullOrEmpty(request.CollectionCenterId) && request.CollectionCenterId != "-1")
//                {
//                    sqlFilterExp.Append(" And LS.SycCollectionCenterId in(").Append(request.CollectionCenterId).Append(")");
//                }

//                if (!string.IsNullOrEmpty(request.CollectorId) && request.CollectorId != "-1")
//                {
//                    sqlFilterExp.Append(" and LS.HurCollectorId = ").Append(request.CollectorId);
//                }

//                var sqlFilterExpOrderby = new StringBuilder();
//                var orderByClause = BuildSqlOrderBy(request);
//                if (!string.IsNullOrEmpty(orderByClause))
//                {
//                    sqlFilterExpOrderby.Append(" order by ").Append(orderByClause);
//                }

//                // Root-cause fix: sp_7_16_LoanDefaulterDueSummary (LDR) declares only 2
//                // parameters (@SqlFilterExp, @SqlFilterExpOrderby) — @sqltiidate is unique
//                // to sp_7_16_LoanDefaulterDueSummaryTobePaid (LDTPR). Passing 3 parameters
//                // unconditionally to whichever SP was selected caused "too many arguments
//                // specified" whenever the LDR variant was called.
//                var isTobePaid = request.ReportType == "LDTPR";
//                var spName = isTobePaid
//                    ? "sp_7_16_LoanDefaulterDueSummaryTobePaid"
//                    : "sp_7_16_LoanDefaulterDueSummary";

//                var parameters = new DynamicParameters();
//                parameters.Add("@SqlFilterExp", sqlFilterExp.ToString(), DbType.String, size: -1);
//                parameters.Add("@SqlFilterExpOrderby", sqlFilterExpOrderby.ToString(), DbType.String, size: -1);

//                if (isTobePaid)
//                {
//                    // The SP accepts @sqltiidate as a quoted string literal, embedded
//                    // directly into its dynamic SQL.
//                    parameters.Add("@sqltiidate", $"'{tillDateStr}'", DbType.String, size: -1);
//                }

//                var rows = await connection.QueryAsync<LoanDefaulterDueSummaryRowDto>(
//                    spName,
//                    parameters,
//                    commandType: CommandType.StoredProcedure,
//                    commandTimeout: 120
//                );

//                var resultList = rows.AsList();

//                string branchName = "All";
//                if (!string.IsNullOrEmpty(branchIds))
//                {
//                    var branchIdList = branchIds
//                        .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
//                        .Select(id => long.TryParse(id, out var parsed) ? (long?)parsed : null)
//                        .Where(id => id.HasValue)
//                        .Select(id => id!.Value)
//                        .ToList();

//                    if (branchIdList.Count > 0)
//                    {
//                        var names = await connection.QueryAsync<string>(
//                            "SELECT OfficeName FROM UsmOffice WHERE UsmOfficeId IN @Ids",
//                            new { Ids = branchIdList });
//                        var nameList = names.Where(n => !string.IsNullOrWhiteSpace(n)).ToList();
//                        branchName = nameList.Count > 0 ? string.Join(", ", nameList) : "All";
//                    }
//                }

//                string? collectionCenterName = null;
//                if (!string.IsNullOrEmpty(request.CollectionCenterId) && request.CollectionCenterId != "-1")
//                {
//                    // Root cause note: same STRING_AGG compat-level risk as other reports
//                    // in this project. If your DB rejects STRING_AGG, split/join in C#
//                    // the same way ResolveBranchNamesAsync does elsewhere.
//                    collectionCenterName = await connection.QueryFirstOrDefaultAsync<string>(
//                        $"SELECT STRING_AGG(CollectionCenterName, ', ') FROM SycCollectionCenter WHERE SycCollectionCenterId IN ({request.CollectionCenterId})");
//                }

//                string? collectorName = null;
//                if (!string.IsNullOrEmpty(request.CollectorId) && request.CollectorId != "-1")
//                {
//                    collectorName = await connection.QueryFirstOrDefaultAsync<string>(
//                        "SELECT CollectorFullName FROM HurCollector WHERE HurCollectorId = @Id",
//                        new { Id = request.CollectorId });
//                }

//                return new LoanDefaulterDueSummaryData
//                {
//                    Rows = resultList,
//                    TotalRecords = resultList.Count,
//                    TotalLoanIssueAmount = resultList.Sum(r => r.LoanIssueAmount ?? 0),
//                    TotalInterest = resultList.Sum(r => r.Interest ?? 0),
//                    TotalPrincipleAmount = resultList.Sum(r => r.PrincipleAmount ?? 0),
//                    TotalInstallmentAmount = resultList.Sum(r => r.InstallamentAmount ?? 0),
//                    TillDate = request.TillDate,
//                    BranchName = branchName,
//                    CollectionCenterName = collectionCenterName,
//                    CollectorName = collectorName,
//                    ReportType = request.ReportType,
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




// Repository/Loan/OtherReports/LoanDefaulterDueSummaryRepository.cs
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using NexgenCosysReport.DbContext;
using NexgenCosysReport.Dtos.RequestDtos.Loan.OtherReports;
using NexgenCosysReport.Inteface.ServiceInterface.Common;
using NexgenCosysReport.Inteface.ServiceInterface.Loan.OtherReports;
using System.Data;
using System.Text;

namespace NexgenCosysReport.Repository.Loan.OtherReports
{
    public class LoanDefaulterDueSummaryRepository : ILoanDefaulterDueSummaryRepository
    {
        private readonly AppDbContext _context;
        private readonly IDateConverterService _dateConverter;
        private readonly ILogger<LoanDefaulterDueSummaryRepository> _logger;

        // Both SPs join Loan/Schedule/Office tables inside dynamic SQL, and the
        // LDTPR variant additionally calls scalar UDFs (fn_GetLoanInterestTillDate /
        // fn_GetLoanPrincipleDueTillDate) per row — so neither is sargable and both
        // can legitimately run well past ADO.NET's 30s default CommandTimeout on
        // large branches. The legacy WebForms CDataAccessLayer had this configured
        // higher; that setting was lost in the migration. Restore it explicitly
        // here instead of relying on the default.
        private const int ReportCommandTimeoutSeconds = 180;

        public LoanDefaulterDueSummaryRepository(
            AppDbContext context,
            IDateConverterService dateConverter,
            ILogger<LoanDefaulterDueSummaryRepository> logger)
        {
            _context = context;
            _dateConverter = dateConverter;
            _logger = logger;
        }

        public async Task<LoanDefaulterDueSummaryData> GetReportDataAsync(LoanDefaulterDueSummaryRequestDto request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.TillDate) || request.TillDate == "-1")
                {
                    throw new ArgumentException("TillDate is required.");
                }

                var branchIds = SanitizeBranchIds(request.BranchIds);
                var collectionCenterIds = SanitizeCollectionCenterIds(request.CollectionCenterId);

                var connectionString = _context.Database.GetConnectionString();
                using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();

                // --- Convert Nepali date to English ---
                // The SPs embed @sqltiidate as a quoted string literal inside their
                // dynamic SQL, so keep the ISO "yyyy-MM-dd" form for safe comparison
                // against ScheduleDateOn / TransactionOn columns.
                string tillDateStr;
                try
                {
                    var tillDateAd = await _dateConverter.NepaliToEnglishAsync(request.TillDate);
                    tillDateStr = tillDateAd.ToString("yyyy-MM-dd");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Date conversion failed for TillDate: {TillDate}", request.TillDate);
                    tillDateStr = DateTime.Now.ToString("yyyy-MM-dd");
                }

                // --- Build filters ---
                var sqlFilterExp = new StringBuilder();

                if (!string.IsNullOrEmpty(branchIds))
                {
                    sqlFilterExp.Append(" And LS.UsmOfficeId in(").Append(branchIds).Append(")");
                }

                if (!string.IsNullOrEmpty(collectionCenterIds))
                {
                    sqlFilterExp.Append(" And LS.SycCollectionCenterId in(").Append(collectionCenterIds).Append(")");
                }

                if (!string.IsNullOrEmpty(request.CollectorId) && request.CollectorId != "-1")
                {
                    sqlFilterExp.Append(" and LS.HurCollectorId = ").Append(request.CollectorId);
                }

                // --- Order By ---
                var sqlFilterExpOrderby = new StringBuilder();
                var orderByClause = MapOrderBy(request.OrderBy);
                if (!string.IsNullOrEmpty(orderByClause))
                {
                    sqlFilterExpOrderby.Append(" order by ").Append(orderByClause);
                }

                // Root-cause fix: sp_7_16_LoanDefaulterDueSummary (LDR) declares only 2
                // parameters (@SqlFilterExp, @SqlFilterExpOrderby) — @sqltiidate is unique
                // to sp_7_16_LoanDefaulterDueSummaryTobePaid (LDTPR). Passing 3 parameters
                // unconditionally to whichever SP was selected caused "too many arguments
                // specified" whenever the LDR variant was called.
                var isTobePaid = request.ReportType == "LDTPR";
                var spName = isTobePaid
                    ? "sp_7_16_LoanDefaulterDueSummaryTobePaid"
                    : "sp_7_16_LoanDefaulterDueSummary";

                var parameters = new DynamicParameters();
                parameters.Add("@SqlFilterExp", sqlFilterExp.ToString(), DbType.String, size: -1);
                parameters.Add("@SqlFilterExpOrderby", sqlFilterExpOrderby.ToString(), DbType.String, size: -1);

                if (isTobePaid)
                {
                    // The SP accepts @sqltiidate as a quoted string literal, embedded
                    // directly into its dynamic SQL.
                    parameters.Add("@sqltiidate", $"'{tillDateStr}'", DbType.String, size: -1);
                }

                _logger.LogInformation(
                    "LoanDefaulterDueSummary SP params -> SP: {SpName}, SqlFilterExp: {SqlFilterExp}, SqlFilterExpOrderby: {SqlFilterExpOrderby}",
                    spName, sqlFilterExp.ToString(), sqlFilterExpOrderby.ToString());

                List<LoanDefaulterDueSummaryRowDto> resultList;
                try
                {
                    var rows = await connection.QueryAsync<LoanDefaulterDueSummaryRowDto>(
                        spName,
                        parameters,
                        commandType: CommandType.StoredProcedure,
                        commandTimeout: ReportCommandTimeoutSeconds
                    );

                    resultList = rows.AsList();
                }
                catch (SqlException ex) when (ex.Number == -2 || ex.Message.Contains("Timeout"))
                {
                    _logger.LogError(ex,
                        "LoanDefaulterDueSummary SP timed out after {Timeout}s. Filters -> SqlFilterExp: {SqlFilterExp}, SqlFilterExpOrderby: {SqlFilterExpOrderby}",
                        ReportCommandTimeoutSeconds, sqlFilterExp.ToString(), sqlFilterExpOrderby.ToString());
                    throw new TimeoutException(
                        $"The Loan Defaulter Due Summary report timed out after {ReportCommandTimeoutSeconds}s. " +
                        "Try narrowing the branch/date filters, or check that the underlying Loan/Schedule tables " +
                        "have supporting indexes on UsmOfficeId, LmtLoanStatusId, IsPaid, and IsActive.", ex);
                }

                _logger.LogInformation("LoanDefaulterDueSummary returned {Count} rows", resultList.Count);

                // --- Resolve branch names for the report header ---
                string branchName = "All";
                if (!string.IsNullOrEmpty(branchIds))
                {
                    var branchIdList = branchIds
                        .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                        .Select(id => long.TryParse(id, out var parsed) ? (long?)parsed : null)
                        .Where(id => id.HasValue)
                        .Select(id => id!.Value)
                        .ToList();

                    if (branchIdList.Count > 0)
                    {
                        var names = await connection.QueryAsync<string>(
                            "SELECT OfficeName FROM UsmOffice WHERE UsmOfficeId IN @Ids",
                            new { Ids = branchIdList });
                        var nameList = names.Where(n => !string.IsNullOrWhiteSpace(n)).ToList();
                        branchName = nameList.Count > 0 ? string.Join(", ", nameList) : "All";
                    }
                }

                // --- Resolve collection center names ---
                // STRING_AGG requires SQL Server 2017+ (compat level 140) and was
                // rejected on this DB. Same split-in-C# + join pattern as branch names.
                string? collectionCenterName = null;
                if (!string.IsNullOrEmpty(collectionCenterIds))
                {
                    var ccIdList = collectionCenterIds
                        .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                        .Select(id => long.TryParse(id, out var parsed) ? (long?)parsed : null)
                        .Where(id => id.HasValue)
                        .Select(id => id!.Value)
                        .ToList();

                    if (ccIdList.Count > 0)
                    {
                        var ccNames = await connection.QueryAsync<string>(
                            "SELECT CollectionCenterName FROM SycCollectionCenter WHERE SycCollectionCenterId IN @Ids",
                            new { Ids = ccIdList });

                        var ccNameList = ccNames.Where(n => !string.IsNullOrWhiteSpace(n)).ToList();
                        collectionCenterName = ccNameList.Count > 0 ? string.Join(", ", ccNameList) : null;
                    }
                }

                // --- Resolve collector name ---
                string? collectorName = null;
                if (!string.IsNullOrEmpty(request.CollectorId) && request.CollectorId != "-1")
                {
                    collectorName = await connection.QueryFirstOrDefaultAsync<string>(
                        "SELECT CollectorFullName FROM HurCollector WHERE HurCollectorId = @Id",
                        new { Id = request.CollectorId });
                }

                return new LoanDefaulterDueSummaryData
                {
                    Rows = resultList,
                    TotalRecords = resultList.Count,
                    TotalLoanIssueAmount = resultList.Sum(r => r.LoanIssueAmount ?? 0),
                    TotalInterest = resultList.Sum(r => r.Interest ?? 0),
                    TotalPrincipleAmount = resultList.Sum(r => r.PrincipleAmount ?? 0),
                    TotalInstallmentAmount = resultList.Sum(r => r.InstallamentAmount ?? 0),
                    TillDate = request.TillDate,
                    BranchName = branchName,
                    CollectionCenterName = collectionCenterName,
                    CollectorName = collectorName,
                    ReportType = request.ReportType,
                    OrderBy = request.OrderBy
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetReportDataAsync");
                throw;
            }
        }

        // --------------------------------------------------------------
        // @SqlFilterExpOrderby — column names match the SP's final SELECT.
        // Preserves the legacy substring-based natural sort for
        // MemberId / LoanAccountNo (strips a trailing "-N" suffix before
        // sorting) exactly as in the original BLL.
        // --------------------------------------------------------------
        private string MapOrderBy(string orderBy)
        {
            if (string.IsNullOrEmpty(orderBy) || orderBy == "-1")
                return string.Empty;

            return orderBy.Trim() switch
            {
                "MemberId" => " substring(MemberId, 1,(len(MemberId)-charindex('-', MemberId))-1), MemberId ",
                "FullName" => "FullName",
                "LoanAccountNo" => " substring(LoanAccountNo, 1,(len(LoanAccountNo)-charindex('-', LoanAccountNo))-1), LoanAccountNo ",
                "LoanTypeName" => " LoanTypeName",
                "PrincipleAmount" => " PrincipleAmount DESC",
                "InterestAmount" => " InterestAmount",
                "InstallmentAmount" => " InstallmentAmount",
                "DateOnBS" => " DateOnBS",
                _ => string.Empty
            };
        }

        // --------------------------------------------------------------
        // Guards against injection through the comma-separated branch id
        // list, same pattern used across the other reports.
        // --------------------------------------------------------------
        private static string SanitizeBranchIds(string? branchIds)
        {
            if (string.IsNullOrWhiteSpace(branchIds) || branchIds == "-1" || branchIds == "string")
                return string.Empty;

            var validIds = branchIds
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Where(id => long.TryParse(id, out _));

            return string.Join(",", validIds);
        }

        // --------------------------------------------------------------
        // Same guard for the collection center id list, which is also
        // interpolated directly into the dynamic SQL fragment.
        // --------------------------------------------------------------
        private static string SanitizeCollectionCenterIds(string? ccIds)
        {
            if (string.IsNullOrWhiteSpace(ccIds) || ccIds == "-1" || ccIds == "string")
                return string.Empty;

            var validIds = ccIds
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Where(id => long.TryParse(id, out _));

            return string.Join(",", validIds);
        }
    }
}

