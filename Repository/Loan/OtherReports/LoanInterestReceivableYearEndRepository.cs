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

        // --------------------------------------------------------------
        // @SqlFilterExpOrderBy — column names match the SP's final SELECT
        // Preserves the substring-based natural sort for MemberId and
        // LoanAccountNo exactly as in the legacy BLL.
        // --------------------------------------------------------------
        private static string BuildSqlOrderBy(LoanInterestReceivableYearEndRequestDto request)
        {
            if (string.IsNullOrEmpty(request.OrderBy) || request.OrderBy == "-1")
                return string.Empty;

            return request.OrderBy.Trim() switch
            {
                "MemberId" => " order by substring(MemberId, 1,(len(MemberId)-charindex('-', MemberId))-1), MemberId ",
                "FullName" => " order by FullName",
                "LoanAccountNo" => " order by substring(LoanAccountNo, 1,(len(LoanAccountNo)-charindex('-', LoanAccountNo))-1), LoanAccountNo ",
                "InterestAmount" => " order by InterestAmount DESC",
                "LoanTypeName" => " order by LoanTypeName",
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

        public async Task<LoanInterestReceivableYearEndData> GetReportDataAsync(LoanInterestReceivableYearEndRequestDto request)
        {
            try
            {
                var branchIds = SanitizeBranchIds(request.BranchIds);

                var connectionString = _context.Database.GetConnectionString();
                using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();

                var sqlFilterExpBranchId = new StringBuilder();
                string? memberGroupName = null;

                // --------------------------------------------------------------
                // Build filter expression matching legacy BLL:
                // 1. Member Group filter (@SqlFilterExpbranchId)
                // 2. Branch filter (@SqlFilterExpbranchId)
                // 3. @SqlFilterExp = quoted date string (used inside SP)
                // 4. ORDER BY (@SqlFilterExpOrderBy)
                // --------------------------------------------------------------
                if (!string.IsNullOrEmpty(request.MemberGroupId) && request.MemberGroupId != "-1")
                {
                    sqlFilterExpBranchId.Append(" AND MR.SycMemberGroupId = ").Append(request.MemberGroupId);

                    // Get member group name for display
                    memberGroupName = await connection.QueryFirstOrDefaultAsync<string>(
                        "SELECT Name FROM SycMemberGroup WHERE SycMemberGroupId = @Id",
                        new { Id = request.MemberGroupId });
                }

                if (!string.IsNullOrEmpty(branchIds))
                {
                    sqlFilterExpBranchId.Append(" And v.UsmOfficeId in (").Append(branchIds).Append(")");
                }

                // --------------------------------------------------------------
                // Determine the target date based on SelectType:
                // - "As" (As on date): use AsOnDateBs directly
                // - "Monthly": use end date of given year/month
                // - "Yearly": use end date of Ashad (month 3) of given year
                // --------------------------------------------------------------
                DateTime? targetDateAd = null;

                if (request.SelectType == "As")
                {
                    if (!string.IsNullOrEmpty(request.AsOnDateBs) && request.AsOnDateBs != "-1")
                    {
                        targetDateAd = await _dateConverter.NepaliToEnglishAsync(request.AsOnDateBs);
                    }
                }
                else if (request.SelectType == "Monthly")
                {
                    if (request.MonthlyYear.HasValue && request.MonthlyMonth.HasValue)
                    {
                        // Get end date of the BS month
                        var monthEndBs = await GetNepaliMonthEndDateAsync(connection, request.MonthlyYear.Value, request.MonthlyMonth.Value);
                        if (!string.IsNullOrEmpty(monthEndBs))
                        {
                            targetDateAd = await _dateConverter.NepaliToEnglishAsync(monthEndBs);
                        }
                    }
                }
                else if (request.SelectType == "Yearly")
                {
                    if (request.YearlyYear.HasValue)
                    {
                        // Get end date of Ashad (month 3) of the BS year
                        var yearEndBs = await GetNepaliMonthEndDateAsync(connection, request.YearlyYear.Value, 3);
                        if (!string.IsNullOrEmpty(yearEndBs))
                        {
                            targetDateAd = await _dateConverter.NepaliToEnglishAsync(yearEndBs);
                        }
                    }
                }

                if (!targetDateAd.HasValue)
                {
                    throw new ArgumentException("Unable to determine target date from the provided parameters.");
                }

                var targetDateStr = targetDateAd.Value.ToString("yyyy-MM-dd");

                // @SqlFilterExp is a quoted date string (SP concatenates it inside SQL)
                var sqlFilterExp = $"'{targetDateStr}'";
                var sqlFilterExpOrderBy = BuildSqlOrderBy(request);

                var parameters = new DynamicParameters();
                parameters.Add("@SqlFilterExp", sqlFilterExp, DbType.String, size: -1);
                parameters.Add("@SqlFilterExpbranchId", sqlFilterExpBranchId.ToString(), DbType.String, size: -1);
                parameters.Add("@SqlFilterExpOrderBy", sqlFilterExpOrderBy, DbType.String, size: -1);

                var rows = await connection.QueryAsync<LoanInterestReceivableYearEndRowDto>(
                    "sp_7_16_LoanInterestReceivableYearEndReport",
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

                return new LoanInterestReceivableYearEndData
                {
                    Rows = resultList,
                    TotalRecords = resultList.Count,
                    TotalLoanIssueAmount = resultList.Sum(r => r.LoanIssueAmount ?? 0),
                    TotalDepositAmount = resultList.Sum(r => r.DepositAmount ?? 0),
                    TotalBalance = resultList.Sum(r => r.Balance ?? 0),
                    TotalInterestAmount = resultList.Sum(r => r.InterestAmount ?? 0),
                    AsOnDate = targetDateAd.Value.ToString("yyyy-MM-dd"),
                    BranchName = branchName,
                    MemberGroupName = memberGroupName,
                    SelectType = request.SelectType,
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
        // Helper: Gets the end BS date (YYYY/MM/DD) for the given
        // Nepali year and month by looking up the ComCalendar table.
        // --------------------------------------------------------------
        private async Task<string?> GetNepaliMonthEndDateAsync(SqlConnection connection, int year, int month)
        {
            // Adjust month formatting to MM
            var monthStr = month.ToString("D2");

            var endBs = await connection.QueryFirstOrDefaultAsync<string>(
                @"SELECT TOP 1 
                        CAST(NepaliYear AS nvarchar) + '/' + 
                        RIGHT('0' + CAST(NepaliMonth AS nvarchar), 2) + '/' + 
                        RIGHT('0' + CAST(NepaliDay AS nvarchar), 2) AS NepaliDate
                  FROM ComCalendar 
                  WHERE NepaliYear = @Year AND NepaliMonth = @Month
                  ORDER BY NepaliDay DESC",
                new { Year = year, Month = month });

            return endBs;
        }
    }
}