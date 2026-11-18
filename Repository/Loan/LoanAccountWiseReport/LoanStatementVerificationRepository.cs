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
    public class LoanStatementVerificationRepository : ILoanStatementVerificationRepository
    {
        // Alias returned by sp_7_16_GetLoanStatementVerification (lsv.VerifiedToDateOnBs as PassBookVerifyTill)
        private const string VerifiedTillColumn = "PassBookVerifyTill";

        private readonly AppDbContext _context;
        private readonly IDateConverterService _dateConverter;
        private readonly ILogger<LoanStatementVerificationRepository> _logger;

        public LoanStatementVerificationRepository(
            AppDbContext context,
            IDateConverterService dateConverter,
            ILogger<LoanStatementVerificationRepository> logger)
        {
            _context = context;
            _dateConverter = dateConverter;
            _logger = logger;
        }

        // Same mapping as CLmtLoanIssueReports.GetLoanStatementVerification,
        // using the SP's real output column names
        private static string BuildSqlOrderBy(string? orderBy)
        {
            if (string.IsNullOrWhiteSpace(orderBy) || orderBy == "-1")
                return string.Empty;

            return orderBy.Trim() switch
            {
                "Member Name" => " order by MemberName ",
                "Member Id" => " order by substring(MemberId, 1,(len(MemberId)-charindex('-', MemberId))-1), MemberId  ",
                "Account No" => " order by substring(LoanAccountNo, 1,(len(LoanAccountNo)-charindex('-', LoanAccountNo))-1), LoanAccountNo",
                "Verified Till" => " order by " + VerifiedTillColumn + " ",
                "Verified Date" => " order by VerifiedDate ",
                "Verified By" => " order by VerifiedBy ",
                _ => string.Empty
            };
        }

        // "1, 2,3" -> [1,2,3]; ignores "-1", "string" and non-numeric values
        private static List<long> ParseIds(string? csv)
        {
            if (string.IsNullOrWhiteSpace(csv))
                return new List<long>();

            return csv
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(s => long.TryParse(s, out var id) ? id : 0)
                .Where(id => id > 0)
                .Distinct()
                .ToList();
        }

        // Swagger placeholders and blanks mean "not supplied"
        private static string? CleanText(string? value)
        {
            if (string.IsNullOrWhiteSpace(value)) return null;
            var v = value.Trim();
            return v == "-1" || v.Equals("string", StringComparison.OrdinalIgnoreCase) ? null : v;
        }

        public async Task<LoanStatementVerificationData> GetReportDataAsync(
            LoanStatementVerificationRequestDto request,
            long userId)
        {
            try
            {
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

                var sqlFilterExp = new StringBuilder();
                string? memberName = null;
                string? collectorName = null;
                string? fromDateBs = null;
                string? toDateBs = null;

                var memberId = CleanText(request.MemberId);

                if (memberId != null)
                {
                    // btnViewByMemberId_Click: member only, no dates, no collector.
                    // MemberId is text (e.g. MR-01-202): escape quotes and wrap in quotes.
                    sqlFilterExp.Append(" And MR.MemberId = '").Append(memberId.Replace("'", "''")).Append("'");

                    memberName = await connection.QueryFirstOrDefaultAsync<string>(
                        @"SELECT FirstName + ' ' +
                                 CASE WHEN ISNULL(MiddleName,'') = '' THEN '' ELSE MiddleName + ' ' END +
                                 LastName
                          FROM MemMemberRegistration
                          WHERE MemberId = @MemberId",
                        new { MemberId = memberId });
                }
                else
                {
                    // btnViewByDate_Click: dates required, collector optional
                    fromDateBs = request.FromDateBs;
                    toDateBs = request.ToDateBs;

                    var fromDateAd = await _dateConverter.NepaliToEnglishAsync(fromDateBs!);
                    var toDateAd = await _dateConverter.NepaliToEnglishAsync(toDateBs!);

                    sqlFilterExp.Append(" And lsv.VerifiedDateOn between '").Append(fromDateAd.ToString("yyyy-MM-dd"))
                                .Append("' And '").Append(toDateAd.ToString("yyyy-MM-dd")).Append("' ");

                    if (request.CollectorId > 0)
                    {
                        sqlFilterExp.Append(" And li.HurCollectorId = ").Append(request.CollectorId);

                        collectorName = await connection.QueryFirstOrDefaultAsync<string>(
                            "SELECT CollectorFullName FROM HurCollector WHERE HurCollectorId = @Id",
                            new { Id = request.CollectorId });
                    }
                }

                // Offices (both modes). In the SP, "us" is UsmUser, so this matches the verifier's office, as in the webform.
                sqlFilterExp.Append(" And us.UsmOfficeId in (").Append(string.Join(",", officeIds)).Append(")");

                var sqlFilterExpOrder = BuildSqlOrderBy(request.OrderBy);

                _logger.LogInformation("SP sp_7_16_GetLoanStatementVerification | exp={Exp} | order={Order}",
                    sqlFilterExp, sqlFilterExpOrder);

                var parameters = new DynamicParameters();
                parameters.Add("@SqlFilterExp", sqlFilterExp.ToString(), DbType.String, size: -1);
                parameters.Add("@SqlFilterExpOrder", sqlFilterExpOrder, DbType.String, size: -1);

                var rows = (await connection.QueryAsync<LoanStatementVerificationRowDto>(
                    "sp_7_16_GetLoanStatementVerification",
                    parameters,
                    commandType: CommandType.StoredProcedure,
                    commandTimeout: 120
                )).AsList();

                // Header: names of the selected offices
                var names = await connection.QueryAsync<string>(
                    "SELECT OfficeName FROM UsmOffice WHERE UsmOfficeId IN @Ids",
                    new { Ids = officeIds });
                var nameList = names.Where(n => !string.IsNullOrWhiteSpace(n)).ToList();
                var branchName = nameList.Count > 0 ? string.Join(", ", nameList) : "All";

                return new LoanStatementVerificationData
                {
                    Rows = rows,
                    TotalRecords = rows.Count,
                    FromDateBs = fromDateBs,
                    ToDateBs = toDateBs,
                    BranchName = branchName,
                    MemberName = memberName,
                    CollectorName = collectorName,
                    OrderBy = request.OrderBy
                };
            }
            catch (UnauthorizedAccessException)
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