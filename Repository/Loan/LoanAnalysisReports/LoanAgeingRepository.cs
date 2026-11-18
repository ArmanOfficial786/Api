using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using NexgenCosysReport.DbContext;
using NexgenCosysReport.Dtos.RequestDtos.Loan.LoanAnalysisReport;
using NexgenCosysReport.Inteface.ServiceInterface.Common;
using NexgenCosysReport.Inteface.ServiceInterface.Loan.LoanAnalysisReport;
using System.Data;
using System.Text;

namespace NexgenCosysReport.Repository.Loan.LoanAnalysisReport
{
    public class LoanAgeingRepository : ILoanAgeingRepository
    {
        private readonly AppDbContext _context;
        private readonly IDateConverterService _dateConverter;
        private readonly ILogger<LoanAgeingRepository> _logger;

        public LoanAgeingRepository(
            AppDbContext context,
            IDateConverterService dateConverter,
            ILogger<LoanAgeingRepository> logger)
        {
            _context = context;
            _dateConverter = dateConverter;
            _logger = logger;
        }

        // CHANGED: only positive numeric ids are allowed (drops -1 / 0), duplicates removed
        private static string SanitizeBranchIds(string? branchIds)
        {
            if (string.IsNullOrWhiteSpace(branchIds) || branchIds == "-1" || branchIds == "string")
                return "-1";

            var validIds = branchIds
                .Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Where(id => long.TryParse(id, out var v) && v > 0)
                .Select(id => long.Parse(id).ToString())
                .Distinct()
                .ToList();

            return validIds.Count == 0 ? "-1" : string.Join(",", validIds);
        }

        private static long? ParseSingleId(string? id)
        {
            if (string.IsNullOrWhiteSpace(id) || id == "-1" || id == "string")
                return null;

            return long.TryParse(id.Trim(), out var value) && value != -1 ? value : null;
        }

        private static string ResolveAgeingOn(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return "P";

            var v = value.Trim().ToUpperInvariant().Replace(" ", "");

            if (v == "PI" || v == "P+I" || v == "P/PI" || v.Contains("INTEREST"))
                return "PI";

            return "P";
        }

        private static string ResolvePenaltyType(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return "S";

            var v = value.Trim().ToUpperInvariant();

            if (v == "R" || v.StartsWith("REMAINING"))
                return "R";

            if (v == "A" || v.StartsWith("AFTER"))
                return "A";

            return "S";
        }

        private static string ResolveShowLoanIssueDate(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return "ID";

            var v = value.Trim().ToUpperInvariant();

            if (v == "RD" || v.StartsWith("CURRENT") || v.Contains("RESCHEDULE"))
                return "RD";

            return "ID";
        }

        private static string BuildSqlOrderBy(string? orderBy)
        {
            if (string.IsNullOrEmpty(orderBy) || orderBy == "-1")
                return string.Empty;

            return orderBy.Trim() switch
            {
                "MemberId" => " order by substring(MemberId, 1,(len(MemberId)-charindex('-', MemberId))-1), MemberId ",
                "FullName" => " order by FullName",
                "LoanAccountNo" => " order by substring(LoanAccountNo, 1,(len(LoanAccountNo)-charindex('-', LoanAccountNo))-1), LoanAccountNo ",
                "LoanIssueAmount" => " order by LoanIssueAmount DESC",
                _ => string.Empty
            };
        }

        private static string GetPenaltyTypeName(string penaltyType)
        {
            return penaltyType switch
            {
                "R" => "Remaining Principal",
                "A" => "After Maturity",
                _ => "Schedule Wise"
            };
        }

        private static string GetAgeingOnName(string ageingOn)
        {
            return ageingOn == "PI" ? "Principle + Interest" : "Principle";
        }

        private static string GetShowLoanIssueDateName(string showLoanIssueDate)
        {
            return showLoanIssueDate == "RD" ? "Current ReSchedule Date" : "Issue Date";
        }

        private async Task<List<LoanAgeingRowDto>> GetAgeingDataAsync(
            SqlConnection connection,
            string tillDateStr,
            string sqlFilterExpBranchId,
            string sqlFilterDate,
            string sqlFilterExpOrderby,
            string sqlPenaltyType,
            string sqlShowIssueDate,
            string sqlAgeingReportPI)
        {
            var parameters = new DynamicParameters();
            parameters.Add("@SqlFilterExp", tillDateStr, DbType.String, size: -1);
            parameters.Add("@SqlFilterExpbranchId", sqlFilterExpBranchId, DbType.String, size: -1);
            parameters.Add("@SqlFilterExpOrderby", sqlFilterExpOrderby, DbType.String, size: -1);
            parameters.Add("@SqlFilterDate", sqlFilterDate, DbType.String, size: -1);
            parameters.Add("@SqlPenaltyType", sqlPenaltyType, DbType.String, size: -1);
            parameters.Add("@SqlShowIssueDate", sqlShowIssueDate, DbType.String, size: -1);
            parameters.Add("@SqlAgeingReportPI", sqlAgeingReportPI, DbType.String, size: -1);

            var rows = await connection.QueryAsync<LoanAgeingRowDto>(
                "sp_7_16_LoanAgeingReport",
                parameters,
                commandType: CommandType.StoredProcedure,
                commandTimeout: 300
            );

            return rows.AsList();
        }

        public async Task<LoanAgeingData> GetReportDataAsync(LoanAgeingRequestDto request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.TillDateBs) || request.TillDateBs == "-1")
                {
                    throw new ArgumentException("Till Date is required.");
                }

                if (string.IsNullOrWhiteSpace(request.BranchIds))
                {
                    throw new ArgumentException("Please select Branch Name");
                }

                var penaltyType = ResolvePenaltyType(request.PenaltyType);
                var ageingOn = ResolveAgeingOn(request.AgeingOn);
                var showLoanIssueDate = ResolveShowLoanIssueDate(request.ShowLoanIssueDate);

                var branchIds = SanitizeBranchIds(request.BranchIds);
                var memberGroupId = ParseSingleId(request.MemberGroupId);
                var collectorId = ParseSingleId(request.CollectorId);

                var connectionString = _context.Database.GetConnectionString();
                if (string.IsNullOrEmpty(connectionString))
                {
                    throw new InvalidOperationException("Database connection string is not configured.");
                }

                using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();

                var tillDateAd = await _dateConverter.NepaliToEnglishAsync(request.TillDateBs);
                var tillDateStr = $"'{tillDateAd:yyyy-MM-dd}'";

                var sqlFilterExpBranchId = new StringBuilder();
                if (branchIds != "-1")
                {
                    sqlFilterExpBranchId.Append(" And v.UsmOfficeId in (").Append(branchIds).Append(")");
                }

                if (memberGroupId.HasValue)
                {
                    sqlFilterExpBranchId.Append(" AND MR.SycMemberGroupId = ").Append(memberGroupId.Value);
                }

                if (collectorId.HasValue)
                {
                    sqlFilterExpBranchId.Append(" and LS.HurCollectorId = ").Append(collectorId.Value);
                }

                var sqlFilterExpOrderby = BuildSqlOrderBy(request.OrderBy);
                var sqlPenaltyType = $"'{penaltyType}'";
                var sqlShowIssueDate = $"'{showLoanIssueDate}'";
                var sqlAgeingReportPI = $"'{ageingOn}'";
                var branchFilter = sqlFilterExpBranchId.ToString();

                var sections = new List<LoanAgeingSectionDto>
                {
                    new() { SectionName = "0 Days (Good Loan)" },
                    new() { SectionName = "1 to 90 Days" },
                    new() { SectionName = "91 to 180 Days" },
                    new() { SectionName = "181 to 365 Days" },
                    new() { SectionName = "Greater than 365 Days" }
                };

                var bucketFilters = new[]
                {
                    "  And DueDays <= 0 ",
                    " And DueDays >= 1 and DueDays <= 90 ",
                    " And DueDays >= 91 and DueDays <= 180 ",
                    " And DueDays >= 181 and DueDays <= 365 ",
                    " And DueDays > 365 "
                };

                for (var i = 0; i < sections.Count; i++)
                {
                    sections[i].Rows = await GetAgeingDataAsync(
                        connection, tillDateStr, branchFilter,
                        bucketFilters[i], sqlFilterExpOrderby, sqlPenaltyType,
                        sqlShowIssueDate, sqlAgeingReportPI);
                }

                foreach (var section in sections)
                {
                    section.TotalRecords = section.Rows.Count;
                    section.TotalLoanIssueAmount = section.Rows.Sum(r => r.LoanIssueAmount ?? 0);
                    section.TotalOverDue = section.Rows.Sum(r => r.OverDue ?? 0);
                    section.TotalPrincipalAmount = section.Rows.Sum(r => r.TotalPrincipalAmount ?? 0);
                }

                var allRows = sections.SelectMany(s => s.Rows).ToList();

                // CHANGED: Nepali report uses OfficeNameNepali (as the WebForms Nepali page does)
                string branchName = "All";
                long companyBranchId = -1;
                if (branchIds != "-1")
                {
                    var officeSql = request.IsNepaliReport
                        ? $"SELECT COALESCE(NULLIF(LTRIM(RTRIM(OfficeNameNepali)), ''), OfficeName) FROM UsmOffice WHERE UsmOfficeId IN ({branchIds})"
                        : $"SELECT OfficeName FROM UsmOffice WHERE UsmOfficeId IN ({branchIds})";

                    var names = await connection.QueryAsync<string>(officeSql);
                    var nameList = names.Where(n => !string.IsNullOrWhiteSpace(n)).ToList();
                    branchName = nameList.Count > 0 ? string.Join(", ", nameList) : "All";

                    var idParts = branchIds.Split(',');
                    if (idParts.Length == 1)
                    {
                        companyBranchId = long.Parse(idParts[0]);
                    }
                }

                string? memberGroupName = null;
                if (memberGroupId.HasValue)
                {
                    memberGroupName = await connection.QueryFirstOrDefaultAsync<string>(
                        "SELECT Name FROM SycMemberGroup WHERE SycMemberGroupId = @Id",
                        new { Id = memberGroupId.Value });
                }

                string? collectorName = null;
                if (collectorId.HasValue)
                {
                    collectorName = await connection.QueryFirstOrDefaultAsync<string>(
                        "SELECT CollectorFullName FROM HurCollector WHERE HurCollectorId = @Id",
                        new { Id = collectorId.Value });
                }

                return new LoanAgeingData
                {
                    Sections = sections,
                    TotalRecords = allRows.Count,
                    TotalLoanIssueAmount = allRows.Sum(r => r.LoanIssueAmount ?? 0),
                    TotalOverDue = allRows.Sum(r => r.OverDue ?? 0),
                    TotalPrincipalAmount = allRows.Sum(r => r.TotalPrincipalAmount ?? 0),
                    TillDateBs = request.TillDateBs,
                    BranchName = branchName,
                    MemberGroupName = memberGroupName,
                    CollectorName = collectorName,
                    PenaltyType = penaltyType,
                    PenaltyTypeName = GetPenaltyTypeName(penaltyType),
                    AgeingOn = ageingOn,
                    AgeingOnName = GetAgeingOnName(ageingOn),
                    ShowLoanIssueDate = showLoanIssueDate,
                    ShowLoanIssueDateName = GetShowLoanIssueDateName(showLoanIssueDate),
                    OrderBy = request.OrderBy,
                    IsNepaliReport = request.IsNepaliReport,
                    CompanyBranchId = companyBranchId
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