//// Repository/Loan/LoanAnalysisReport/LoanDetailsRepository.cs
//using Dapper;
//using Microsoft.Data.SqlClient;
//using Microsoft.EntityFrameworkCore;
//using NexgenCosysReport.DbContext;
//using NexgenCosysReport.Dtos.RequestDtos.Loan.LoanAnalysisReport;
//using NexgenCosysReport.Inteface.ServiceInterface.Common;
//using NexgenCosysReport.Inteface.ServiceInterface.Loan.LoanAnalysisReport;
//using System.Data;
//using System.Text;

//namespace NexgenCosysReport.Repository.Loan.LoanAnalysisReport
//{
//    public class LoanDetailsRepository : ILoanDetailsRepository
//    {
//        private readonly AppDbContext _context;
//        private readonly IDateConverterService _dateConverter;
//        private readonly ILogger<LoanDetailsRepository> _logger;

//        public LoanDetailsRepository(
//            AppDbContext context,
//            IDateConverterService dateConverter,
//            ILogger<LoanDetailsRepository> logger)
//        {
//            _context = context;
//            _dateConverter = dateConverter;
//            _logger = logger;
//        }

//        private static string SanitizeBranchIds(string? branchIds)
//        {
//            if (string.IsNullOrWhiteSpace(branchIds) || branchIds == "-1" || branchIds == "string")
//                return "-1";

//            var validIds = branchIds
//                .Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
//                .Where(id => long.TryParse(id, out _));

//            var joined = string.Join(",", validIds);
//            return string.IsNullOrEmpty(joined) ? "-1" : joined;
//        }

//        private static string BuildSqlOrderBy(string? orderBy)
//        {
//            if (string.IsNullOrEmpty(orderBy) || orderBy == "-1")
//                return string.Empty;

//            return orderBy.Trim() switch
//            {
//                "MemberId" => " ORDER BY SUBSTRING(MemberId, 1, LEN(MemberId) - CHARINDEX('-', MemberId) - 1), MemberId",
//                "FullName" => " order by FullName",
//                "LoanType" => " order by LoanTypeName",
//                "LoanAcAmount" => " ORDER BY SUBSTRING(LoanAccountNo, 1, LEN(LoanAccountNo) - CHARINDEX('-', LoanAccountNo) - 1), LoanAccountNo",
//                _ => string.Empty
//            };
//        }

//        public async Task<LoanDetailsData> GetReportDataAsync(LoanDetailsRequestDto request)
//        {
//            try
//            {
//                if (string.IsNullOrWhiteSpace(request.FromDateBs) || string.IsNullOrWhiteSpace(request.ToDateBs)
//                    || request.FromDateBs == "-1" || request.ToDateBs == "-1")
//                {
//                    throw new ArgumentException("From Date and To Date are required.");
//                }

//                var branchIds = SanitizeBranchIds(request.BranchIds);

//                var connectionString = _context.Database.GetConnectionString();
//                using var connection = new SqlConnection(connectionString);
//                await connection.OpenAsync();

//                var fromDateAd = await _dateConverter.NepaliToEnglishAsync(request.FromDateBs);
//                var toDateAd = await _dateConverter.NepaliToEnglishAsync(request.ToDateBs);

//                var fromDateStr = fromDateAd.ToString("yyyy-MM-dd");
//                var toDateStr = toDateAd.ToString("yyyy-MM-dd");

//                var sqlFilterExp = new StringBuilder();

//                if (!string.IsNullOrEmpty(request.LoanTypeId) && request.LoanTypeId != "-1")
//                {
//                    sqlFilterExp.Append(" And Ltm.LmtLoanTypeMasterId = ").Append(request.LoanTypeId);
//                }

//                if (branchIds != "-1")
//                {
//                    sqlFilterExp.Append(" And LS.UsmOfficeId in(").Append(branchIds).Append(")");
//                }

//                if (!string.IsNullOrEmpty(request.CollectorId) && request.CollectorId != "-1")
//                {
//                    sqlFilterExp.Append(" and LS.HurCollectorId = ").Append(request.CollectorId);
//                }

//                if (!string.IsNullOrEmpty(request.CollectionCenterId) && request.CollectionCenterId != "-1")
//                {
//                    sqlFilterExp.Append(" And LS.SycCollectionCenterId in(").Append(request.CollectionCenterId).Append(")");
//                }

//                var status = request.Status;
//                if (!string.IsNullOrEmpty(status) && status != "-1")
//                {
//                    if (status == "3" || status == "1")
//                    {
//                        sqlFilterExp.Append(" AND Ls.LmtLoanStatusId = ").Append(status);
//                    }
//                }

//                var transactionDate = string.Empty;
//                if (!string.IsNullOrEmpty(fromDateStr) && !string.IsNullOrEmpty(toDateStr))
//                {
//                    transactionDate = $" And TransactionOn >= '{fromDateStr}' and TransactionOn <= '{toDateStr}'";
//                }

//                var sqlOrderExp = BuildSqlOrderBy(request.OrderBy);
//                if (status == "4")
//                {
//                    sqlOrderExp = " Where BalanceAmount > 0 " + sqlOrderExp;
//                }

//                var parameters = new DynamicParameters();
//                parameters.Add("@SqlFilterExp", sqlFilterExp.ToString(), DbType.String, size: -1);
//                parameters.Add("@TransactionDate", transactionDate, DbType.String, size: -1);
//                parameters.Add("@SqlOrderExp", sqlOrderExp, DbType.String, size: -1);
//                parameters.Add("@SqlOrderExpFrom", fromDateStr, DbType.String, size: -1);
//                parameters.Add("@SqlOrderExpTill", toDateStr, DbType.String, size: -1);

//                var rows = (await connection.QueryAsync<LoanDetailsRowDto>(
//                    "sp_7_16_LoanDetailsReport",
//                    parameters,
//                    commandType: CommandType.StoredProcedure,
//                    commandTimeout: 300
//                )).AsList();

//                // --------------------------------------------------------------
//                // These four lookups are header display labels (derived from the
//                // request's filter ids, e.g. "Branch Name: Kathmandu Branch,
//                // Damak Branch, ..."), not row data - the SP has no way to
//                // return them since they aren't part of any row. Every actual
//                // row field (MemberId, FullName, LoanAccountNo, LoanTypeName,
//                // PaymentMode, DisburseAmount, RepaidTill, Repaid,
//                // BalanceAmount, CollectionCenterName) comes straight from the
//                // SP's own result set with no table fallback.
//                // --------------------------------------------------------------
//                string branchName = "All";
//                if (branchIds != "-1")
//                {
//                    var names = await connection.QueryAsync<string>(
//                        $"SELECT OfficeName FROM UsmOffice WHERE UsmOfficeId IN ({branchIds})");
//                    var nameList = names.Where(n => !string.IsNullOrWhiteSpace(n)).ToList();
//                    branchName = nameList.Count > 0 ? string.Join(", ", nameList) : "All";
//                }

//                string? loanTypeName = null;
//                if (!string.IsNullOrEmpty(request.LoanTypeId) && request.LoanTypeId != "-1")
//                {
//                    loanTypeName = await connection.QueryFirstOrDefaultAsync<string>(
//                        "SELECT LoanTypeName FROM LmtLoanTypeMaster WHERE LmtLoanTypeMasterId = @Id",
//                        new { Id = request.LoanTypeId });
//                }

//                string? collectionCenterName = null;
//                if (!string.IsNullOrEmpty(request.CollectionCenterId) && request.CollectionCenterId != "-1")
//                {
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

//                var statusName = status switch
//                {
//                    "1" => "Open",
//                    "3" => "Close",
//                    "4" => "With Balance",
//                    _ => "All"
//                };

//                return new LoanDetailsData
//                {
//                    Rows = rows,
//                    TotalRecords = rows.Count,
//                    TotalDisburseAmount = rows.Sum(r => r.DisburseAmount ?? 0),
//                    TotalRepaidTill = rows.Sum(r => r.RepaidTill ?? 0),
//                    TotalRepaid = rows.Sum(r => r.Repaid ?? 0),
//                    TotalBalanceAmount = rows.Sum(r => r.BalanceAmount ?? 0),
//                    FromDateBs = request.FromDateBs,
//                    ToDateBs = request.ToDateBs,
//                    BranchName = branchName,
//                    LoanTypeName = loanTypeName,
//                    StatusName = statusName,
//                    CollectionCenterName = collectionCenterName,
//                    CollectorName = collectorName,
//                    OrderBy = request.OrderBy,
//                    ReportType = request.ReportType
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


using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using NexgenCosysReport.DbContext;
using NexgenCosysReport.Dtos.RequestDtos.Loan.LoanAnalysisReport;
using NexgenCosysReport.Inteface.ServiceInterface.Common;
using NexgenCosysReport.Inteface.ServiceInterface.Loan.LoanAnalysisReport;
using System.Data;
using System.Text;

namespace NexgenCosysReport.Repository.Loan.LoanAnalysisReport
{
    public class LoanDetailsRepository : ILoanDetailsRepository
    {
        private readonly AppDbContext _context;
        private readonly IDateConverterService _dateConverter;
        private readonly ILogger<LoanDetailsRepository> _logger;

        public LoanDetailsRepository(
            AppDbContext context,
            IDateConverterService dateConverter,
            ILogger<LoanDetailsRepository> logger)
        {
            _context = context;
            _dateConverter = dateConverter;
            _logger = logger;
        }

        // "1, 2,3" -> [1,2,3]; ignores "-1", "string" and non-numeric values
        private static List<long> ParseIds(string? csv)
        {
            if (string.IsNullOrWhiteSpace(csv))
                return new List<long>();

            return csv
                .Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(s => long.TryParse(s, out var id) ? id : 0)
                .Where(id => id > 0)
                .Distinct()
                .ToList();
        }

        // Same mapping as the webform's ddlOrderBy
        private static string BuildSqlOrderBy(string? orderBy)
        {
            if (string.IsNullOrEmpty(orderBy) || orderBy == "-1")
                return string.Empty;

            return orderBy.Trim() switch
            {
                "MemberId" => " ORDER BY SUBSTRING(MemberId, 1, LEN(MemberId) - CHARINDEX('-', MemberId) - 1), MemberId",
                "FullName" => " order by FullName",
                "LoanType" => " order by LoanTypeName",
                "LoanAcAmount" => " ORDER BY SUBSTRING(LoanAccountNo, 1, LEN(LoanAccountNo) - CHARINDEX('-', LoanAccountNo) - 1), LoanAccountNo",
                _ => string.Empty
            };
        }

        // Webform radio rbtnReportType: "D" = Detail (default), "S" = Summary.
        // It is not sent to the SP; it only drives the report's visible state.
        private static string NormalizeReportType(string? reportType) =>
            reportType?.Trim().ToUpperInvariant() is "S" or "SUMMARY" ? "S" : "D";

        public async Task<LoanDetailsData> GetReportDataAsync(LoanDetailsRequestDto request, long userId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.FromDateBs) || string.IsNullOrWhiteSpace(request.ToDateBs)
                    || request.FromDateBs == "-1" || request.ToDateBs == "-1")
                    throw new ArgumentException("From Date and To Date are required.");

                var reportType = NormalizeReportType(request.ReportType);

                var connectionString = _context.Database.GetConnectionString();
                using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();

                // 1. Offices: webform checkbox list = CUsmOffice.GetByUsmUserId(userId)
                var allowedOfficeIds = (await connection.QueryAsync<long>(
                    "SELECT DISTINCT UsmOfficeId FROM UsmRelationUserToOffice WHERE UsmUserId = @UserId",
                    new { UserId = userId })).ToList();

                if (allowedOfficeIds.Count == 0)
                    throw new UnauthorizedAccessException("User has no office access.");

                var requestedOfficeIds = ParseIds(request.BranchIds);
                if (requestedOfficeIds.Except(allowedOfficeIds).Any())
                    throw new UnauthorizedAccessException("You do not have access to one or more selected branches.");

                // Nothing sent = webform "Select All" = all of the user's offices
                var officeIds = requestedOfficeIds.Count > 0 ? requestedOfficeIds : allowedOfficeIds;

                // 2. Dates
                var fromDateStr = (await _dateConverter.NepaliToEnglishAsync(request.FromDateBs)).ToString("yyyy-MM-dd");
                var toDateStr = (await _dateConverter.NepaliToEnglishAsync(request.ToDateBs)).ToString("yyyy-MM-dd");

                // 3. Filters, in the same order as CLmtLoanIssueReports.GetLoanDetails
                var sqlFilterExp = new StringBuilder();

                string? loanTypeName = null;
                if (long.TryParse(request.LoanTypeId, out var loanTypeId) && loanTypeId > 0)
                {
                    sqlFilterExp.Append(" And Ltm.LmtLoanTypeMasterId =  ").Append(loanTypeId);

                    loanTypeName = await connection.QueryFirstOrDefaultAsync<string>(
                        "SELECT LoanTypeName FROM LmtLoanTypeMaster WHERE LmtLoanTypeMasterId = @Id",
                        new { Id = loanTypeId });
                }

                sqlFilterExp.Append(" And LS.UsmOfficeId in(").Append(string.Join(",", officeIds)).Append(")");

                string? collectorName = null;
                if (long.TryParse(request.CollectorId, out var collectorId) && collectorId > 0)
                {
                    sqlFilterExp.Append(" and LS.HurCollectorId = ").Append(collectorId);

                    collectorName = await connection.QueryFirstOrDefaultAsync<string>(
                        "SELECT CollectorFullName FROM HurCollector WHERE HurCollectorId = @Id",
                        new { Id = collectorId });
                }

                string? collectionCenterName = null;
                var centerIds = ParseIds(request.CollectionCenterId);
                if (centerIds.Count > 0)
                {
                    sqlFilterExp.Append(" And LS.SycCollectionCenterId in(").Append(string.Join(",", centerIds)).Append(")");

                    var centerNames = await connection.QueryAsync<string>(
                        "SELECT CollectionCenterName FROM SycCollectionCenter WHERE SycCollectionCenterId IN @Ids",
                        new { Ids = centerIds });
                    collectionCenterName = string.Join(", ", centerNames.Where(n => !string.IsNullOrWhiteSpace(n)));
                }

                // Status: "1" Open, "3" Close, "4" With balance, anything else = All
                var status = request.Status?.Trim();
                if (status == "1" || status == "3")
                    sqlFilterExp.Append(" AND Ls.LmtLoanStatusId = ").Append(status);

                // 4. Transaction date window and order (status 4 puts the WHERE before the ORDER BY)
                var transactionDate = $" And TransactionOn >= '{fromDateStr}' and TransactionOn <= '{toDateStr}'";

                var sqlOrderExp = BuildSqlOrderBy(request.OrderBy);
                if (status == "4")
                    sqlOrderExp = " Where BalanceAmount > 0 " + sqlOrderExp;

                _logger.LogInformation("SP sp_7_16_LoanDetailsReport | exp={Exp} | order={Order}",
                    sqlFilterExp, sqlOrderExp);

                var parameters = new DynamicParameters();
                parameters.Add("@SqlFilterExp", sqlFilterExp.ToString(), DbType.String, size: -1);
                parameters.Add("@TransactionDate", transactionDate, DbType.String, size: -1);
                parameters.Add("@SqlOrderExp", sqlOrderExp, DbType.String, size: -1);
                parameters.Add("@SqlOrderExpFrom", fromDateStr, DbType.String, size: -1);
                parameters.Add("@SqlOrderExpTill", toDateStr, DbType.String, size: -1);

                var rows = (await connection.QueryAsync<LoanDetailsRowDto>(
                    "sp_7_16_LoanDetailsReport",
                    parameters,
                    commandType: CommandType.StoredProcedure,
                    commandTimeout: 300
                )).AsList();

                // 5. Header branch names
                var names = await connection.QueryAsync<string>(
                    "SELECT OfficeName FROM UsmOffice WHERE UsmOfficeId IN @Ids",
                    new { Ids = officeIds });
                var nameList = names.Where(n => !string.IsNullOrWhiteSpace(n)).ToList();
                var branchName = nameList.Count > 0 ? string.Join(", ", nameList) : "All";

                var statusName = status switch
                {
                    "1" => "Open",
                    "3" => "Close",
                    "4" => "With Balance",
                    _ => "All"
                };

                return new LoanDetailsData
                {
                    Rows = rows,
                    TotalRecords = rows.Count,
                    TotalDisburseAmount = rows.Sum(r => r.DisburseAmount ?? 0),
                    TotalRepaidTill = rows.Sum(r => r.RepaidTill ?? 0),
                    TotalRepaid = rows.Sum(r => r.Repaid ?? 0),
                    TotalBalanceAmount = rows.Sum(r => r.BalanceAmount ?? 0),
                    FromDateBs = request.FromDateBs,
                    ToDateBs = request.ToDateBs,
                    BranchName = branchName,
                    LoanTypeName = loanTypeName,
                    StatusName = statusName,
                    CollectionCenterName = collectionCenterName,
                    CollectorName = collectorName,
                    OrderBy = request.OrderBy,
                    ReportType = reportType
                };
            }
            catch (Exception ex) when (ex is UnauthorizedAccessException || ex is ArgumentException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetReportDataAsync");
                throw;
            }
        }
    }
}