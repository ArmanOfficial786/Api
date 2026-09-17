//// Repository/Loan/OtherReports/MaturedLoanRepository.cs
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
//    public class MaturedLoanRepository : IMaturedLoanRepository
//    {
//        private readonly AppDbContext _context;
//        private readonly IDateConverterService _dateConverter;
//        private readonly ILogger<MaturedLoanRepository> _logger;

//        public MaturedLoanRepository(
//            AppDbContext context,
//            IDateConverterService dateConverter,
//            ILogger<MaturedLoanRepository> logger)
//        {
//            _context = context;
//            _dateConverter = dateConverter;
//            _logger = logger;
//        }

//        // --------------------------------------------------------------
//        // @SqlFilterExpOrder — column names match the SP's final SELECT
//        // Preserves the substring-based natural sort for MemberId
//        // (strips a trailing "-N" suffix before sorting) exactly as
//        // in the legacy BLL.
//        // --------------------------------------------------------------
//        private static string BuildSqlOrderBy(MaturedLoanRequestDto request)
//        {
//            if (string.IsNullOrEmpty(request.OrderBy) || request.OrderBy == "-1")
//                return string.Empty;

//            return request.OrderBy.Trim() switch
//            {
//                "MemberId" => " ORDER BY SUBSTRING(MemberId, 1, LEN(MemberId) - CHARINDEX('-', MemberId) - 1), MemberId",
//                "FullName" => " order by FullName",
//                "MaturityDate" => " order by MaturityDate",
//                "LoanAccountNo" => " order by LoanAccountNo",
//                "DisburseAmount" => " order by LoanIssueAmount desc",
//                "DueBalance" => " order by TotalDueAmount desc",
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

//        public async Task<MaturedLoanData> GetReportDataAsync(MaturedLoanRequestDto request)
//        {
//            try
//            {
//                var branchIds = SanitizeBranchIds(request.BranchIds);

//                var connectionString = _context.Database.GetConnectionString();
//                using var connection = new SqlConnection(connectionString);
//                await connection.OpenAsync();

//                var sqlFilterExp = new StringBuilder();
//                var sqlFilterExpBranchId = new StringBuilder();
//                string? memberName = null;
//                string? memberGroupName = null;
//                string sqlTilldate = string.Empty;

//                // --------------------------------------------------------------
//                // Build filter expression matching legacy BLL:
//                // 1. Member Group filter (goes into @SqlFilterExp)
//                // 2. Member + Date range OR Date range only
//                // 3. Branch filter (separate @SqlFilterExpbranchId)
//                // 4. ORDER BY (separate @SqlFilterExpOrder)
//                // 5. @SqlTilldate = Today's AD date (member mode) or ToDate (date mode)
//                // --------------------------------------------------------------
//                if (request.MemberGroupId != -1)
//                {
//                    sqlFilterExp.Append(" AND MR.SycMemberGroupId = ").Append(request.MemberGroupId);

//                    // Get member group name for display
//                    memberGroupName = await connection.QueryFirstOrDefaultAsync<string>(
//                        "SELECT Name FROM SycMemberGroup WHERE SycMemberGroupId = @Id",
//                        new { Id = request.MemberGroupId });
//                }

//                if (!string.IsNullOrWhiteSpace(request.MemberId))
//                {
//                    sqlFilterExp.Append(" And Mr.MemberId = '").Append(request.MemberId.Trim()).Append("'");

//                    // For member mode, @SqlTilldate = today's AD date
//                    sqlTilldate = $"'{DateTime.Now:yyyy-MM-dd}'";

//                    // Get member name for display
//                    var name = await connection.QueryFirstOrDefaultAsync<string>(
//                        @"SELECT FirstName + ' ' +
//                                 CASE WHEN MiddleName = '' THEN '' ELSE MiddleName + ' ' END +
//                                 LastName
//                          FROM MemMemberRegistration 
//                          WHERE MemberId = @MemberId AND IsActive = 1",
//                        new { MemberId = request.MemberId.Trim() });
//                    memberName = name;
//                }
//                else if (!string.IsNullOrEmpty(request.FromDateBs) && !string.IsNullOrEmpty(request.ToDateBs))
//                {
//                    var fromDateAd = await _dateConverter.NepaliToEnglishAsync(request.FromDateBs);
//                    var toDateAd = await _dateConverter.NepaliToEnglishAsync(request.ToDateBs);

//                    var fromDateStr = fromDateAd.ToString("yyyy-MM-dd");
//                    var toDateStr = toDateAd.ToString("yyyy-MM-dd");

//                    sqlFilterExp.Append(" And ls.MaturityOn between '").Append(fromDateStr)
//                                .Append("' And '").Append(toDateStr).Append("'");

//                    // For date mode, @SqlTilldate = ToDate
//                    sqlTilldate = $"'{toDateStr}'";
//                }

//                if (!string.IsNullOrEmpty(branchIds))
//                {
//                    sqlFilterExpBranchId.Append(" And v.UsmOfficeId in (").Append(branchIds).Append(")");
//                }

//                var sqlFilterExpOrder = BuildSqlOrderBy(request);

//                var parameters = new DynamicParameters();
//                parameters.Add("@SqlFilterExp", sqlFilterExp.ToString(), DbType.String, size: -1);
//                parameters.Add("@SqlFilterExpbranchId", sqlFilterExpBranchId.ToString(), DbType.String, size: -1);
//                parameters.Add("@SqlFilterExpOrder", sqlFilterExpOrder, DbType.String, size: -1);
//                parameters.Add("@SqlTilldate", sqlTilldate, DbType.String, size: -1);

//                var rows = await connection.QueryAsync<MaturedLoanRowDto>(
//                    "sp_7_16_MaturedLoanReport",
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

//                return new MaturedLoanData
//                {
//                    Rows = resultList,
//                    TotalRecords = resultList.Count,
//                    TotalLoanIssueAmount = resultList.Sum(r => r.LoanIssueAmount ?? 0),
//                    TotalDueAmount = resultList.Sum(r => r.TotalDueAmount ?? 0),
//                    TotalPaidAmount = resultList.Sum(r => r.TotalPaidAmount ?? 0),
//                    TotalBalanceAmount = resultList.Sum(r => r.BalanceAmount ?? 0),
//                    FromDateBs = request.FromDateBs,
//                    ToDateBs = request.ToDateBs,
//                    BranchName = branchName,
//                    MemberName = memberName,
//                    MemberGroupName = memberGroupName,
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





// Repository/Loan/OtherReports/MaturedLoanRepository.cs
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
    public class MaturedLoanRepository : IMaturedLoanRepository
    {
        private readonly AppDbContext _context;
        private readonly IDateConverterService _dateConverter;
        private readonly ILogger<MaturedLoanRepository> _logger;

        public MaturedLoanRepository(
            AppDbContext context,
            IDateConverterService dateConverter,
            ILogger<MaturedLoanRepository> logger)
        {
            _context = context;
            _dateConverter = dateConverter;
            _logger = logger;
        }

        // --------------------------------------------------------------
        // @SqlFilterExpOrder — column names match the SP's final SELECT
        // Preserves the substring-based natural sort for MemberId
        // (strips a trailing "-N" suffix before sorting) exactly as
        // in the legacy BLL.
        //
        // NOTE: the report is always visually grouped by Member (see the
        // view). This ORDER BY only controls the order loans appear in
        // within/across those member groups - it does not turn grouping
        // off.
        // --------------------------------------------------------------
        private static string BuildSqlOrderBy(MaturedLoanRequestDto request)
        {
            if (string.IsNullOrEmpty(request.OrderBy) || request.OrderBy == "-1")
                return string.Empty;

            return request.OrderBy.Trim() switch
            {
                "MemberId" => " ORDER BY SUBSTRING(MemberId, 1, LEN(MemberId) - CHARINDEX('-', MemberId) - 1), MemberId",
                "FullName" => " order by FullName",
                "MaturityDate" => " order by MaturityDate",
                "LoanAccountNo" => " order by LoanAccountNo",
                "DisburseAmount" => " order by LoanIssueAmount desc",
                "DueBalance" => " order by TotalDueAmount desc",
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

        public async Task<MaturedLoanData> GetReportDataAsync(MaturedLoanRequestDto request)
        {
            try
            {
                var branchIds = SanitizeBranchIds(request.BranchIds);

                var connectionString = _context.Database.GetConnectionString();
                using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();

                var sqlFilterExp = new StringBuilder();
                var sqlFilterExpBranchId = new StringBuilder();
                string? memberName = null;
                string? memberGroupName = null;
                string sqlTilldate = string.Empty;

                // --------------------------------------------------------------
                // Build filter expression matching legacy BLL:
                // 1. Member Group filter (goes into @SqlFilterExp)
                // 2. Member + Date range OR Date range only
                // 3. Branch filter (separate @SqlFilterExpbranchId)
                // 4. ORDER BY (separate @SqlFilterExpOrder)
                // 5. @SqlTilldate = Today's AD date (member mode) or ToDate (date mode)
                // --------------------------------------------------------------
                if (request.MemberGroupId != -1)
                {
                    sqlFilterExp.Append(" AND MR.SycMemberGroupId = ").Append(request.MemberGroupId);

                    // Get member group name for display
                    memberGroupName = await connection.QueryFirstOrDefaultAsync<string>(
                        "SELECT Name FROM SycMemberGroup WHERE SycMemberGroupId = @Id",
                        new { Id = request.MemberGroupId });
                }

                if (!string.IsNullOrWhiteSpace(request.MemberId))
                {
                    sqlFilterExp.Append(" And Mr.MemberId = '").Append(request.MemberId.Trim()).Append("'");

                    // For member mode, @SqlTilldate = today's AD date
                    sqlTilldate = $"'{DateTime.Now:yyyy-MM-dd}'";

                    // Get member name for display
                    var name = await connection.QueryFirstOrDefaultAsync<string>(
                        @"SELECT FirstName + ' ' +
                                 CASE WHEN MiddleName = '' THEN '' ELSE MiddleName + ' ' END +
                                 LastName
                          FROM MemMemberRegistration 
                          WHERE MemberId = @MemberId AND IsActive = 1",
                        new { MemberId = request.MemberId.Trim() });
                    memberName = name;
                }
                else if (!string.IsNullOrEmpty(request.FromDateBs) && !string.IsNullOrEmpty(request.ToDateBs))
                {
                    var fromDateAd = await _dateConverter.NepaliToEnglishAsync(request.FromDateBs);
                    var toDateAd = await _dateConverter.NepaliToEnglishAsync(request.ToDateBs);

                    var fromDateStr = fromDateAd.ToString("yyyy-MM-dd");
                    var toDateStr = toDateAd.ToString("yyyy-MM-dd");

                    sqlFilterExp.Append(" And ls.MaturityOn between '").Append(fromDateStr)
                                .Append("' And '").Append(toDateStr).Append("'");

                    // For date mode, @SqlTilldate = ToDate
                    sqlTilldate = $"'{toDateStr}'";
                }

                if (!string.IsNullOrEmpty(branchIds))
                {
                    sqlFilterExpBranchId.Append(" And v.UsmOfficeId in (").Append(branchIds).Append(")");
                }

                var sqlFilterExpOrder = BuildSqlOrderBy(request);

                var parameters = new DynamicParameters();
                parameters.Add("@SqlFilterExp", sqlFilterExp.ToString(), DbType.String, size: -1);
                parameters.Add("@SqlFilterExpbranchId", sqlFilterExpBranchId.ToString(), DbType.String, size: -1);
                parameters.Add("@SqlFilterExpOrder", sqlFilterExpOrder, DbType.String, size: -1);
                parameters.Add("@SqlTilldate", sqlTilldate, DbType.String, size: -1);

                var rows = await connection.QueryAsync<MaturedLoanRowDto>(
                    "sp_7_16_MaturedLoanReport",
                    parameters,
                    commandType: CommandType.StoredProcedure,
                    commandTimeout: 120
                );

                var resultList = rows.AsList();

                // Get branch names for display
                string branchName = "All";
                if (!string.IsNullOrEmpty(branchIds))
                {
                    var names = await connection.QueryAsync<string>(
                        $"SELECT OfficeName FROM UsmOffice WHERE UsmOfficeId IN ({branchIds})");
                    var nameList = names.Where(n => !string.IsNullOrWhiteSpace(n)).ToList();
                    branchName = nameList.Count > 0 ? string.Join(", ", nameList) : "All";
                }

                // --------------------------------------------------------------
                // Totals - computed from the fields the SP actually returns.
                // "Due Balance" total = TotalDueAmount, which the SP already
                // defines as PrincipleDue + InterestDue.
                // --------------------------------------------------------------
                return new MaturedLoanData
                {
                    Rows = resultList,
                    TotalRecords = resultList.Count,
                    TotalMembers = resultList
                        .Select(r => r.MemberId)
                        .Distinct()
                        .Count(),
                    TotalLoanIssueAmount = resultList.Sum(r => r.LoanIssueAmount ?? 0),
                    TotalPrincipleDue = resultList.Sum(r => r.PrincipleDue ?? 0),
                    TotalInterestDue = resultList.Sum(r => r.InterestDue ?? 0),
                    TotalDueAmount = resultList.Sum(r => r.TotalDueAmount ?? 0),
                    FromDateBs = request.FromDateBs,
                    ToDateBs = request.ToDateBs,
                    BranchName = branchName,
                    MemberName = memberName,
                    MemberGroupName = memberGroupName,
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
