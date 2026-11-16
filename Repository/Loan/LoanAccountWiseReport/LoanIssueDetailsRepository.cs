using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using NexgenCosysReport.DbContext;
using NexgenCosysReport.Dtos.RequestDtos.Loan.LoanAccountWiseReport;
using NexgenCosysReport.Inteface.ServiceInterface.Common;
using NexgenCosysReport.Inteface.ServiceInterface.Loan.LoanAccountWiseReport;
using System.Data;
using System.Text;

namespace NexgenCosysReport.Repository.Loan.LoanAccountWiseReport
{
    public class LoanIssueDetailsRepository : ILoanIssueDetailsRepository
    {
        private readonly AppDbContext _context;
        private readonly IDateConverterService _dateConverter;
        private readonly ILogger<LoanIssueDetailsRepository> _logger;

        private const string DefaultReportType = "LIR";

        // Report type -> stored procedure (same as the webform)
        private static readonly Dictionary<string, string> ProcedureMap = new(StringComparer.OrdinalIgnoreCase)
        {
            { "LoanIssueReport",  "sp_7_16_LoanIssueReport" },
            { "LoanIssueReportReschedule", "sp_7_16_RescheduleLoanIssueReport" }
        };

        // "Order By" dropdown value -> SQL (identical to the webform)
        private static readonly Dictionary<string, string> OrderByMap = new(StringComparer.OrdinalIgnoreCase)
        {
            { "MemberId",        "substring(MemberId, 1,(len(MemberId)-charindex('-', MemberId))-1), MemberId " },
            { "FullName",        "FullName" },
            { "LoanAccountNo",   "substring(LoanAccountNo, 1,(len(LoanAccountNo)-charindex('-', LoanAccountNo))-1), LoanAccountNo " },
            { "LoanTypeName",    "LoanTypeName" },
            { "LoanIssueAmount", "LoanIssueAmount DESC" },
            { "LoanIssueDate",   "LoanIssueDate" },
            { "InterestRate",    "InterestRate DESC" }
        };

        public LoanIssueDetailsRepository(
            AppDbContext context,
            IDateConverterService dateConverter,
            ILogger<LoanIssueDetailsRepository> logger)
        {
            _context = context;
            _dateConverter = dateConverter;
            _logger = logger;
        }

        // blank -> LIR (webform default); anything unknown -> 400
        private static string ResolveProcedureName(string? reportType, out string resolvedType)
        {
            resolvedType = string.IsNullOrWhiteSpace(reportType)
                ? DefaultReportType
                : reportType.Trim().ToUpperInvariant();

            if (!ProcedureMap.TryGetValue(resolvedType, out var procedureName))
                throw new ArgumentException($"Unknown ReportType '{reportType}'. Use LIR or LIRR.");

            return procedureName;
        }

        private static string BuildSqlOrderBy(string? orderBy)
        {
            if (string.IsNullOrWhiteSpace(orderBy))
                return string.Empty;

            return OrderByMap.TryGetValue(orderBy.Trim(), out var clause)
                ? " order by " + clause
                : string.Empty;
        }

        // "1, 2 ,x,3" -> "1,2,3"; empty / -1 / junk -> "-1" (= no filter)
        private static string SanitizeIdList(string? ids)
        {
            if (string.IsNullOrWhiteSpace(ids) || ids == "-1" || ids == "string")
                return "-1";

            var validIds = ids
                .Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Where(id => long.TryParse(id, out _));

            var joined = string.Join(",", validIds);
            return string.IsNullOrEmpty(joined) ? "-1" : joined;
        }

        public async Task<LoanIssueDetailsData> GetReportDataAsync(LoanIssueDetailsRequestDto request)
        {
            try
            {
                var procedureName = ResolveProcedureName(request.ReportType, out var resolvedType);

                var branchIds = SanitizeIdList(request.BranchIds);
                var collectionCenterIds = SanitizeIdList(request.CollectionCenterIds);

                var connectionString = _context.Database.GetConnectionString();
                using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();

                var sqlFilterExp = new StringBuilder();
                string? memberName = null;

                // ── Mode 1: by member id (dates are ignored, like the webform) ──
                if (!string.IsNullOrWhiteSpace(request.MemberId))
                {
                    var memberId = request.MemberId.Trim();

                    // The procedure builds dynamic SQL from this text, so it must be a literal,
                    // not a parameter. Escape ' so it cannot break out of the string.
                    sqlFilterExp.Append(" And MR.MemberId = '")
                                .Append(memberId.Replace("'", "''"))
                                .Append("'");

                    memberName = await connection.QueryFirstOrDefaultAsync<string>(
                        @"SELECT LTRIM(RTRIM(REPLACE(
                                 ISNULL(FirstName, '') + ' ' + ISNULL(MiddleName, '') + ' ' + ISNULL(LastName, ''),
                                 '  ', ' ')))
                          FROM MemMemberRegistration WHERE MemberId = @MemberId",
                        new { MemberId = memberId });
                }
                // ── Mode 2: by issue date range (Nepali dates -> AD) ──
                else if (!string.IsNullOrEmpty(request.FromDateBs) && !string.IsNullOrEmpty(request.ToDateBs))
                {
                    var fromDateAd = await _dateConverter.NepaliToEnglishAsync(request.FromDateBs);
                    var toDateAd = await _dateConverter.NepaliToEnglishAsync(request.ToDateBs);

                    sqlFilterExp.Append(" And ls.LoanIssueOn between '")
                                .Append(fromDateAd.ToString("yyyy-MM-dd")).Append("' And '")
                                .Append(toDateAd.ToString("yyyy-MM-dd")).Append("' ");
                }
                else
                {
                    throw new ArgumentException("Either MemberId or both FromDateBs and ToDateBs must be supplied.");
                }

                // ── Branch + collection center apply to both modes (same as the webform) ──
                if (branchIds != "-1")
                    sqlFilterExp.Append(" And LS.UsmOfficeId in(").Append(branchIds).Append(")");

                // Webform filters by the selected centers whenever the list is not -1.
                // EnableCollectionCenter only controls grouping, not filtering.
                if (collectionCenterIds != "-1")
                    sqlFilterExp.Append(" And LS.SycCollectionCenterId in(").Append(collectionCenterIds).Append(")");

                var parameters = new DynamicParameters();
                parameters.Add("@SqlFilterExp", sqlFilterExp.ToString(), DbType.String, size: -1);
                parameters.Add("@SqlFilterExpOrderBy", BuildSqlOrderBy(request.OrderBy), DbType.String, size: -1);

                var rows = await connection.QueryAsync<LoanIssueDetailsRowDto>(
                    procedureName,
                    parameters,
                    commandType: CommandType.StoredProcedure,
                    commandTimeout: 120);

                var resultList = rows.AsList();

                var branchName = "All";
                if (branchIds != "-1")
                {
                    var names = await connection.QueryAsync<string>(
                        $"SELECT OfficeName FROM UsmOffice WHERE UsmOfficeId IN ({branchIds})");
                    var nameList = names.Where(n => !string.IsNullOrWhiteSpace(n)).ToList();
                    branchName = nameList.Count > 0 ? string.Join(", ", nameList) : "All";
                }

                return new LoanIssueDetailsData
                {
                    Rows = resultList,
                    TotalRecords = resultList.Count,
                    MemberId = request.MemberId,
                    MemberName = memberName,
                    FromDateBs = request.FromDateBs,
                    ToDateBs = request.ToDateBs,
                    BranchName = branchName,
                    ReportType = resolvedType,
                    OrderBy = request.OrderBy,
                    EnableCollectionCenter = request.EnableCollectionCenter
                };
            }
            catch (ArgumentException)
            {
                throw; // bad input, not a server fault: the controller returns 400
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetReportDataAsync");
                throw;
            }
        }
    }
}