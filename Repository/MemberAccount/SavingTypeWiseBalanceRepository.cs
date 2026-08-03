//// Repository/AccountOperation/SavingTypeWiseBalanceRepository.cs
//using Dapper;
//using Microsoft.Data.SqlClient;
//using Microsoft.EntityFrameworkCore;
//using NexgenCosysReport.Dtos.RequestDtos.MemberAccount;
//using NexgenCosysReport.Inteface.ServiceInterface.Common;
//using NexgenCosysReport.Inteface.ServiceInterface.MemberAccount;
//using System.Data;

//namespace NexgenCosysReport.Repository.MemberAccount
//{
//    public class SavingTypeWiseBalanceRepository : ISavingTypeWiseBalance
//    {
//        private readonly AppDbContext _context;
//        private readonly IDateConverterService _dateConverter;
//        private readonly ILogger<SavingTypeWiseBalanceRepository> _logger;

//        public SavingTypeWiseBalanceRepository(
//            AppDbContext context,
//            IDateConverterService dateConverter,
//            ILogger<SavingTypeWiseBalanceRepository> logger)
//        {
//            _context = context;
//            _dateConverter = dateConverter;
//            _logger = logger;
//        }

//        public async Task<SavingTypeWiseBalanceData> GetSavingTypeWiseBalance(SavingTypeWiseBalanceRequest request)
//        {
//            var connectionString = _context.Database.GetConnectionString();
//            using var connection = new SqlConnection(connectionString);
//            await connection.OpenAsync();

//            // Convert Nepali dates to English (MM/dd/yyyy)
//            var fromDateAd = await _dateConverter.NepaliToEnglishAsync(request.FromDate);
//            var toDateAd = await _dateConverter.NepaliToEnglishAsync(request.ToDate);
//            string fromDateStr = fromDateAd.ToString("MM/dd/yyyy");
//            string toDateStr = toDateAd.ToString("MM/dd/yyyy");

//            // Build branch filter
//            string branchFilter = "-1";
//            if (!request.SameCompanyName && !string.IsNullOrEmpty(request.BranchId) && request.BranchId != "-1")
//            {
//                branchFilter = request.BranchId;
//            }

//            // Collection Center filter
//            string centerFilter = string.IsNullOrEmpty(request.CollectionCenterId) ? "-1" : request.CollectionCenterId;

//            // Member Group filter
//            string groupFilter = string.IsNullOrEmpty(request.MemberGroupId) ? "-1" : request.MemberGroupId;

//            // Collector filter
//            string collectorFilter = string.IsNullOrEmpty(request.CollectorId) ? "-1" : request.CollectorId;

//            // Map order by
//            string orderByClause = MapOrderBy(request.OrderBy);

//            // Choose SP based on Opening Balance flag
//            string spName = request.OpeningBalance
//                ? "sp_5_43_GetSavingTypeWiseBalance1"
//                : "sp_5_43_GetSavingTypeWiseBalanceNoOpening";

//            // Build parameters for the SP (matches the BLL)
//            var parameters = new DynamicParameters();

//            // The SP expects parameters in a specific order:
//            // @SqlFilterExp (for transaction date filter)
//            // @SqlFilterExpCount (for account opening date filter)
//            // @SqlFilterExpOrderBy
//            // @SqlFilterExpOpening (for opening balance filter)

//            // Build filter expressions
//            string sqlFilterExp = BuildFilterExpression(fromDateStr, toDateStr, branchFilter, centerFilter, groupFilter, collectorFilter);
//            string sqlFilterExpCount = BuildCountFilterExpression(toDateStr, branchFilter, centerFilter, groupFilter, collectorFilter);
//            string sqlFilterExpOrderBy = BuildOrderByClause(orderByClause);
//            string sqlFilterExpOpening = BuildOpeningFilterExpression(fromDateStr, branchFilter, centerFilter, groupFilter, collectorFilter);

//            parameters.Add("@SqlFilterExp", sqlFilterExp);
//            parameters.Add("@SqlFilterExpCount", sqlFilterExpCount);
//            parameters.Add("@SqlFilterExpOrderBy", sqlFilterExpOrderBy);
//            parameters.Add("@SqlFilterExpOpening", sqlFilterExpOpening);

//            var rows = await connection.QueryAsync<SavingTypeWiseBalanceRowDto>(
//                spName,
//                parameters,
//                commandType: CommandType.StoredProcedure
//            );

//            var list = rows.AsList();

//            // Calculate percentages if requested
//            decimal totalBalance = list.Sum(r => r.Balance);
//            if (request.PercentageBalance && totalBalance > 0)
//            {
//                foreach (var row in list)
//                {
//                    row.Percentage = (row.Balance / totalBalance) * 100;
//                }
//            }

//            return new SavingTypeWiseBalanceData
//            {
//                Rows = list,
//                TotalOpening = list.Sum(r => r.Opening),
//                TotalDeposit = list.Sum(r => r.Deposit),
//                TotalWithdraw = list.Sum(r => r.Withdraw),
//                TotalBalance = totalBalance,
//                TotalClosing = list.Sum(r => r.Closing),
//                TotalRecords = list.Count
//            };
//        }

//        private string BuildFilterExpression(string fromDate, string toDate, string branchFilter, string centerFilter, string groupFilter, string collectorFilter)
//        {
//            var filters = new List<string>();

//            // Date range filter
//            filters.Add($" And t.TransactionOn between '{fromDate}' And '{toDate}'");

//            // Branch filter
//            if (branchFilter != "-1")
//                filters.Add($" And a.UsmOfficeId in({branchFilter})");

//            // Collection Center filter
//            if (centerFilter != "-1")
//                filters.Add($" And CC.SycCollectionCenterId in({centerFilter})");

//            // Member Group filter
//            if (groupFilter != "-1")
//                filters.Add($" And m.SycMemberGroupId = {groupFilter}");

//            // Collector filter
//            if (collectorFilter != "-1")
//                filters.Add($" And a.HurCollectorId = {collectorFilter}");

//            return string.Join(" ", filters);
//        }

//        private string BuildCountFilterExpression(string toDate, string branchFilter, string centerFilter, string groupFilter, string collectorFilter)
//        {
//            var filters = new List<string>();

//            // Account opening date filter
//            filters.Add($" And a.AccountOpenOn <= '{toDate}'");

//            if (branchFilter != "-1")
//                filters.Add($" And a.UsmOfficeId in({branchFilter})");

//            if (centerFilter != "-1")
//                filters.Add($" And CC.SycCollectionCenterId in({centerFilter})");

//            if (groupFilter != "-1")
//                filters.Add($" And m.SycMemberGroupId = {groupFilter}");

//            if (collectorFilter != "-1")
//                filters.Add($" And a.HurCollectorId = {collectorFilter}");

//            return string.Join(" ", filters);
//        }

//        private string BuildOpeningFilterExpression(string fromDate, string branchFilter, string centerFilter, string groupFilter, string collectorFilter)
//        {
//            var filters = new List<string>();

//            // Opening balance filter: transactions before fromDate
//            filters.Add($" And t.TransactionOn < '{fromDate}'");

//            if (branchFilter != "-1")
//                filters.Add($" And a.UsmOfficeId in({branchFilter})");

//            if (centerFilter != "-1")
//                filters.Add($" And CC.SycCollectionCenterId in({centerFilter})");

//            if (groupFilter != "-1")
//                filters.Add($" And m.SycMemberGroupId = {groupFilter}");

//            if (collectorFilter != "-1")
//                filters.Add($" And a.HurCollectorId = {collectorFilter}");

//            return string.Join(" ", filters);
//        }

//        private string BuildOrderByClause(string orderBy)
//        {
//            return $" order by {orderBy}";
//        }

//        private string MapOrderBy(string orderBy)
//        {
//            return orderBy switch
//            {
//                "Deposit" => "Deposit DESC",
//                "Withdraw" => "Withdraw DESC",
//                "Balance" => "Balance DESC",
//                _ => "SavingType"   // default
//            };
//        }
//    }
//}




// Repository/AccountOperation/SavingTypeWiseBalanceRepository.cs
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using NexgenCosysReport.Dtos.RequestDtos.MemberAccount;
using NexgenCosysReport.Inteface.ServiceInterface.Common;
using NexgenCosysReport.Inteface.ServiceInterface.MemberAccount;
using System.Data;

namespace NexgenCosysReport.Repository.MemberAccount
{
    public class SavingTypeWiseBalanceRepository : ISavingTypeWiseBalance
    {
        private readonly AppDbContext _context;
        private readonly IDateConverterService _dateConverter;
        private readonly ILogger<SavingTypeWiseBalanceRepository> _logger;

        // Sentinel used by the legacy BLL to mean "no filter"
        private const string NoFilter = "-1";

        // Dapper/SqlCommand default is 30s, which is too short for wide date-range
        // reports (e.g. multi-year spans) hitting sp_5_43_GetSavingTypeWiseBalance1 /
        // sp_5_43_GetSavingTypeWiseBalanceNoOpening on large transaction tables.
        // Bump this (and/or move it to appsettings under ReportSettings) if large
        // ranges still time out in your environment.
        private const int ReportCommandTimeoutSeconds = 180;

        public SavingTypeWiseBalanceRepository(
            AppDbContext context,
            IDateConverterService dateConverter,
            ILogger<SavingTypeWiseBalanceRepository> logger)
        {
            _context = context;
            _dateConverter = dateConverter;
            _logger = logger;
        }

        public async Task<SavingTypeWiseBalanceData> GetSavingTypeWiseBalance(SavingTypeWiseBalanceRequest request)
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
            // The WebForms dropdowns use "-1" as their "not selected / all" sentinel value
            // (added by Utils.AddSelectInDropDrownList). The API can receive "0", null, or
            // empty string for the same "no filter" intent - all of these must normalize to
            // "-1", otherwise they get treated as real IDs (e.g. "in(0)") and the SP returns
            // zero rows.
            string centerFilter = NormalizeId(request.CollectionCenterId);
            string groupFilter = NormalizeId(request.MemberGroupId);
            string collectorFilter = NormalizeId(request.CollectorId);

            // Map order by
            string orderByClause = MapOrderBy(request.OrderBy);

            // Choose SP based on Opening Balance flag
            string spName = request.OpeningBalance
                ? "sp_5_43_GetSavingTypeWiseBalance1"
                : "sp_5_43_GetSavingTypeWiseBalanceNoOpening";

            // Build filter expressions - mirrors CMemberAccountManagementReports.GetSavingTypeWiseDetails(NoOpening)
            // exactly: SqlFilterExpCount and SqlFilterExpOpening only ever receive the date-range
            // and branch/office filters. CollectionCenter / MemberGroup / Collector are applied
            // ONLY to the main SqlFilterExp.
            string sqlFilterExp = BuildFilterExpression(fromDateStr, toDateStr, branchFilter, centerFilter, groupFilter, collectorFilter);
            string sqlFilterExpCount = BuildCountFilterExpression(toDateStr, branchFilter);
            string sqlFilterExpOrderBy = BuildOrderByClause(orderByClause);
            string sqlFilterExpOpening = BuildOpeningFilterExpression(fromDateStr, branchFilter);

            _logger.LogDebug(
                "SavingTypeWiseBalance filters -> Exp: {Exp} | Count: {Count} | Opening: {Opening} | OrderBy: {OrderBy} | SP: {Sp}",
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

            IEnumerable<SavingTypeWiseBalanceRowDto> rows;
            try
            {
                rows = await connection.QueryAsync<SavingTypeWiseBalanceRowDto>(command);
            }
            catch (SqlException ex) when (ex.Number == -2) // SQL timeout
            {
                _logger.LogError(ex,
                    "SavingTypeWiseBalance SP timed out after {Timeout}s for range {From}-{To}, branch {Branch}",
                    ReportCommandTimeoutSeconds, fromDateStr, toDateStr, branchFilter);
                throw;
            }

            var list = rows.AsList();

            // Calculate percentages if requested
            decimal totalBalance = list.Sum(r => r.Balance);
            if (request.PercentageBalance && totalBalance > 0)
            {
                foreach (var row in list)
                {
                    row.Percentage = (row.Balance / totalBalance) * 100;
                }
            }

            return new SavingTypeWiseBalanceData
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
            var filters = new List<string>();

            filters.Add($" And t.TransactionOn between '{fromDate}' And '{toDate}'");

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

        private string BuildCountFilterExpression(string toDate, string branchFilter)
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