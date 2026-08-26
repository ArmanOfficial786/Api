// Repository/AccountOperation/SavingTypeWiseIndividualBalanceRepository.cs
using Dapper;
using NexgenCosysReport.DbContext;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using NexgenCosysReport.Inteface.ServiceInterface.Common;
using System.Data;
using NexgenCosysReport.Inteface.ServiceInterface.MemberAccount.SavingAcWiseReport;
using NexgenCosysReport.Dtos.RequestDtos.MemberAccount.SavingAcWiseReport;

namespace NexgenCosysReport.Repository.MemberAccount.SavingAcWiseReport
{
    public class SavingTypeWiseIndividualBalanceRepository : ISavingTypeWiseIndividualBalance
    {
        private readonly AppDbContext _context;
        private readonly IDateConverterService _dateConverter;
        private readonly ILogger<SavingTypeWiseIndividualBalanceRepository> _logger;

        // Sentinel used by the legacy BLL to mean "no filter"
        private const string NoFilter = "-1";

        // Dapper/SqlCommand default is 30s, too short for wide date-range reports.
        private const int ReportCommandTimeoutSeconds = 180;

        public SavingTypeWiseIndividualBalanceRepository(
            AppDbContext context,
            IDateConverterService dateConverter,
            ILogger<SavingTypeWiseIndividualBalanceRepository> logger)
        {
            _context = context;
            _dateConverter = dateConverter;
            _logger = logger;
        }

        public async Task<SavingTypeWiseIndividualBalanceData> GetSavingTypeWiseIndividualBalance(SavingTypeWiseIndividualBalanceRequest request)
        {
            var connectionString = _context.Database.GetConnectionString();
            using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();

            // Convert Nepali dates to English (MM/dd/yyyy)
            var fromDateAd = await _dateConverter.NepaliToEnglishAsync(request.FromDate);
            var toDateAd = await _dateConverter.NepaliToEnglishAsync(request.ToDate);
            string fromDateStr = fromDateAd.ToString("MM/dd/yyyy");
            string toDateStr = toDateAd.ToString("MM/dd/yyyy");

            // Build branch filter ("-1" = all branches, matches legacy usmOfficeId semantics)
            string branchFilter = NoFilter;
            if (!request.SameCompanyName && !string.IsNullOrEmpty(request.BranchId) && NormalizeId(request.BranchId) != NoFilter)
            {
                branchFilter = request.BranchId;
            }

            // Collection Center / Member Group / Collector filters.
            // "0", null, or empty all mean "no filter" - the WebForms dropdowns'
            // "not selected / all" sentinel is "-1", not "0". Passing "0" through
            // literally turns into "in(0)" / "= 0" in the SQL, which matches
            // nothing and makes the whole report look empty/zeroed.
            string centerFilter = NormalizeId(request.CollectionCenterId);
            string groupFilter = NormalizeId(request.MemberGroupId);
            string collectorFilter = NormalizeId(request.CollectorId);

            // Map order by
            string orderByClause = MapOrderBy(request.OrderBy);

            // Choose SP based on Opening Balance flag
            string spName = request.OpeningBalance
                ? "sp_5_43_GetSavingTypeWiseIndividualBalance"
                : "sp_5_43_GetSavingTypeWiseIndividualBalanceNoOpening";

            // Build filter expressions - mirrors CMemberAccountManagementReports
            // .GetSavingTypeWiseIndividualBalance / GetSavingTypeWiseIndividualDetailsNoOpening
            // EXACTLY:
            //  - SqlFilterExpCount and SqlFilterExpOpening only ever receive the
            //    date-range and branch/office filter - CollectionCenter/MemberGroup/
            //    Collector are applied ONLY to SqlFilterExp.
            //  - The WITH-opening variant filters SqlFilterExpCount with
            //    "AccountOpenOn BETWEEN fromDate AND toDate"; the NO-opening
            //    variant uses "AccountOpenOn <= toDate". These are NOT the same
            //    and must not be unified - using the wrong one for a given SP
            //    silently returns 0 rows/zeroed money columns for that variant.
            string sqlFilterExp = BuildFilterExpression(fromDateStr, toDateStr, branchFilter, centerFilter, groupFilter, collectorFilter);
            string sqlFilterExpCount = request.OpeningBalance
                ? BuildCountFilterExpression_WithOpening(fromDateStr, toDateStr, branchFilter)
                : BuildCountFilterExpression_NoOpening(toDateStr, branchFilter);
            string sqlFilterExpOrderBy = BuildOrderByClause(orderByClause);
            string sqlFilterExpOpening = BuildOpeningFilterExpression(fromDateStr, branchFilter);

            _logger.LogDebug(
                "SavingTypeWiseIndividualBalance filters -> Exp: {Exp} | Count: {Count} | Opening: {Opening} | OrderBy: {OrderBy} | SP: {Sp}",
                sqlFilterExp, sqlFilterExpCount, sqlFilterExpOpening, sqlFilterExpOrderBy, spName);

            var parameters = new DynamicParameters();
            parameters.Add("@SqlFilterExp", sqlFilterExp);
            parameters.Add("@SqlFilterExpCount", sqlFilterExpCount);
            parameters.Add("@SqlFilterExpOrderBy", sqlFilterExpOrderBy);
            parameters.Add("@SqlFilterExpOpening", sqlFilterExpOpening);

            var command = new CommandDefinition(
                spName,
                parameters,
                commandType: CommandType.StoredProcedure,
                commandTimeout: ReportCommandTimeoutSeconds);

            IEnumerable<SavingTypeWiseIndividualBalanceRowDto> rows;
            try
            {
                rows = await connection.QueryAsync<SavingTypeWiseIndividualBalanceRowDto>(command);
            }
            catch (SqlException ex) when (ex.Number == -2) // SQL timeout
            {
                _logger.LogError(ex,
                    "SavingTypeWiseIndividualBalance SP timed out after {Timeout}s for range {From}-{To}, branch {Branch}",
                    ReportCommandTimeoutSeconds, fromDateStr, toDateStr, branchFilter);
                throw;
            }

            var list = rows.AsList();

            // Calculate percentages if requested
            decimal totalBalance = list.Sum(r => r.Balance);
            if (request.PercentageBalance && totalBalance != 0)
            {
                foreach (var row in list)
                {
                    row.Percentage = (row.Balance / totalBalance) * 100;
                }
            }

            return new SavingTypeWiseIndividualBalanceData
            {
                Rows = list,
                TotalOpening = list.Sum(r => r.Opening),
                TotalDeposit = list.Sum(r => r.Deposit),
                TotalWithdraw = list.Sum(r => r.Withdraw),
                TotalBalance = totalBalance,
                TotalClosing = list.Sum(r => r.Closing),
                TotalRecords = list.Count
            };
        }

        /// <summary>
        /// Normalizes an incoming ID filter to the legacy "-1" no-filter sentinel.
        /// Treats null, empty, "-1", and "0" as "no filter".
        /// </summary>
        private static string NormalizeId(string? value)
        {
            if (string.IsNullOrWhiteSpace(value)) return NoFilter;
            var trimmed = value.Trim();
            return (trimmed == "0" || trimmed == NoFilter) ? NoFilter : trimmed;
        }

        private string BuildFilterExpression(string fromDate, string toDate, string branchFilter, string centerFilter, string groupFilter, string collectorFilter)
        {
            var filters = new List<string>
            {
                $" And t.TransactionOn between '{fromDate}' And '{toDate}'"
            };

            if (branchFilter != NoFilter)
                filters.Add($" And a.UsmOfficeId in({branchFilter})");

            if (groupFilter != NoFilter)
                filters.Add($" And m.SycMemberGroupId = {groupFilter}");

            if (collectorFilter != NoFilter)
                filters.Add($" And a.HurCollectorId = {collectorFilter}");

            if (centerFilter != NoFilter)
                filters.Add($" And CC.SycCollectionCenterId in({centerFilter})");

            return string.Join(" ", filters);
        }

        // Used only when request.OpeningBalance == true (sp_5_43_GetSavingTypeWiseIndividualBalance).
        // Legacy: SqlFilterExpCount += " And a.AccountOpenOn between 'from' And 'to' "
        private string BuildCountFilterExpression_WithOpening(string fromDate, string toDate, string branchFilter)
        {
            var filters = new List<string>
            {
                $" And a.AccountOpenOn between '{fromDate}' And '{toDate}'"
            };

            if (branchFilter != NoFilter)
                filters.Add($" And a.UsmOfficeId in({branchFilter})");

            return string.Join(" ", filters);
        }

        // Used only when request.OpeningBalance == false (sp_5_43_GetSavingTypeWiseIndividualBalanceNoOpening).
        // Legacy: SqlFilterExpCount += " And a.AccountOpenOn <= 'to' "
        private string BuildCountFilterExpression_NoOpening(string toDate, string branchFilter)
        {
            var filters = new List<string>
            {
                $" And a.AccountOpenOn <= '{toDate}'"
            };

            if (branchFilter != NoFilter)
                filters.Add($" And a.UsmOfficeId in({branchFilter})");

            return string.Join(" ", filters);
        }

        private string BuildOpeningFilterExpression(string fromDate, string branchFilter)
        {
            var filters = new List<string>
            {
                $" And t.TransactionOn < '{fromDate}'"
            };

            if (branchFilter != NoFilter)
                filters.Add($" And a.UsmOfficeId in({branchFilter})");

            return string.Join(" ", filters);
        }

        private string BuildOrderByClause(string orderBy)
        {
            return $" order by {orderBy}";
        }

        private string MapOrderBy(string orderBy)
        {
            return orderBy switch
            {
                "Deposit" => "Deposit DESC",
                "Withdraw" => "Withdraw DESC",
                "Balance" => "Balance DESC",
                _ => "SavingType"   // default
            };
        }
    }
}
