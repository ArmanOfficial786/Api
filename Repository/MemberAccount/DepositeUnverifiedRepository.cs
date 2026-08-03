
// Repository/AccountOperation/DepositUnverifiedRepository.cs
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using NexgenCosysReport.Dtos.RequestDtos.MemberAccount;
using NexgenCosysReport.Inteface.ServiceInterface.Common;
using NexgenCosysReport.Inteface.ServiceInterface.MemberAccount;
using System.Data;

namespace NexgenCosysReport.Repository.MemberAccount
{
    public class DepositUnverifiedRepository : IDepositUnverified
    {
        private readonly AppDbContext _context;
        private readonly IDateConverterService _dateConverter;
        private readonly ILogger<DepositUnverifiedRepository> _logger;

        public DepositUnverifiedRepository(
            AppDbContext context,
            IDateConverterService dateConverter,
            ILogger<DepositUnverifiedRepository> logger)
        {
            _context = context;
            _dateConverter = dateConverter;
            _logger = logger;

            // Map SP output columns that don't exactly match the DTO property names.
            // Dapper only matches by exact (case-insensitive) name, so without this,
            // "Collector" and "AccountOpenOnBs" from the SPs never bind.
            if (SqlMapper.GetTypeMap(typeof(DepositUnverifiedRowDto)) is not CustomPropertyTypeMap)
            {
                SqlMapper.SetTypeMap(
                    typeof(DepositUnverifiedRowDto),
                    new CustomPropertyTypeMap(typeof(DepositUnverifiedRowDto), (type, columnName) =>
                    {
                        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                        {
                            ["Collector"] = nameof(DepositUnverifiedRowDto.CollectorName),
                            ["AccountOpenOnBs"] = nameof(DepositUnverifiedRowDto.AccountOpenDate),
                        };
                        var propName = map.TryGetValue(columnName, out var mapped) ? mapped : columnName;
                        return type.GetProperty(propName)!;
                    })
                );
            }
        }

        public async Task<DepositUnverifiedData> GetDepositUnverified(DepositUnverifiedRequest request)
        {
            var connectionString = _context.Database.GetConnectionString();
            using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();

            string spName = GetStoredProcedureName(request.ReportType);

            // --- Date parameters ---
            // The "A" and "U" SPs concatenate @SqlFilterExpFrom / @SqlSqlFilterExpTo directly
            // into dynamic SQL as a date literal, e.g.:
            //     DV.VerifiedDateOn >= ' + @SqlFilterExpFrom + '
            // so the *value itself* must already contain the surrounding quotes, e.g. '2025-01-15'.
            // Sending an unquoted "08/01/2025" turns it into arithmetic (08 / 01 / 2025) and
            // silently kills the correlated subquery matches for VerifiedDate/Till/By.
            string fromDateSql = "'1900-01-01'";
            string toDateSql = "'2099-12-31'";

            if (!string.IsNullOrEmpty(request.FromDate) && request.FromDate != "-1")
            {
                var fromDateAd = await _dateConverter.NepaliToEnglishAsync(request.FromDate);
                fromDateSql = $"'{fromDateAd:yyyy-MM-dd}'";
            }

            if (!string.IsNullOrEmpty(request.ToDate) && request.ToDate != "-1")
            {
                var toDateAd = await _dateConverter.NepaliToEnglishAsync(request.ToDate);
                toDateSql = $"'{toDateAd:yyyy-MM-dd}'";
            }

            // Member ID filter (if provided)
            long memberId = -1;
            if (!string.IsNullOrEmpty(request.MemberId))
            {
                var memberSql = "SELECT MemMemberRegistrationId FROM MemMemberRegistration WHERE MemberId = @MemberId";
                var result = await connection.QueryFirstOrDefaultAsync<long?>(memberSql, new { MemberId = request.MemberId });
                if (result.HasValue)
                    memberId = result.Value;
            }

            // Branch filter
            string branchFilter = "-1";
            if (!request.SameCompanyName && !string.IsNullOrEmpty(request.BranchIds) && request.BranchIds != "-1")
            {
                branchFilter = request.BranchIds;
            }

            // sp_5_43_GetDepositStatementVerification uses different table aliases
            // (a / m) than the other two SPs (MA / MR), and has no HurCollector join at all.
            bool isVerificationOnlySp = request.ReportType == "V";

            string filterExpression = BuildFilterExpression(
                memberId, branchFilter, request.DepositTypeId, request.CollectorId, isVerificationOnlySp);

            string orderByClause = MapOrderBy(request.OrderBy, isVerificationOnlySp);

            var parameters = new DynamicParameters();
            parameters.Add("@SqlFilterExp", filterExpression);
            parameters.Add("@SqlFilterExpOrder", orderByClause);

            // sp_5_43_GetDepositStatementVerification only declares 2 parameters.
            // Sending the date params to it throws a SQL exception.
            if (!isVerificationOnlySp)
            {
                parameters.Add("@SqlFilterExpFrom", fromDateSql);
                parameters.Add("@SqlSqlFilterExpTo", toDateSql);
            }

            _logger.LogInformation(
                "SP: {SpName}, Filter: {Filter}, OrderBy: {OrderBy}, From: {From}, To: {To}",
                spName, filterExpression, orderByClause, fromDateSql, toDateSql);

            var rows = (await connection.QueryAsync<DepositUnverifiedRowDto>(
                spName,
                parameters,
                commandType: CommandType.StoredProcedure
            )).AsList();

            // sp_5_43_GetDepositStatementUnVerifiedReport never returns VerifiedDate at all
            // (unverified rows have none by definition), and none of the SPs return a
            // dedicated IsVerified bit column — derive it instead of trusting a bound value.
            foreach (var row in rows)
            {
                row.IsVerified = !string.IsNullOrWhiteSpace(row.VerifiedDate);
            }

            return new DepositUnverifiedData
            {
                Rows = rows,
                TotalRecords = rows.Count,
                VerifiedCount = rows.Count(r => r.IsVerified),
                UnverifiedCount = rows.Count(r => !r.IsVerified)
            };
        }

        private string BuildFilterExpression(
            long memberId, string branchFilter, string? depositTypeId, string? collectorId, bool isVerificationOnlySp)
        {
            var filters = new List<string>();

            // sp_5_43_GetDepositStatementVerification aliases MemMemberRegistration as "m"
            // and MamAccountOpening as "a" — not "MR"/"MA" like the other two SPs.
            string memberAlias = isVerificationOnlySp ? "m" : "MR";
            string officeAlias = isVerificationOnlySp ? "a" : "MA";

            if (memberId != -1)
                filters.Add($" And {memberAlias}.MemMemberRegistrationId = {memberId}");

            if (branchFilter != "-1")
                filters.Add($" And {officeAlias}.UsmOfficeId in ({branchFilter})");

            if (!string.IsNullOrEmpty(depositTypeId) && depositTypeId != "-1")
                filters.Add($" And {officeAlias}.SycDepositTypeId = {depositTypeId}");

            // sp_5_43_GetDepositStatementVerification doesn't join HurCollector at all,
            // so a collector filter has no valid column to bind to there — skip it.
            if (!isVerificationOnlySp && !string.IsNullOrEmpty(collectorId) && collectorId != "-1")
                filters.Add($" And {officeAlias}.HurCollectorId = {collectorId}");

            return string.Join(" ", filters);
        }

        private string GetStoredProcedureName(string reportType)
        {
            return reportType switch
            {
                "V" => "sp_5_43_GetDepositStatementVerification",
                "U" => "sp_5_43_GetDepositStatementUnVerifiedReport",
                _ => "sp_5_43_GetAllDepositStatementVerifiedUnVerifiedReport"  // A = All
            };
        }

        private string MapOrderBy(string orderBy, bool isVerificationOnlySp)
        {
            string column = orderBy switch
            {
                "MemberName" => "MemberName",
                "AccountNo" => "AccountNo",
                "DepositTypeName" => "DepositTypeName",
                "OpenDate" => "AccountOpenOnBs",
                "CollectorFullName" => "Collector",
                "VerifiedTill" => "VerifiedTill",
                "VerifiedDate" => "VerifiedDate",
                "VerifiedBy" => "VerifiedBy",
                _ => "MemberId"
            };

            // sp_5_43_GetDepositStatementVerification doesn't select DepositTypeName,
            // AccountOpenOnBs, or Collector — ordering by them would throw "Invalid column name".
            if (isVerificationOnlySp && column is "DepositTypeName" or "AccountOpenOnBs" or "Collector")
                column = "MemberId";

            return $" Order By {column}";
        }
    }
}