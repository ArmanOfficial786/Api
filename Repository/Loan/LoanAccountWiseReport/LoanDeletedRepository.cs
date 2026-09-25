
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
    public class LoanDeletedRepository : ILoanDeletedRepository
    {
        private readonly AppDbContext _context;
        private readonly IDateConverterService _dateConverter;
        private readonly ILogger<LoanDeletedRepository> _logger;


        private static readonly Dictionary<string, string> OrderByMap = new(StringComparer.OrdinalIgnoreCase)
        {
            { "MemberId", " order by substring(MemberId, 1,(len(MemberId)-charindex('-', MemberId))-1), MemberId " },
            { "FullName", " order by FullName " },
            { "LoanAccountNo", " order by substring(LoanAccountNo, 1,(len(LoanAccountNo)-charindex('-', LoanAccountNo))-1), LoanAccountNo " },
            { "InterestRate", " order by InterestRate" },
            { "LoanTypeName", " order by LoanTypeName" },
            { "LoanIssueAmount", " order by LoanIssueAmount" },
            { "LoanIssueDate", " order by LoanIssueDate" },
            { "Period", " order by Period" },
            { "ModifyDate", " order by ModifyDate" }
        };

        public LoanDeletedRepository(
            AppDbContext context,
            IDateConverterService dateConverter,
            ILogger<LoanDeletedRepository> logger)
        {
            _context = context;
            _dateConverter = dateConverter;
            _logger = logger;
        }

        private static string BuildSqlOrderBy(string? orderBy)
        {
            if (string.IsNullOrWhiteSpace(orderBy))
                return string.Empty;

            return OrderByMap.TryGetValue(orderBy.Trim(), out var clause) ? clause : string.Empty;
        }

        private static string SanitizeBranchIds(string? branchIds)
        {
            if (string.IsNullOrWhiteSpace(branchIds) || branchIds == "-1" || branchIds == "string")
                return "-1";

            var validIds = branchIds
                .Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Where(id => long.TryParse(id, out _));

            var joined = string.Join(",", validIds);
            return string.IsNullOrEmpty(joined) ? "-1" : joined;
        }

        public async Task<LoanDeletedData> GetReportDataAsync(LoanDeletedRequestDto request)
        {
            try
            {
                var branchIds = SanitizeBranchIds(request.BranchIds);

                var connectionString = _context.Database.GetConnectionString();
                using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();


                var sqlFilterExpBranchId = string.Empty;
                if (branchIds != "-1")
                {
                    sqlFilterExpBranchId += $" And v.UsmOfficeId in ({branchIds})";
                }

                if (!string.IsNullOrWhiteSpace(request.MemberGroupId) && request.MemberGroupId != "-1" && request.MemberGroupId != "string"
                    && long.TryParse(request.MemberGroupId, out var memberGroupId))
                {
                    sqlFilterExpBranchId += $" AND MR.SycMemberGroupId = {memberGroupId}";
                }


                var sqlFilterExp = string.Empty;
                if (request.ReportType.Equals("BtnDate", StringComparison.OrdinalIgnoreCase))
                {
                    if (string.IsNullOrEmpty(request.FromDateBs) || string.IsNullOrEmpty(request.ToDateBs))
                    {
                        throw new ArgumentException("FromDateBs and ToDateBs are required when ReportType is 'BtnDate'.");
                    }

                    var fromDateAd = await _dateConverter.NepaliToEnglishAsync(request.FromDateBs);
                    var toDateAd = await _dateConverter.NepaliToEnglishAsync(request.ToDateBs);
                    var fromDateStr = fromDateAd.ToString("yyyy-MM-dd");
                    var toDateStr = toDateAd.ToString("yyyy-MM-dd");

                    sqlFilterExp = $" And DeletedDateOn between '{fromDateStr}' And '{toDateStr}'";
                }

                var sqlFilterExpOrderBy = BuildSqlOrderBy(request.OrderBy);

                var parameters = new DynamicParameters();
                parameters.Add("@SqlFilterExp", sqlFilterExp, DbType.String, size: -1);
                parameters.Add("@SqlFilterExpbranchId", sqlFilterExpBranchId, DbType.String, size: -1);
                parameters.Add("@SqlFilterExpOrderBy", sqlFilterExpOrderBy, DbType.String, size: -1);

                var rows = await connection.QueryAsync<LoanDeletedRowDto>(
                    "sp_7_16_LoanDletedReport",
                    parameters,
                    commandType: CommandType.StoredProcedure,
                    commandTimeout: 120
                );

                var resultList = rows.AsList();

                string branchName = "All";
                if (branchIds != "-1")
                {
                    var names = await connection.QueryAsync<string>(
                        $"SELECT OfficeName FROM UsmOffice WHERE UsmOfficeId IN ({branchIds})");
                    var nameList = names.Where(n => !string.IsNullOrWhiteSpace(n)).ToList();
                    branchName = nameList.Count > 0 ? string.Join(", ", nameList) : "All";
                }

                string? memberGroupName = null;
                if (!string.IsNullOrWhiteSpace(request.MemberGroupId) && request.MemberGroupId != "-1" && request.MemberGroupId != "string"
                    && long.TryParse(request.MemberGroupId, out var memberGroupIdForName))
                {
                    memberGroupName = await connection.QueryFirstOrDefaultAsync<string>(
                        "SELECT Name FROM SycMemberGroup WHERE SycMemberGroupId = @Id",
                        new { Id = memberGroupIdForName });
                }

                return new LoanDeletedData
                {
                    Rows = resultList,
                    TotalRecords = resultList.Count,
                    ReportType = request.ReportType,
                    FromDateBs = request.FromDateBs,
                    ToDateBs = request.ToDateBs,
                    BranchName = branchName,
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