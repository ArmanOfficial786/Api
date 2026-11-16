using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using NexgenCosysReport.DbContext;
using NexgenCosysReport.Dtos.RequestDtos.Loan.LoanAccountWiseReport;
using NexgenCosysReport.Inteface.ServiceInterface.Common;
using NexgenCosysReport.Inteface.ServiceInterface.Loan.LoanAccountWiseReport;
using System.Data;

namespace NexgenCosysReport.Repository.Loan.LoanAccountWiseReport
{
    /// <summary>
    /// Deleted loans report (webform: GetLoanDeletedReport). One stored procedure for every report type;
    /// the report type from the request only decides whether the date range is used:
    ///   ViewDeletedBetweenDate -> only loans deleted between FromDateBs and ToDateBs (both required)
    ///   anything else          -> all deleted loans (dates in the request are ignored)
    /// Branch, member group and order by come from the request in both cases.
    /// </summary>
    public class LoanDeletedRepository : ILoanDeletedRepository
    {
        // The only report type that uses the dates (the value the frontend sends). Everything else is "all details".
        private const string BetweenDatesReportType = "ViewDeletedBetweenDate";

        // "Order by" key sent by the frontend -> SQL
        private static readonly Dictionary<string, string> OrderBy = new(StringComparer.OrdinalIgnoreCase)
        {
            { "MemberId",        " order by substring(MemberId, 1,(len(MemberId)-charindex('-', MemberId))-1), MemberId " },
            { "FullName",        " order by FullName " },
            { "LoanAccountNo",   " order by substring(LoanAccountNo, 1,(len(LoanAccountNo)-charindex('-', LoanAccountNo))-1), LoanAccountNo " },
            { "AccountNo",       " order by substring(LoanAccountNo, 1,(len(LoanAccountNo)-charindex('-', LoanAccountNo))-1), LoanAccountNo " },
            { "LoanTypeName",    " order by LoanTypeName" },
            { "Type",            " order by LoanTypeName" },
            { "InterestRate",    " order by InterestRate" },
            { "LoanIssueAmount", " order by LoanIssueAmount" },
            { "IssueAmount",     " order by LoanIssueAmount" },
            { "LoanIssueDate",   " order by LoanIssueDate" },
            { "IssueDate",       " order by LoanIssueDate" },
            { "Period",          " order by Period" },
            { "ModifyDate",      " order by ModifyDate" }
        };

        private readonly AppDbContext _context;
        private readonly IDateConverterService _dateConverter;
        private readonly ILogger<LoanDeletedRepository> _logger;

        public LoanDeletedRepository(
            AppDbContext context,
            IDateConverterService dateConverter,
            ILogger<LoanDeletedRepository> logger)
        {
            _context = context;
            _dateConverter = dateConverter;
            _logger = logger;
        }

        public async Task<LoanDeletedData> GetReportDataAsync(LoanDeletedRequestDto request)
        {
            try
            {
                // 1. date filter: only for the "between dates" report (the screen sends Nepali dates, the database stores English)
                var betweenDates = string.Equals(request.ReportType?.Trim(), BetweenDatesReportType, StringComparison.OrdinalIgnoreCase);

                var dateFilter = "";
                if (betweenDates)
                {
                    if (string.IsNullOrWhiteSpace(request.FromDateBs) || string.IsNullOrWhiteSpace(request.ToDateBs))
                        throw new ArgumentException($"FromDateBs and ToDateBs are required when ReportType is '{BetweenDatesReportType}'.");

                    var from = await _dateConverter.NepaliToEnglishAsync(request.FromDateBs);
                    var to = await _dateConverter.NepaliToEnglishAsync(request.ToDateBs);

                    // the whole last day counts, even if DeletedDateOn has a time part
                    dateFilter = $" And DeletedDateOn >= '{from:yyyy-MM-dd}' And DeletedDateOn < '{to.AddDays(1):yyyy-MM-dd}'";
                }

                // 2. branch + member group filter ("-1" / empty = no filter)
                var branchIds = ToIdList(request.BranchIds);
                var hasGroup = long.TryParse(request.MemberGroupId, out var groupId) && groupId > 0;

                var branchFilter = "";
                if (branchIds != "-1") branchFilter += $" And v.UsmOfficeId in ({branchIds})";
                if (hasGroup) branchFilter += $" AND MR.SycMemberGroupId = {groupId}";

                // 3. order by (unknown or empty key = no ordering)
                var orderBy = OrderBy.GetValueOrDefault(request.OrderBy?.Trim() ?? "", "");

                // 4. run the stored procedure
                using var connection = new SqlConnection(_context.Database.GetConnectionString());
                await connection.OpenAsync();

                var parameters = new DynamicParameters();
                parameters.Add("@SqlFilterExp", dateFilter, DbType.String, size: -1);
                parameters.Add("@SqlFilterExpbranchId", branchFilter, DbType.String, size: -1);
                parameters.Add("@SqlFilterExpOrderBy", orderBy, DbType.String, size: -1);

                var rows = (await connection.QueryAsync<LoanDeletedRowDto>(
                    "sp_7_16_LoanDletedReport", parameters,
                    commandType: CommandType.StoredProcedure, commandTimeout: 120)).AsList();

                // 5. names for the report header (ids are numbers only, so they are safe in the query)
                var branchName = "All";
                if (branchIds != "-1")
                {
                    var names = await connection.QueryAsync<string>(
                        $"SELECT OfficeName FROM UsmOffice WHERE UsmOfficeId IN ({branchIds})");
                    branchName = string.Join(", ", names.Where(n => !string.IsNullOrWhiteSpace(n)));
                    if (branchName == "") branchName = "All";
                }

                var memberGroupName = hasGroup
                    ? await connection.QueryFirstOrDefaultAsync<string>(
                        "SELECT Name FROM SycMemberGroup WHERE SycMemberGroupId = @Id", new { Id = groupId })
                    : null;

                return new LoanDeletedData
                {
                    Rows = rows,
                    TotalRecords = rows.Count,
                    ReportType = request.ReportType,      // as sent by the frontend
                    FromDateBs = betweenDates ? request.FromDateBs : null,   // "all details" shows no date range
                    ToDateBs = betweenDates ? request.ToDateBs : null,
                    BranchName = branchName,
                    MemberGroupName = memberGroupName,
                    OrderBy = request.OrderBy
                };
            }
            catch (ArgumentException)
            {
                throw; // bad input: the controller returns 400
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetReportDataAsync");
                throw;
            }
        }

        // "1, 2 ,x,3" -> "1,2,3"; empty / -1 / junk -> "-1" (no filter). Only numbers survive, so it is safe in SQL.
        private static string ToIdList(string? ids)
        {
            if (string.IsNullOrWhiteSpace(ids) || ids == "-1") return "-1";

            var numbers = ids
                .Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Where(id => long.TryParse(id, out _));

            var list = string.Join(",", numbers);
            return list == "" ? "-1" : list;
        }
    }
}