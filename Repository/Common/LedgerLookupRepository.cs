//using Dapper;
//using Microsoft.Data.SqlClient;
//using Microsoft.EntityFrameworkCore;
//using NexgenCosysReport.DbContext;
//using NexgenCosysReport.Dtos.RequestDtos.Common;
//using NexgenCosysReport.Inteface.ServiceInterface.Common;
//using System.Data;

//namespace NexgenCosysReport.Repository.Common
//{
//    public class LedgerLookupRepository : ILedgerLookupRepository
//    {
//        private readonly AppDbContext _context;
//        private readonly IDateConverterService _dateConverter;

//        public LedgerLookupRepository(AppDbContext context, IDateConverterService dateConverter)
//        {
//            _context = context;
//            _dateConverter = dateConverter;
//        }

//        // Mirrors CAcoAccountType.GetAll() — feeds ddlLedgerHead in the webform.
//        public async Task<List<LedgerHeadRowDto>> GetLedgerHeadsAsync()
//        {
//            var connectionString = _context.Database.GetConnectionString();
//            await using var connection = new SqlConnection(connectionString);

//            const string sql = @"
//                SELECT AcoAccountTypeId, AccountType
//                FROM AcoAccountType
//                ORDER BY AccountType";

//            var rows = await connection.QueryAsync<LedgerHeadRowDto>(sql);
//            return rows.ToList();
//        }

//        // --------------------------------------------------------------
//        // Shared filter builder — matches CAcoVoucherPosting.GetVoucherLedgerDetailsMainLedger's
//        // SqlFilterExp construction (date range, branch, account type), minus the
//        // fiscal-year-spanning "forNewSp" branch (see method-level note in the caller).
//        // --------------------------------------------------------------
//        private async Task<string> BuildSqlFilterExp(string fromDate, string toDate, string? branchId, int accountTypeId)
//        {
//            var filter = string.Empty;

//            if (!string.IsNullOrEmpty(fromDate) && !string.IsNullOrEmpty(toDate)
//                && fromDate != "-1" && toDate != "-1")
//            {
//                string fromDateAd = await _dateConverter.BsToAdStringAsync(fromDate);
//                string toDateAd = await _dateConverter.BsToAdStringAsync(toDate);

//                if (!string.IsNullOrEmpty(fromDateAd) && !string.IsNullOrEmpty(toDateAd))
//                {
//                    filter += $" AND v.VoucherOn BETWEEN '{fromDateAd}' AND '{toDateAd}'";
//                }
//            }

//            if (!string.IsNullOrEmpty(branchId) && branchId != "-1" && branchId != "string"
//                && long.TryParse(branchId, out var parsedBranchId))
//            {
//                filter += $" AND v.UsmOfficeId = {parsedBranchId}";
//            }

//            if (accountTypeId != -1)
//            {
//                filter += $" AND l.AcoAccountTypeId = {accountTypeId}";
//            }

//            return filter;
//        }

//        // Feeds ddlLedgerName — level 0 in the webform: account-type filter only,
//        // returns distinct MainLedger values. sp_6_56_GetVoucherLedgerDetailsMainLedger
//        // can return duplicate MainLedger rows (one per SubLedger), so this dedups in C#
//        // since I don't have the SP body to confirm whether it already does so.
//        public async Task<List<LedgerNameRowDto>> GetLedgerNamesAsync(LedgerNameRequestDto request)
//        {
//            var sqlFilterExp = await BuildSqlFilterExp(request.FromDate, request.ToDate, request.BranchId, request.AccountTypeId);

//            var connectionString = _context.Database.GetConnectionString();
//            await using var connection = new SqlConnection(connectionString);

//            var parameters = new DynamicParameters();
//            parameters.Add("@SqlFilterExp", sqlFilterExp);
//            parameters.Add("@SqlFilterExpFilter", string.Empty);
//            parameters.Add("@SqlFilterExpOrderBy", string.Empty);
//            parameters.Add("@Level", 0);

//            var rows = await connection.QueryAsync<LedgerNameRowDto>(
//                "sp_6_56_GetVoucherLedgerDetailsMainLedger",
//                parameters,
//                commandType: CommandType.StoredProcedure,
//                commandTimeout: 120
//            );

//            return rows
//                .Where(r => !string.IsNullOrEmpty(r.MainLedger))
//                .Select(r => r.MainLedger!)
//                .Distinct(StringComparer.OrdinalIgnoreCase)
//                .OrderBy(m => m)
//                .Select(m => new LedgerNameRowDto { MainLedger = m })
//                .ToList();
//        }

//        // Feeds ddlSubLedgerName — level 1 in the webform: account-type filter plus
//        // "MainLedger = @MainLedger", returns distinct SubLedger1 values.
//        // Feeds ddlSubLedgerName — level 1 in the webform: account-type filter plus
//        // "MainLedger = @MainLedger", returns distinct SubLedger1 values.
//        public async Task<List<SubLedgerNameRowDto>> GetSubLedgerNamesAsync(SubLedgerNameRequestDto request)
//        {
//            var sqlFilterExp = await BuildSqlFilterExp(request.FromDate, request.ToDate, request.BranchId, request.AccountTypeId);
//            var escapedMainLedger = (request.MainLedger ?? string.Empty).Replace("'", "''");
//            var sqlFilterExpFilter = $" And MainLedger = N'{escapedMainLedger}'";

//            var connectionString = _context.Database.GetConnectionString();
//            await using var connection = new SqlConnection(connectionString);

//            var parameters = new DynamicParameters();
//            parameters.Add("@SqlFilterExp", sqlFilterExp);
//            parameters.Add("@SqlFilterExpFilter", sqlFilterExpFilter);
//            parameters.Add("@SqlFilterExpOrderBy", string.Empty);
//            parameters.Add("@Level", 1);

//            var rows = await connection.QueryAsync<SubLedgerNameRowDto>(
//                "sp_6_56_GetVoucherLedgerDetailsMainLedger",
//                parameters,
//                commandType: CommandType.StoredProcedure,
//                commandTimeout: 120
//            );

//            return rows
//                .Where(r => !string.IsNullOrEmpty(r.SubLedger1))
//                .Select(r => r.SubLedger1!)
//                .Distinct(StringComparer.OrdinalIgnoreCase)
//                .OrderBy(s => s)
//                .Select(s => new SubLedgerNameRowDto { SubLedger1 = s })
//                .ToList();
//        }
//    }
//}



// Repositories/Common/LedgerLookupRepository.cs
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using NexgenCosysReport.DbContext;
using NexgenCosysReport.Dtos.RequestDtos.Common;
using NexgenCosysReport.Inteface.ServiceInterface.Common;
using System.Data;

namespace NexgenCosysReport.Repository.Common
{
    public class LedgerLookupRepository : ILedgerLookupRepository
    {
        private readonly AppDbContext _context;
        private readonly IDateConverterService _dateConverter;

        // Level constants mirroring the legacy CAcoVoucherPosting.GetVoucherLedgerDetailsMainLedger levels
        private const int LevelMainLedger = 0;
        private const int LevelSubLedger1 = 1;
        private const int LevelSubLedger2 = 2;
        private const int LevelSubLedger3 = 3;
        private const int LevelSubLedger4 = 4;

        public LedgerLookupRepository(AppDbContext context, IDateConverterService dateConverter)
        {
            _context = context;
            _dateConverter = dateConverter;
        }

        public async Task<List<LedgerHeadRowDto>> GetLedgerHeadsAsync()
        {
            var connectionString = _context.Database.GetConnectionString();
            using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();

            // Legacy: CAcoAccountType.GetAll() — simple lookup table, no dynamic filter needed
            const string sql = @"
                SELECT AcoAccountTypeId, AccountType
                FROM AcoAccountType
                ORDER BY AcoAccountTypeId";

            var result = await connection.QueryAsync<LedgerHeadRowDto>(sql);
            return result.ToList();
        }

        public async Task<List<LedgerNameRowDto>> GetLedgerNamesAsync(LedgerNameRequestDto request)
        {
            var rows = await GetVoucherLedgerDetailsMainLedgerAsync(
                request.FromDate,
                request.ToDate,
                request.BranchId,
                request.AccountTypeId,
                LevelMainLedger,
                ledger: string.Empty);

            return rows.Select(r => new LedgerNameRowDto { MainLedger = r.MainLedger }).ToList();
        }

        public async Task<List<SubLedgerNameRowDto>> GetSubLedgerNamesAsync(SubLedgerNameRequestDto request)
        {
            var rows = await GetVoucherLedgerDetailsMainLedgerAsync(
                request.FromDate,
                request.ToDate,
                request.BranchId,
                request.AccountTypeId,
                LevelSubLedger1,
                ledger: request.MainLedger);

            return rows.Select(r => new SubLedgerNameRowDto { SubLedger1 = r.SubLedger1 }).ToList();
        }

        public async Task<List<SecondSubLedgerNameRowDto>> GetSecondSubLedgerNamesAsync(SecondSubLedgerNameRequestDto request)
        {
            var rows = await GetVoucherLedgerDetailsMainLedgerAsync(
                request.FromDate,
                request.ToDate,
                request.BranchId,
                request.AccountTypeId,
                LevelSubLedger2,
                ledger: request.SubLedger1);

            return rows.Select(r => new SecondSubLedgerNameRowDto { SubLedger2 = r.SubLedger2 }).ToList();
        }

        public async Task<List<ThirdSubLedgerNameRowDto>> GetThirdSubLedgerNamesAsync(ThirdSubLedgerNameRequestDto request)
        {
            var rows = await GetVoucherLedgerDetailsMainLedgerAsync(
                request.FromDate,
                request.ToDate,
                request.BranchId,
                request.AccountTypeId,
                LevelSubLedger3,
                ledger: request.SubLedger2);

            return rows.Select(r => new ThirdSubLedgerNameRowDto { SubLedger3 = r.SubLedger3 }).ToList();
        }

        public async Task<List<FourthSubLedgerNameRowDto>> GetFourthSubLedgerNamesAsync(FourthSubLedgerNameRequestDto request)
        {
            var rows = await GetVoucherLedgerDetailsMainLedgerAsync(
                request.FromDate,
                request.ToDate,
                request.BranchId,
                request.AccountTypeId,
                LevelSubLedger4,
                ledger: request.SubLedger3);

            return rows.Select(r => new FourthSubLedgerNameRowDto { SubLedger4 = r.SubLedger4 }).ToList();
        }

        /// <summary>
        /// Shared core: mirrors CAcoVoucherPosting.GetVoucherLedgerDetailsMainLedger exactly.
        /// The legacy method — and therefore the SP itself — only ever accepts 4 parameters:
        /// @SqlFilterExp, @SqlFilterExpFilter, @SqlFilterExpOrderBy, @Level. AccountTypeId and
        /// the ledger value are NOT separate bound parameters; they're literal text folded into
        /// @SqlFilterExp / @SqlFilterExpFilter, same as legacy. Passing them as extra named
        /// Dapper parameters (as a prior version of this method did) sends 5+ parameters to a
        /// 4-parameter procedure, which SQL Server rejects with "too many arguments specified" —
        /// that's what caused the 500 on 2nd/3rd/4th sub-ledger lookups once forBackYearSp kicked in.
        /// Also picks the "ForBackYear" SP variant when the date range exceeds a year, same as
        /// legacy's dateFrom.AddYears(1) &lt; dateTo rule.
        /// </summary>
        private async Task<List<LedgerLevelRow>> GetVoucherLedgerDetailsMainLedgerAsync(
            string fromDateBs,
            string toDateBs,
            string? branchId,
            int accountTypeId,
            int level,
            string ledger)
        {
            // AccountTypeId of -1 means "no account type selected" — legacy code returns
            // an empty DataTable in this case rather than hitting the SP at all.
            if (accountTypeId == -1)
            {
                return new List<LedgerLevelRow>();
            }

            var forBackYearSp = false;
            var sqlFilterExp = string.Empty;
            var sqlFilterExpFilter = string.Empty;
            var sqlFilterExpOrderBy = string.Empty;

            if (fromDateBs != "-1" && toDateBs != "-1"
                && !string.IsNullOrWhiteSpace(fromDateBs) && !string.IsNullOrWhiteSpace(toDateBs))
            {
                // BS dates normalized hyphen->slash before conversion, per established pattern
                var normalizedFrom = fromDateBs.Replace("-", "/");
                var normalizedTo = toDateBs.Replace("-", "/");

                var dateFromEng = await _dateConverter.NepaliToEnglishAsync(normalizedFrom);
                var dateToEng = await _dateConverter.NepaliToEnglishAsync(normalizedTo);

                // Same >1 year rule as legacy: switch to the back-year SP variant, and (matching
                // legacy exactly) skip the date filter entirely in that branch rather than passing
                // it through some other way — the back-year SP handles the date range internally.
                if (dateFromEng.AddYears(1) < dateToEng)
                {
                    forBackYearSp = true;
                }
                else
                {
                    var dateFromAd = dateFromEng.ToString("yyyy-MM-dd");
                    var dateToAd = dateToEng.ToString("yyyy-MM-dd");
                    sqlFilterExp += $" And v.VoucherOn between '{dateFromAd}' And '{dateToAd}' ";
                }
            }

            if (!forBackYearSp
                && !string.IsNullOrWhiteSpace(branchId) && branchId != "-1"
                && long.TryParse(branchId, out var parsedBranchId))
            {
                sqlFilterExp += $" And v.UsmOfficeId = {parsedBranchId} ";
                // NOTE: if you need the "all branches" sentinel to resolve to the active
                // UsmOfficeId list (per your established migration pattern) rather than
                // simply omitting the branch filter, resolve that list here instead and
                // fold it into sqlFilterExp as an IN(...) clause.
            }

            // AccountTypeId is always applied as a literal — legacy never omits this based on
            // forBackYearSp, and it is NOT a separate SP parameter.
            sqlFilterExp += $" And l.AcoAccountTypeId = {accountTypeId} ";

            var ledgerColumn = level switch
            {
                LevelSubLedger1 => "MainLedger",
                LevelSubLedger2 => "SubLedger1",
                LevelSubLedger3 => "SubLedger2",
                LevelSubLedger4 => "SubLedger3",
                _ => null
            };

            if (ledgerColumn != null && !string.IsNullOrWhiteSpace(ledger))
            {
                // Escaped the same way the legacy code escapes string literals elsewhere in
                // this file (doubling single quotes) — the ledger value is folded into the
                // filter text, not passed as its own bound parameter.
                var escapedLedger = ledger.Replace("'", "''");
                sqlFilterExpFilter += $" And {ledgerColumn} = N'{escapedLedger}'";
            }

            var parameters = new DynamicParameters();
            parameters.Add("SqlFilterExp", sqlFilterExp);
            parameters.Add("SqlFilterExpFilter", sqlFilterExpFilter);
            parameters.Add("SqlFilterExpOrderBy", sqlFilterExpOrderBy);
            parameters.Add("Level", level);

            var spName = forBackYearSp
                ? "sp_6_56_GetVoucherLedgerDetailsMainLedgerForBackYear"
                : "sp_6_56_GetVoucherLedgerDetailsMainLedger";

            var connectionString = _context.Database.GetConnectionString();
            using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();

            var result = await connection.QueryAsync<LedgerLevelRow>(
                spName,
                parameters,
                commandType: CommandType.StoredProcedure,
                commandTimeout: 120);

            return result.ToList();
        }

        /// <summary>
        /// Raw row shape returned by the SP — covers every level's column so a single
        /// query method can serve all four sub-ledger levels plus the main ledger level.
        /// </summary>
        private class LedgerLevelRow
        {
            public string? MainLedger { get; set; }
            public string? SubLedger1 { get; set; }
            public string? SubLedger2 { get; set; }
            public string? SubLedger3 { get; set; }
            public string? SubLedger4 { get; set; }
        }
    }
}