//// Repository/Loan/OtherReports/LoanInterestReceivableYearEndRepository.cs
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
//    public class LoanInterestReceivableYearEndRepository : ILoanInterestReceivableYearEndRepository
//    {
//        private readonly AppDbContext _context;
//        private readonly IDateConverterService _dateConverter;
//        private readonly ILogger<LoanInterestReceivableYearEndRepository> _logger;

//        public LoanInterestReceivableYearEndRepository(
//            AppDbContext context,
//            IDateConverterService dateConverter,
//            ILogger<LoanInterestReceivableYearEndRepository> logger)
//        {
//            _context = context;
//            _dateConverter = dateConverter;
//            _logger = logger;
//        }

//        // --------------------------------------------------------------
//        // @SqlFilterExpOrderBy — column names match the SP's final SELECT
//        // Preserves the substring-based natural sort for MemberId and
//        // LoanAccountNo exactly as in the legacy BLL.
//        // --------------------------------------------------------------
//        private static string BuildSqlOrderBy(LoanInterestReceivableYearEndRequestDto request)
//        {
//            if (string.IsNullOrEmpty(request.OrderBy) || request.OrderBy == "-1")
//                return string.Empty;

//            return request.OrderBy.Trim() switch
//            {
//                "MemberId" => " order by substring(MemberId, 1,(len(MemberId)-charindex('-', MemberId))-1), MemberId ",
//                "FullName" => " order by FullName",
//                "LoanAccountNo" => " order by substring(LoanAccountNo, 1,(len(LoanAccountNo)-charindex('-', LoanAccountNo))-1), LoanAccountNo ",
//                "InterestAmount" => " order by InterestAmount DESC",
//                "LoanTypeName" => " order by LoanTypeName",
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

//        public async Task<LoanInterestReceivableYearEndData> GetReportDataAsync(LoanInterestReceivableYearEndRequestDto request)
//        {
//            try
//            {
//                var branchIds = SanitizeBranchIds(request.BranchIds);

//                var connectionString = _context.Database.GetConnectionString();
//                using var connection = new SqlConnection(connectionString);
//                await connection.OpenAsync();

//                var sqlFilterExpBranchId = new StringBuilder();
//                string? memberGroupName = null;

//                // --------------------------------------------------------------
//                // Build filter expression matching legacy BLL:
//                // 1. Member Group filter (@SqlFilterExpbranchId)
//                // 2. Branch filter (@SqlFilterExpbranchId)
//                // 3. @SqlFilterExp = quoted date string (used inside SP)
//                // 4. ORDER BY (@SqlFilterExpOrderBy)
//                // --------------------------------------------------------------
//                if (request.MemberGroupId != -1)
//                {
//                    sqlFilterExpBranchId.Append(" AND MR.SycMemberGroupId = ").Append(request.MemberGroupId);

//                    // Get member group name for display
//                    memberGroupName = await connection.QueryFirstOrDefaultAsync<string>(
//                        "SELECT Name FROM SycMemberGroup WHERE SycMemberGroupId = @Id",
//                        new { Id = request.MemberGroupId });
//                }

//                if (!string.IsNullOrEmpty(branchIds))
//                {
//                    sqlFilterExpBranchId.Append(" And v.UsmOfficeId in (").Append(branchIds).Append(")");
//                }

//                // --------------------------------------------------------------
//                // Determine the target date based on SelectType:
//                // - "Monthly": end date of given year/month
//                // - "Yearly": end date of Ashad (month 3) of given year
//                // - anything else ("As", blank, unrecognized): falls back to
//                //   AsOnDateBs directly if it was supplied. This is a
//                //   deliberate fallback (not just an "As" case) so a caller
//                //   that omits/mis-cases SelectType but does send AsOnDateBs
//                //   still works, instead of always throwing.
//                // --------------------------------------------------------------
//                DateTime? targetDateAd = null;
//                var selectType = (request.SelectType ?? string.Empty).Trim();

//                if (selectType.Equals("Monthly", StringComparison.OrdinalIgnoreCase))
//                {
//                    if (request.MonthlyYear.HasValue && request.MonthlyMonth.HasValue)
//                    {
//                        var monthEndBs = await GetNepaliMonthEndDateAsync(connection, request.MonthlyYear.Value, request.MonthlyMonth.Value);
//                        if (!string.IsNullOrEmpty(monthEndBs))
//                        {
//                            targetDateAd = await _dateConverter.NepaliToEnglishAsync(monthEndBs);
//                        }
//                    }
//                }
//                else if (selectType.Equals("Yearly", StringComparison.OrdinalIgnoreCase))
//                {
//                    if (request.YearlyYear.HasValue)
//                    {
//                        var yearEndBs = await GetNepaliMonthEndDateAsync(connection, request.YearlyYear.Value, 3);
//                        if (!string.IsNullOrEmpty(yearEndBs))
//                        {
//                            targetDateAd = await _dateConverter.NepaliToEnglishAsync(yearEndBs);
//                        }
//                    }
//                }
//                else if (!string.IsNullOrEmpty(request.AsOnDateBs) && request.AsOnDateBs != "-1")
//                {
//                    // Covers "As" explicitly, and also covers a blank/omitted
//                    // SelectType as long as AsOnDateBs was actually supplied.
//                    targetDateAd = await _dateConverter.NepaliToEnglishAsync(request.AsOnDateBs);
//                }

//                if (!targetDateAd.HasValue)
//                {
//                    throw new ArgumentException(
//                        $"Unable to determine target date. SelectType='{request.SelectType}', " +
//                        $"AsOnDateBs='{request.AsOnDateBs}', MonthlyYear={request.MonthlyYear}, " +
//                        $"MonthlyMonth={request.MonthlyMonth}, YearlyYear={request.YearlyYear}.");
//                }

//                var targetDateStr = targetDateAd.Value.ToString("yyyy-MM-dd");

//                // @SqlFilterExp is a quoted date string (SP concatenates it inside SQL)
//                var sqlFilterExp = $"'{targetDateStr}'";
//                var sqlFilterExpOrderBy = BuildSqlOrderBy(request);

//                var parameters = new DynamicParameters();
//                parameters.Add("@SqlFilterExp", sqlFilterExp, DbType.String, size: -1);
//                parameters.Add("@SqlFilterExpbranchId", sqlFilterExpBranchId.ToString(), DbType.String, size: -1);
//                parameters.Add("@SqlFilterExpOrderBy", sqlFilterExpOrderBy, DbType.String, size: -1);

//                var rows = await connection.QueryAsync<LoanInterestReceivableYearEndRowDto>(
//                    "sp_7_16_LoanInterestReceivableYearEndReport",
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

//                return new LoanInterestReceivableYearEndData
//                {
//                    Rows = resultList,
//                    TotalRecords = resultList.Count,
//                    TotalLoanIssueAmount = resultList.Sum(r => r.LoanIssueAmount ?? 0),
//                    TotalDepositAmount = resultList.Sum(r => r.DepositAmount ?? 0),
//                    TotalBalance = resultList.Sum(r => r.Balance ?? 0),
//                    TotalInterestAmount = resultList.Sum(r => r.InterestAmount ?? 0),
//                    AsOnDate = targetDateAd.Value.ToString("yyyy-MM-dd"),
//                    BranchName = branchName,
//                    MemberGroupName = memberGroupName,
//                    SelectType = request.SelectType,
//                    OrderBy = request.OrderBy
//                };
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, "Error in GetReportDataAsync");
//                throw;
//            }
//        }

//        // --------------------------------------------------------------
//        // Helper: Gets the end BS date (YYYY/MM/DD) for the given
//        // Nepali year and month by looking up the ComCalendar table.
//        // --------------------------------------------------------------
//        private async Task<string?> GetNepaliMonthEndDateAsync(SqlConnection connection, int year, int month)
//        {
//            var endBs = await connection.QueryFirstOrDefaultAsync<string>(
//                @"SELECT TOP 1 
//                        CAST(NepaliYear AS nvarchar) + '/' + 
//                        RIGHT('0' + CAST(NepaliMonth AS nvarchar), 2) + '/' + 
//                        RIGHT('0' + CAST(NepaliDay AS nvarchar), 2) AS NepaliDate
//                  FROM ComCalendar 
//                  WHERE NepaliYear = @Year AND NepaliMonth = @Month
//                  ORDER BY NepaliDay DESC",
//                new { Year = year, Month = month });

//            return endBs;
//        }
//    }
//}



// Repository/Loan/OtherReports/LoanInterestReceivableYearEndRepository.cs
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
    public class LoanInterestReceivableYearEndRepository : ILoanInterestReceivableYearEndRepository
    {
        private readonly AppDbContext _context;
        private readonly IDateConverterService _dateConverter;
        private readonly ILogger<LoanInterestReceivableYearEndRepository> _logger;

        public LoanInterestReceivableYearEndRepository(
            AppDbContext context,
            IDateConverterService dateConverter,
            ILogger<LoanInterestReceivableYearEndRepository> logger)
        {
            _context = context;
            _dateConverter = dateConverter;
            _logger = logger;
        }

        // Same values and SQL text as the ddlOrderBy switch in the WebForm.
        private static string BuildSqlOrderBy(string? orderBy)
        {
            if (string.IsNullOrWhiteSpace(orderBy) || orderBy == "-1")
                return string.Empty;

            return orderBy.Trim() switch
            {
                "MemberId" => " order by substring(MemberId, 1,(len(MemberId)-charindex('-', MemberId))-1), MemberId ",
                "FullName" => " order by FullName",
                "LoanAccountNo" => " order by substring(LoanAccountNo, 1,(len(LoanAccountNo)-charindex('-', LoanAccountNo))-1), LoanAccountNo ",
                "InterestAmount" => " order by InterestAmount DESC",
                "LoanTypeName" => " order by LoanTypeName",
                _ => string.Empty
            };
        }

        // Positive numeric ids only; "" means "no filter".
        private static string SanitizeBranchIds(string? branchIds)
        {
            if (string.IsNullOrWhiteSpace(branchIds) || branchIds == "-1" || branchIds == "string")
                return string.Empty;

            var valid = branchIds
                .Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Where(id => long.TryParse(id, out var v) && v > 0)
                .Select(id => long.Parse(id).ToString())
                .Distinct();

            return string.Join(",", valid);
        }

        public async Task<LoanInterestReceivableYearEndData> GetReportDataAsync(LoanInterestReceivableYearEndRequestDto request)
        {
            try
            {
                // WebForms: "Please select Branch Name" when nothing is selected
                var branchIds = SanitizeBranchIds(request.BranchIds);
                if (string.IsNullOrEmpty(branchIds))
                    throw new ArgumentException("Please select Branch Name");

                var connectionString = _context.Database.GetConnectionString();
                if (string.IsNullOrEmpty(connectionString))
                    throw new InvalidOperationException("Database connection string is not configured.");

                using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();

                // ---- @SqlFilterExpbranchId: member group first, then branch (same order as the BLL) ----
                var sqlFilterExpBranchId = new StringBuilder();
                string? memberGroupName = null;

                if (request.MemberGroupId > 0)
                {
                    sqlFilterExpBranchId.Append(" AND MR.SycMemberGroupId = ").Append(request.MemberGroupId);

                    memberGroupName = await connection.QueryFirstOrDefaultAsync<string>(
                        "SELECT Name FROM SycMemberGroup WHERE SycMemberGroupId = @Id",
                        new { Id = request.MemberGroupId });
                }

                sqlFilterExpBranchId.Append(" And v.UsmOfficeId in (").Append(branchIds).Append(")");

                // ---- target date, same three modes as rbl_selectType ----
                var targetDateAd = await ResolveTargetDateAsync(connection, request);
                var targetDateStr = targetDateAd.ToString("yyyy-MM-dd");

                // @SqlFilterExp is a quoted date string (the SP concatenates it into its SQL)
                var parameters = new DynamicParameters();
                parameters.Add("@SqlFilterExp", $"'{targetDateStr}'", DbType.String, size: -1);
                parameters.Add("@SqlFilterExpbranchId", sqlFilterExpBranchId.ToString(), DbType.String, size: -1);
                parameters.Add("@SqlFilterExpOrderBy", BuildSqlOrderBy(request.OrderBy), DbType.String, size: -1);

                var rows = await connection.QueryAsync<LoanInterestReceivableYearEndRowDto>(
                    "sp_7_16_LoanInterestReceivableYearEndReport",
                    parameters,
                    commandType: CommandType.StoredProcedure,
                    commandTimeout: 120);

                var resultList = rows.AsList();

                // ---- header: branch names ----
                var branchIdList = branchIds.Split(',').Select(long.Parse).ToList();
                var names = await connection.QueryAsync<string>(
                    "SELECT OfficeName FROM UsmOffice WHERE UsmOfficeId IN @Ids",
                    new { Ids = branchIdList });
                var nameList = names.Where(n => !string.IsNullOrWhiteSpace(n)).ToList();
                var branchName = nameList.Count > 0 ? string.Join(", ", nameList) : "All";

                return new LoanInterestReceivableYearEndData
                {
                    Rows = resultList,
                    TotalRecords = resultList.Count,
                    TotalLoanIssueAmount = resultList.Sum(r => r.LoanIssueAmount ?? 0),
                    TotalDepositAmount = resultList.Sum(r => r.DepositAmount ?? 0),
                    TotalBalance = resultList.Sum(r => r.Balance ?? 0),
                    TotalInterestAmount = resultList.Sum(r => r.InterestAmount ?? 0),
                    AsOnDate = targetDateStr,
                    BranchName = branchName,
                    MemberGroupName = memberGroupName,
                    SelectType = request.SelectType,
                    OrderBy = request.OrderBy
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetReportDataAsync (LoanInterestReceivableYearEnd)");
                throw;
            }
        }

        // WebForms behaviour:
        //   As      -> the picked date
        //   Monthly -> English end date of the selected Nepali year/month
        //   Yearly  -> English end date of month 3 (Ashad) of the selected year
        private async Task<DateTime> ResolveTargetDateAsync(
            SqlConnection connection, LoanInterestReceivableYearEndRequestDto request)
        {
            var selectType = (request.SelectType ?? string.Empty).Trim();

            if (selectType.Equals("Monthly", StringComparison.OrdinalIgnoreCase))
            {
                if (!request.MonthlyYear.HasValue || !request.MonthlyMonth.HasValue
                    || request.MonthlyMonth < 1 || request.MonthlyMonth > 12)
                    throw new ArgumentException("Monthly report requires a valid year and month.");

                var endBs = await GetNepaliMonthEndDateAsync(connection, request.MonthlyYear.Value, request.MonthlyMonth.Value);
                if (string.IsNullOrEmpty(endBs))
                    throw new ArgumentException("Unable to find the month end date for the selected year and month.");

                return await _dateConverter.NepaliToEnglishAsync(endBs);
            }

            if (selectType.Equals("Yearly", StringComparison.OrdinalIgnoreCase))
            {
                if (!request.YearlyYear.HasValue)
                    throw new ArgumentException("Yearly report requires a year.");

                var endBs = await GetNepaliMonthEndDateAsync(connection, request.YearlyYear.Value, 3);
                if (string.IsNullOrEmpty(endBs))
                    throw new ArgumentException("Unable to find the year end date for the selected year.");

                return await _dateConverter.NepaliToEnglishAsync(endBs);
            }

            // "As" (default): as-on date
            if (string.IsNullOrWhiteSpace(request.AsOnDateBs) || request.AsOnDateBs == "-1")
                throw new ArgumentException("As on date is required.");

            return await _dateConverter.NepaliToEnglishAsync(request.AsOnDateBs);
        }

        // End BS date (YYYY/MM/DD) of a Nepali year/month from ComCalendar.
        private static async Task<string?> GetNepaliMonthEndDateAsync(SqlConnection connection, int year, int month)
        {
            return await connection.QueryFirstOrDefaultAsync<string>(
                @"SELECT TOP 1
                        CAST(NepaliYear AS nvarchar) + '/' +
                        RIGHT('0' + CAST(NepaliMonth AS nvarchar), 2) + '/' +
                        RIGHT('0' + CAST(NepaliDay AS nvarchar), 2) AS NepaliDate
                  FROM ComCalendar
                  WHERE NepaliYear = @Year AND NepaliMonth = @Month
                  ORDER BY NepaliDay DESC",
                new { Year = year, Month = month });
        }
    }
}