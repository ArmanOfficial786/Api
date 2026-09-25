
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
    public class LoanVerifiedUnverifiedRepository : ILoanVerifiedUnverifiedRepository
    {
        private readonly AppDbContext _context;
        private readonly IDateConverterService _dateConverter;
        private readonly ILogger<LoanVerifiedUnverifiedRepository> _logger;

        public LoanVerifiedUnverifiedRepository(
            AppDbContext context,
            IDateConverterService dateConverter,
            ILogger<LoanVerifiedUnverifiedRepository> logger)
        {
            _context = context;
            _dateConverter = dateConverter;
            _logger = logger;
        }


        private static string BuildSqlOrderBy(LoanVerifiedUnverifiedRequestDto request)
        {
            if (string.IsNullOrEmpty(request.OrderBy) || request.OrderBy == "-1")
                return string.Empty;

            return request.OrderBy.Trim() switch
            {
                "MemberName" => " order by MemberName ",
                "MemberId" => " order by substring(MemberId, 1,(len(MemberId)-charindex('-', MemberId))-1), MemberId  ",
                "LoanAccountNo" => " order by substring(LoanAccountNo, 1,(len(LoanAccountNo)-charindex('-', LoanAccountNo))-1), LoanAccountNo",
                "LoanTypeName" => " order by LoanTypeName",
                "LoanIssueOn" => " order by L.LoanIssueOn ",
                "VerifiedOn" => " order by VerifiedOn ",
                "VerifiedBy" => " order by US.FullName ",
                _ => string.Empty
            };
        }

        private static string SanitizeBranchIds(string? branchIds)
        {
            if (string.IsNullOrWhiteSpace(branchIds) || branchIds == "-1" || branchIds == "string")
                return string.Empty;

            var validIds = branchIds
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Where(id => long.TryParse(id, out _));

            return string.Join(",", validIds);
        }

        public async Task<LoanVerifiedUnverifiedData> GetReportDataAsync(LoanVerifiedUnverifiedRequestDto request)
        {
            try
            {
                var branchIds = SanitizeBranchIds(request.BranchIds);

                var connectionString = _context.Database.GetConnectionString();
                using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();

                var sqlFilterExp = new StringBuilder();

                if (!string.IsNullOrWhiteSpace(request.MemberRegistrationId) &&
                    request.MemberRegistrationId != "-1" &&
                    request.MemberRegistrationId != "")
                {
                    sqlFilterExp.Append(" And MR.MemMemberRegistrationId = ").Append(request.MemberRegistrationId);
                }

                if (!string.IsNullOrEmpty(request.FromDateBs) && !string.IsNullOrEmpty(request.ToDateBs)
                    && request.FromDateBs != "-1" && request.ToDateBs != "-1")
                {
                    var fromDateAd = await _dateConverter.NepaliToEnglishAsync(request.FromDateBs);
                    var toDateAd = await _dateConverter.NepaliToEnglishAsync(request.ToDateBs);

                    var fromDateStr = fromDateAd.ToString("yyyy-MM-dd");
                    var toDateStr = toDateAd.ToString("yyyy-MM-dd");

                    sqlFilterExp.Append(" And L.LoanIssueOn between '").Append(fromDateStr)
                                .Append("' And '").Append(toDateStr).Append("' ");
                }

                if (!string.IsNullOrEmpty(branchIds))
                {
                    sqlFilterExp.Append(" And L.UsmOfficeId in (").Append(branchIds).Append(")");
                }


                if (request.ReportType == "V")
                {
                    sqlFilterExp.Append(" And L.IsVerified=1  AND L.LmtLoanStatusId IN (1,2)");
                }
                else if (request.ReportType == "U")
                {
                    sqlFilterExp.Append(" And L.IsVerified=0  AND L.LmtLoanStatusId IN (1,2)");
                }

                var sqlFilterExpOrder = BuildSqlOrderBy(request);

                var parameters = new DynamicParameters();
                parameters.Add("@SqlFilterExp", sqlFilterExp.ToString(), DbType.String, size: -1);
                parameters.Add("@SqlFilterExpOrder", sqlFilterExpOrder, DbType.String, size: -1);

                var rows = await connection.QueryAsync<LoanVerifiedUnverifiedRowDto>(
                    "sp_7_16_GetLoanVerifiedUnverifiedReport",
                    parameters,
                    commandType: CommandType.StoredProcedure,
                    commandTimeout: 120
                );

                var resultList = rows.AsList();


                string branchName = "All";
                if (!string.IsNullOrEmpty(branchIds))
                {
                    var names = await connection.QueryAsync<string>(
                        $"SELECT OfficeName FROM UsmOffice WHERE UsmOfficeId IN ({branchIds})");
                    var nameList = names.Where(n => !string.IsNullOrWhiteSpace(n)).ToList();
                    branchName = nameList.Count > 0 ? string.Join(", ", nameList) : "All";
                }

                var reportTypeName = request.ReportType == "V" ? "Loan Verified Report" : "Loan Unverified Report";

                return new LoanVerifiedUnverifiedData
                {
                    Rows = resultList,
                    TotalRecords = resultList.Count,
                    TotalLoanIssueAmount = resultList.Sum(r => r.LoanIssueAmount ?? 0),
                    FromDateBs = request.FromDateBs,
                    ToDateBs = request.ToDateBs,
                    BranchName = branchName,
                    ReportType = request.ReportType,
                    ReportTypeName = reportTypeName,
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