
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
        private static readonly Dictionary<string, string> ProcedureMap = new(StringComparer.OrdinalIgnoreCase)
        {
            { "LIR", "sp_7_16_LoanIssueReport" },
            { "LIRR", "sp_7_16_RescheduleLoanIssueReport" }
        };


        private static readonly Dictionary<string, string> OrderByMap = new(StringComparer.OrdinalIgnoreCase)
        {
            { "MemberId", "substring(MemberId, 1,(len(MemberId)-charindex('-', MemberId))-1), MemberId " },
            { "FullName", "FullName" },
            { "LoanAccountNo", "substring(LoanAccountNo, 1,(len(LoanAccountNo)-charindex('-', LoanAccountNo))-1), LoanAccountNo " },
            { "LoanTypeName", "LoanTypeName" },
            { "LoanIssueAmount", "LoanIssueAmount DESC" },
            { "LoanIssueDate", "LoanIssueDate" },
            { "InterestRate", "InterestRate DESC" }
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

        private string ResolveProcedureName(string? reportType, out string resolvedType)
        {
            if (!string.IsNullOrWhiteSpace(reportType) && ProcedureMap.TryGetValue(reportType, out var procedureName))
            {
                resolvedType = reportType;
                return procedureName;
            }

            _logger.LogWarning(
                "LoanIssueDetails: Unrecognized report type '{ReportType}' — falling back to '{Default}'.",
                reportType, DefaultReportType);

            resolvedType = DefaultReportType;
            return ProcedureMap[DefaultReportType];
        }

        private static string BuildSqlOrderBy(string? orderBy)
        {
            if (string.IsNullOrWhiteSpace(orderBy))
                return string.Empty;

            return OrderByMap.TryGetValue(orderBy.Trim(), out var clause)
                ? " order by " + clause
                : string.Empty;
        }

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


                if (!string.IsNullOrWhiteSpace(request.MemberId))
                {

                    sqlFilterExp.Append(" And MR.MemberId = @MemberIdFilter");

                    var name = await connection.QueryFirstOrDefaultAsync<string>(
                        @"SELECT FirstName + ' ' +
                                 CASE WHEN MiddleName = '' THEN '' ELSE MiddleName + ' ' END +
                                 LastName
                          FROM MemMemberRegistration WHERE MemberId = @MemberId",
                        new { MemberId = request.MemberId.Trim() });
                    memberName = name;
                }
                else if (!string.IsNullOrEmpty(request.FromDateBs) && !string.IsNullOrEmpty(request.ToDateBs))
                {
                    var fromDateAd = await _dateConverter.NepaliToEnglishAsync(request.FromDateBs);
                    var toDateAd = await _dateConverter.NepaliToEnglishAsync(request.ToDateBs);

                    var fromDateStr = fromDateAd.ToString("yyyy-MM-dd");
                    var toDateStr = toDateAd.ToString("yyyy-MM-dd");


                    sqlFilterExp.Append(" And ls.LoanIssueOn between '")
                                .Append(fromDateStr).Append("' And '")
                                .Append(toDateStr).Append("' ");
                }
                else
                {
                    throw new ArgumentException("Either MemberId or both FromDateBs and ToDateBs must be supplied.");
                }

                if (branchIds != "-1")
                {
                    sqlFilterExp.Append(" And LS.UsmOfficeId in(").Append(branchIds).Append(")");
                }

                if (request.EnableCollectionCenter && collectionCenterIds != "-1")
                {
                    sqlFilterExp.Append(" And LS.SycCollectionCenterId in(").Append(collectionCenterIds).Append(")");
                }

                var sqlFilterExpOrderBy = BuildSqlOrderBy(request.OrderBy);

                var parameters = new DynamicParameters();
                parameters.Add("@SqlFilterExp", sqlFilterExp.ToString(), DbType.String, size: -1);
                parameters.Add("@SqlFilterExpOrderBy", sqlFilterExpOrderBy, DbType.String, size: -1);

                if (!string.IsNullOrWhiteSpace(request.MemberId))
                {
                    parameters.Add("@MemberIdFilter", request.MemberId.Trim());
                }

                var rows = await connection.QueryAsync<LoanIssueDetailsRowDto>(
                    procedureName,
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
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetReportDataAsync");
                throw;
            }
        }
    }
}