
//using Dapper;
//using Microsoft.Data.SqlClient;
//using Microsoft.EntityFrameworkCore;
//using NexgenCosysReport.DbContext;
//using NexgenCosysReport.Dtos.RequestDtos.Loan.LoanAccountWiseReport;
//using NexgenCosysReport.Inteface.ServiceInterface.Common;
//using NexgenCosysReport.Inteface.ServiceInterface.Loan.LoanAccountWiseReport;
//using System.Data;
//using System.Text;

//namespace NexgenCosysReport.Repository.Loan.LoanAccountWiseReport
//{
//    public class LoanVerifiedUnverifiedRepository : ILoanVerifiedUnverifiedRepository
//    {
//        private readonly AppDbContext _context;
//        private readonly IDateConverterService _dateConverter;
//        private readonly ILogger<LoanVerifiedUnverifiedRepository> _logger;

//        public LoanVerifiedUnverifiedRepository(
//            AppDbContext context,
//            IDateConverterService dateConverter,
//            ILogger<LoanVerifiedUnverifiedRepository> logger)
//        {
//            _context = context;
//            _dateConverter = dateConverter;
//            _logger = logger;
//        }


//        private static string BuildSqlOrderBy(LoanVerifiedUnverifiedRequestDto request)
//        {
//            if (string.IsNullOrEmpty(request.OrderBy) || request.OrderBy == "-1")
//                return string.Empty;

//            return request.OrderBy.Trim() switch
//            {
//                "MemberName" => " order by MemberName ",
//                "MemberId" => " order by substring(MemberId, 1,(len(MemberId)-charindex('-', MemberId))-1), MemberId  ",
//                "LoanAccountNo" => " order by substring(LoanAccountNo, 1,(len(LoanAccountNo)-charindex('-', LoanAccountNo))-1), LoanAccountNo",
//                "LoanTypeName" => " order by LoanTypeName",
//                "LoanIssueOn" => " order by L.LoanIssueOn ",
//                "VerifiedOn" => " order by VerifiedOn ",
//                "VerifiedBy" => " order by US.FullName ",
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

//        public async Task<LoanVerifiedUnverifiedData> GetReportDataAsync(LoanVerifiedUnverifiedRequestDto request)
//        {
//            try
//            {
//                var branchIds = SanitizeBranchIds(request.BranchIds);

//                var connectionString = _context.Database.GetConnectionString();
//                using var connection = new SqlConnection(connectionString);
//                await connection.OpenAsync();

//                var sqlFilterExp = new StringBuilder();

//                if (!string.IsNullOrWhiteSpace(request.MemberRegistrationId) &&
//                    request.MemberRegistrationId != "-1" &&
//                    request.MemberRegistrationId != "")
//                {
//                    sqlFilterExp.Append(" And MR.MemMemberRegistrationId = ").Append(request.MemberRegistrationId);
//                }

//                if (!string.IsNullOrEmpty(request.FromDateBs) && !string.IsNullOrEmpty(request.ToDateBs)
//                    && request.FromDateBs != "-1" && request.ToDateBs != "-1")
//                {
//                    var fromDateAd = await _dateConverter.NepaliToEnglishAsync(request.FromDateBs);
//                    var toDateAd = await _dateConverter.NepaliToEnglishAsync(request.ToDateBs);

//                    var fromDateStr = fromDateAd.ToString("yyyy-MM-dd");
//                    var toDateStr = toDateAd.ToString("yyyy-MM-dd");

//                    sqlFilterExp.Append(" And L.LoanIssueOn between '").Append(fromDateStr)
//                                .Append("' And '").Append(toDateStr).Append("' ");
//                }

//                if (!string.IsNullOrEmpty(branchIds))
//                {
//                    sqlFilterExp.Append(" And L.UsmOfficeId in (").Append(branchIds).Append(")");
//                }


//                if (request.ReportType == "LoanVerified")
//                {
//                    sqlFilterExp.Append(" And L.IsVerified=1  AND L.LmtLoanStatusId IN (1,2)");
//                }
//                else if (request.ReportType == "LoanUnverified")
//                {
//                    sqlFilterExp.Append(" And L.IsVerified=0  AND L.LmtLoanStatusId IN (1,2)");
//                }

//                var sqlFilterExpOrder = BuildSqlOrderBy(request);

//                var parameters = new DynamicParameters();
//                parameters.Add("@SqlFilterExp", sqlFilterExp.ToString(), DbType.String, size: -1);
//                parameters.Add("@SqlFilterExpOrder", sqlFilterExpOrder, DbType.String, size: -1);

//                var rows = await connection.QueryAsync<LoanVerifiedUnverifiedRowDto>(
//                    "sp_7_16_GetLoanVerifiedUnverifiedReport",
//                    parameters,
//                    commandType: CommandType.StoredProcedure,
//                    commandTimeout: 120
//                );

//                var resultList = rows.AsList();


//                string branchName = "All";
//                if (!string.IsNullOrEmpty(branchIds))
//                {
//                    var names = await connection.QueryAsync<string>(
//                        $"SELECT OfficeName FROM UsmOffice WHERE UsmOfficeId IN ({branchIds})");
//                    var nameList = names.Where(n => !string.IsNullOrWhiteSpace(n)).ToList();
//                    branchName = nameList.Count > 0 ? string.Join(", ", nameList) : "All";
//                }

//                var reportTypeName = request.ReportType == "V" ? "Loan Verified Report" : "Loan Unverified Report";

//                return new LoanVerifiedUnverifiedData
//                {
//                    Rows = resultList,
//                    TotalRecords = resultList.Count,
//                    TotalLoanIssueAmount = resultList.Sum(r => r.LoanIssueAmount ?? 0),
//                    FromDateBs = request.FromDateBs,
//                    ToDateBs = request.ToDateBs,
//                    BranchName = branchName,
//                    ReportType = request.ReportType,
//                    ReportTypeName = reportTypeName,
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




using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using NexgenCosysReport.DbContext;
using NexgenCosysReport.Dtos.RequestDtos.Loan.LoanAccountWiseReport;
using NexgenCosysReport.Inteface.ServiceInterface.Common;
using NexgenCosysReport.Inteface.ServiceInterface.Loan.LoanAccountWiseReport;
using System.Data;
using System.Globalization;
using System.Text;

namespace NexgenCosysReport.Repository.Loan.LoanAccountWiseReport
{
    public class LoanVerifiedUnverifiedRepository : ILoanVerifiedUnverifiedRepository
    {
        private static readonly Dictionary<string, string> OrderByMap = new(StringComparer.OrdinalIgnoreCase)
        {
            ["MemberName"] = " order by MemberName ",
            ["MemberId"] = " order by substring(MemberId, 1,(len(MemberId)-charindex('-', MemberId))-1), MemberId ",
            ["LoanAccountNo"] = " order by substring(LoanAccountNo, 1,(len(LoanAccountNo)-charindex('-', LoanAccountNo))-1), LoanAccountNo",
            ["LoanTypeName"] = " order by LoanTypeName",
            ["LoanIssueOn"] = " order by L.LoanIssueOn ",
            ["VerifiedOn"] = " order by VerifiedOn ",
            ["VerifiedBy"] = " order by US.FullName "
        };

        private readonly AppDbContext _context;
        private readonly IDateConverterService _dateConverter;

        public LoanVerifiedUnverifiedRepository(AppDbContext context, IDateConverterService dateConverter)
        {
            _context = context;
            _dateConverter = dateConverter;
        }

        public async Task<LoanVerifiedUnverifiedData> GetReportDataAsync(LoanVerifiedUnverifiedRequestDto request)
        {
            // Report type: accepts "V" / "LoanVerified" and "U" / "UV" / "LoanUnverified" (case-insensitive)
            bool isVerified;
            var reportTypeInput = request.ReportType?.Trim();

            if (string.IsNullOrEmpty(reportTypeInput) ||
                reportTypeInput.Equals("V", StringComparison.OrdinalIgnoreCase) ||
                reportTypeInput.Equals("LoanVerified", StringComparison.OrdinalIgnoreCase))
            {
                isVerified = true;
            }
            else if (reportTypeInput.Equals("U", StringComparison.OrdinalIgnoreCase) ||
                     reportTypeInput.Equals("UV", StringComparison.OrdinalIgnoreCase) ||
                     reportTypeInput.Equals("LoanUnverified", StringComparison.OrdinalIgnoreCase))
            {
                isVerified = false;
            }
            else
            {
                throw new ArgumentException(
                    $"Invalid ReportType '{request.ReportType}'. Use 'V'/'LoanVerified' or 'U'/'LoanUnverified'.");
            }

            var branchIds = ParseBranchIds(request.BranchIds);

            var filter = new StringBuilder();

            // Date range (sequential: the converter shares the scoped DbContext)
            var fromDateBs = request.FromDateBs?.Trim();
            var toDateBs = request.ToDateBs?.Trim();

            if (!string.IsNullOrEmpty(fromDateBs) && fromDateBs != "-1" &&
                !string.IsNullOrEmpty(toDateBs) && toDateBs != "-1")
            {
                var fromDateAd = await _dateConverter.NepaliToEnglishAsync(fromDateBs);
                var toDateAd = await _dateConverter.NepaliToEnglishAsync(toDateBs);

                filter.Append(" And L.LoanIssueOn between '")
                      .Append(fromDateAd.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture))
                      .Append("' And '")
                      .Append(toDateAd.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture))
                      .Append("' ");
            }

            await using var connection = new SqlConnection(_context.Database.GetConnectionString());
            await connection.OpenAsync();

            // Member filter
            var memberKey = request.MemberRegistrationId?.Trim();
            if (!string.IsNullOrEmpty(memberKey) && memberKey != "-1")
            {
                var registrationId = long.TryParse(memberKey, out var id)
                    ? id
                    : await connection.QueryFirstOrDefaultAsync<long?>(
                        "SELECT MemMemberRegistrationId FROM MemMemberRegistration WHERE MemberId = @MemberId",
                        new { MemberId = memberKey });

                filter.Append(registrationId.HasValue
                    ? $" And MR.MemMemberRegistrationId = {registrationId.Value}"
                    : " And 1 = 0");
            }

            if (branchIds.Length > 0)
                filter.Append($" And L.UsmOfficeId in ({branchIds})");

            filter.Append($" And L.IsVerified = {(isVerified ? 1 : 0)} AND L.LmtLoanStatusId IN (1,2)");

            var orderKey = request.OrderBy?.Trim() ?? "";
            var parameters = new DynamicParameters();
            parameters.Add("@SqlFilterExp", filter.ToString(), DbType.String, size: -1);
            parameters.Add("@SqlFilterExpOrder", OrderByMap.GetValueOrDefault(orderKey, ""), DbType.String, size: -1);

            var rows = (await connection.QueryAsync<LoanVerifiedUnverifiedRowDto>(
                "sp_7_16_GetLoanVerifiedUnverifiedReport",
                parameters,
                commandType: CommandType.StoredProcedure,
                commandTimeout: 120)).AsList();

            var branchName = "All";
            if (branchIds.Length > 0)
            {
                var names = await connection.QueryAsync<string>(
                    $"SELECT OfficeName FROM UsmOffice WHERE UsmOfficeId IN ({branchIds})");
                var joined = string.Join(", ", names.Where(n => !string.IsNullOrWhiteSpace(n)));
                if (joined.Length > 0) branchName = joined;
            }

            return new LoanVerifiedUnverifiedData
            {
                Rows = rows,
                TotalRecords = rows.Count,
                TotalLoanIssueAmount = rows.Sum(r => r.LoanIssueAmount ?? 0),
                FromDateBs = request.FromDateBs,
                ToDateBs = request.ToDateBs,
                BranchName = branchName,
                ReportType = string.IsNullOrEmpty(reportTypeInput)
                    ? "LoanVerified"
                    : reportTypeInput,
                ReportTypeName = isVerified ? "Loan Verified Report" : "Loan Unverified Report",
                OrderBy = request.OrderBy
            };
        }

        private static string ParseBranchIds(string? branchIds)
            => string.IsNullOrWhiteSpace(branchIds)
                ? ""
                : string.Join(",", branchIds
                    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .Where(id => long.TryParse(id, out var value) && value > 0)
                    .Distinct());
    }
}