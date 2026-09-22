// Repository/Loan/LoanAnalysisReport/LoanAgeingRepository.cs
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

        private static string SanitizeBranchIds(string? branchIds)
        {
            if (string.IsNullOrWhiteSpace(branchIds) || branchIds == "-1" || branchIds == "string")
                return "-1";

            var validIds = branchIds
                .Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Where(id => long.TryParse(id, out _))
                .ToList();

            var joined = string.Join(",", validIds);
            return string.IsNullOrEmpty(joined) ? "-1" : joined;
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

        private static string GetPenaltyTypeName(string? penaltyType)
        {
            return penaltyType?.Trim().ToUpper() switch
            {
                "R" => "Remaining Principal",
                "A" => "After Maturity",
                "S" => "Schedule Wise",
                _ => "Schedule Wise"
            };
        }

        private static string GetAgeingOnName(string? ageingOn)
        {
            return ageingOn?.Trim().ToUpper() switch
            {
                "PI" => "Principle + Interest",
                "P" => "Principle",
                _ => "Principle"
            };
        }

        private static string GetShowLoanIssueDateName(string? showLoanIssueDate)
        {
            return showLoanIssueDate?.Trim().ToUpper() switch
            {
                "RD" => "Current ReSchedule Date",
                "ID" => "Issue Date",
                _ => "Issue Date"
            };
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

                var branchIds = SanitizeBranchIds(request.BranchIds);

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

                if (!string.IsNullOrEmpty(request.MemberGroupId) &&
                    request.MemberGroupId != "-1" &&
                    request.MemberGroupId != "string" &&
                    long.TryParse(request.MemberGroupId, out var memberGroupId))
                {
                    sqlFilterExpBranchId.Append(" AND MR.SycMemberGroupId = ").Append(memberGroupId);
                }

                if (!string.IsNullOrEmpty(request.CollectorId) && request.CollectorId != "-1")
                {
                    sqlFilterExpBranchId.Append(" and LS.HurCollectorId = ").Append(request.CollectorId);
                }

                var sqlFilterExpOrderby = BuildSqlOrderBy(request.OrderBy);
                var sqlPenaltyType = $"'{request.PenaltyType.Trim().ToUpper()}'";
                var sqlShowIssueDate = $"'{request.ShowLoanIssueDate.Trim().ToUpper()}'";
                var sqlAgeingReportPI = $"'{request.AgeingOn.Trim().ToUpper()}'";

                var sections = new List<LoanAgeingSectionDto>
                {
                    new() { SectionName = "0 Days (Good Loan)" },
                    new() { SectionName = "1 to 90 Days" },
                    new() { SectionName = "91 to 180 Days" },
                    new() { SectionName = "181 to 365 Days" },
                    new() { SectionName = "Greater than 365 Days" }
                };

                sections[0].Rows = await GetAgeingDataAsync(
                    connection, tillDateStr, sqlFilterExpBranchId.ToString(),
                    " And DueDays <= 0 ", sqlFilterExpOrderby, sqlPenaltyType,
                    sqlShowIssueDate, sqlAgeingReportPI);

                sections[1].Rows = await GetAgeingDataAsync(
                    connection, tillDateStr, sqlFilterExpBranchId.ToString(),
                    " And DueDays >= 1 and DueDays <= 90 ", sqlFilterExpOrderby, sqlPenaltyType,
                    sqlShowIssueDate, sqlAgeingReportPI);

                sections[2].Rows = await GetAgeingDataAsync(
                    connection, tillDateStr, sqlFilterExpBranchId.ToString(),
                    " And DueDays >= 91 and DueDays <= 180 ", sqlFilterExpOrderby, sqlPenaltyType,
                    sqlShowIssueDate, sqlAgeingReportPI);

                sections[3].Rows = await GetAgeingDataAsync(
                    connection, tillDateStr, sqlFilterExpBranchId.ToString(),
                    " And DueDays >= 181 and DueDays <= 365 ", sqlFilterExpOrderby, sqlPenaltyType,
                    sqlShowIssueDate, sqlAgeingReportPI);

                sections[4].Rows = await GetAgeingDataAsync(
                    connection, tillDateStr, sqlFilterExpBranchId.ToString(),
                    " And DueDays > 365 ", sqlFilterExpOrderby, sqlPenaltyType,
                    sqlShowIssueDate, sqlAgeingReportPI);

                foreach (var section in sections)
                {
                    section.TotalRecords = section.Rows.Count;
                    section.TotalLoanIssueAmount = section.Rows.Sum(r => r.LoanIssueAmount ?? 0);
                    section.TotalOverDue = section.Rows.Sum(r => r.OverDue ?? 0);
                    section.TotalPrincipalAmount = section.Rows.Sum(r => r.TotalPrincipalAmount ?? 0);
                }

                var allRows = sections.SelectMany(s => s.Rows).ToList();

                string branchName = "All";
                if (branchIds != "-1")
                {
                    var names = await connection.QueryAsync<string>(
                        $"SELECT OfficeName FROM UsmOffice WHERE UsmOfficeId IN ({branchIds})");
                    var nameList = names.Where(n => !string.IsNullOrWhiteSpace(n)).ToList();
                    branchName = nameList.Count > 0 ? string.Join(", ", nameList) : "All";
                }

                string? memberGroupName = null;
                if (!string.IsNullOrEmpty(request.MemberGroupId) &&
                    request.MemberGroupId != "-1" &&
                    request.MemberGroupId != "string" &&
                    long.TryParse(request.MemberGroupId, out var memberGroupIdForName))
                {
                    memberGroupName = await connection.QueryFirstOrDefaultAsync<string>(
                        "SELECT Name FROM SycMemberGroup WHERE SycMemberGroupId = @Id",
                        new { Id = memberGroupIdForName });
                }

                string? collectorName = null;
                if (!string.IsNullOrEmpty(request.CollectorId) && request.CollectorId != "-1")
                {
                    collectorName = await connection.QueryFirstOrDefaultAsync<string>(
                        "SELECT CollectorFullName FROM HurCollector WHERE HurCollectorId = @Id",
                        new { Id = request.CollectorId });
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
                    PenaltyType = request.PenaltyType,
                    PenaltyTypeName = GetPenaltyTypeName(request.PenaltyType),
                    AgeingOn = request.AgeingOn,
                    AgeingOnName = GetAgeingOnName(request.AgeingOn),
                    ShowLoanIssueDate = request.ShowLoanIssueDate,
                    ShowLoanIssueDateName = GetShowLoanIssueDateName(request.ShowLoanIssueDate),
                    OrderBy = request.OrderBy,
                    IsNepaliReport = request.IsNepaliReport
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