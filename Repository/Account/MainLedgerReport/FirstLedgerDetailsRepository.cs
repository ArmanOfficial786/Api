//// Repository/Account/FirstLedgerDetailsReport/FirstLedgerDetailsRepository.cs
//using Dapper;
//using Microsoft.Data.SqlClient;
//using Microsoft.EntityFrameworkCore;
//using NexgenCosysReport.DbContext;
//using NexgenCosysReport.Dtos.RequestDtos.Account.FirstLedgerDetailsReport;
//using NexgenCosysReport.Inteface.ServiceInterface.Account.FirstLedgerDetailsReport;
//using NexgenCosysReport.Inteface.ServiceInterface.Common;
//using System.Data;

//namespace NexgenCosysReport.Repository.Account.FirstLedgerDetailsReport
//{
//    public class FirstLedgerDetailsRepository : IFirstLedgerDetailsRepository
//    {
//        private readonly AppDbContext _context;
//        private readonly IDateConverterService _dateConverter;
//        private readonly ILogger<FirstLedgerDetailsRepository> _logger;

//        public FirstLedgerDetailsRepository(
//            AppDbContext context,
//            IDateConverterService dateConverter,
//            ILogger<FirstLedgerDetailsRepository> logger)
//        {
//            _context = context;
//            _dateConverter = dateConverter;
//            _logger = logger;
//        }

//        private async Task<string> BuildSqlFilterExp(FirstLedgerDetailsRequestDto request)
//        {
//            var filter = string.Empty;

//            if (!string.IsNullOrEmpty(request.FromDate) && !string.IsNullOrEmpty(request.ToDate)
//                && request.FromDate != "-1" && request.ToDate != "-1")
//            {
//                string fromDateAd = await _dateConverter.BsToAdStringAsync(request.FromDate);
//                string toDateAd = await _dateConverter.BsToAdStringAsync(request.ToDate);

//                if (!string.IsNullOrEmpty(fromDateAd) && !string.IsNullOrEmpty(toDateAd))
//                {
//                    filter += $" AND v.VoucherOn BETWEEN '{fromDateAd}' AND '{toDateAd}'";
//                }
//            }

//            if (!string.IsNullOrEmpty(request.BranchIds) &&
//                request.BranchIds != "-1" &&
//                request.BranchIds != "string")
//            {
//                filter += $" AND v.UsmOfficeId IN ({request.BranchIds})";
//            }

//            if (!string.IsNullOrEmpty(request.VoucherType) && request.VoucherType != "All")
//            {
//                bool isAuto = request.VoucherType == "Auto";
//                filter += $" AND vp.IsAutomatic = {(isAuto ? "1" : "0")}";
//            }

//            return filter;
//        }

//        private async Task<(string opening, string closing)> BuildOpeningClosingFilterExp(
//            SqlConnection connection,
//            FirstLedgerDetailsRequestDto request,
//            string dateFrom,
//            string dateTo)
//        {
//            var openingFilter = string.Empty;
//            var closingFilter = string.Empty;

//            if (request.LedgerHeadId == 3 || request.LedgerHeadId == 4)
//            {
//                var accountYearClosing = await connection.QueryFirstOrDefaultAsync<int?>(
//                    "SELECT MAX(AcoFiscalYearId) FROM AcoAccountYearClosing") ?? 0;

//                if (accountYearClosing == 0)
//                {
//                    openingFilter += $" And v.VoucherOn < '{dateFrom}'";
//                    closingFilter += $" And v.VoucherOn <= '{dateTo}'";
//                }
//                else
//                {
//                    var fiscalYearToOn = await connection.QueryFirstOrDefaultAsync<DateTime?>(
//                        "SELECT FiscalYearToOn FROM AcoFiscalYear WHERE AcoFiscalYearId = @Id",
//                        new { Id = accountYearClosing });

//                    var dateFromParsed = DateTime.Parse(dateFrom);

//                    if (fiscalYearToOn.HasValue && fiscalYearToOn.Value < dateFromParsed.AddDays(-1))
//                    {
//                        var fiscalYearNext = fiscalYearToOn.Value.AddDays(1).ToString("yyyy-MM-dd");
//                        openingFilter += $" And v.VoucherOn between '{fiscalYearNext}' And '{dateFrom}'";
//                        closingFilter += $" And v.VoucherOn between '{fiscalYearNext}' And '{dateTo}'";
//                    }
//                    else
//                    {
//                        openingFilter += " And v.VoucherOn ='1900-01-01'";
//                        closingFilter += $" And v.VoucherOn between '{dateFrom}' And '{dateTo}'";
//                    }
//                }
//            }
//            else
//            {
//                openingFilter += $" And v.VoucherOn < '{dateFrom}'";
//                closingFilter += $" And v.VoucherOn <= '{dateTo}'";
//            }

//            if (!string.IsNullOrEmpty(request.BranchIds) &&
//                request.BranchIds != "-1" &&
//                request.BranchIds != "string")
//            {
//                openingFilter += $" And v.UsmOfficeId in ({request.BranchIds})";
//                closingFilter += $" And v.UsmOfficeId in ({request.BranchIds})";
//            }

//            return (openingFilter, closingFilter);
//        }

//        private static string BuildSqlFilterMainLedger(FirstLedgerDetailsRequestDto request)
//        {
//            var mainLedger = request.LedgerName ?? string.Empty;
//            var subLedger1 = request.SubLedgerName ?? string.Empty;

//            var filter = $"And MainLedger = '{mainLedger}'";
//            filter += $" And SubLedger1 = N'{subLedger1}'";
//            return filter;
//        }

//        private string BuildSqlOrderBy(FirstLedgerDetailsRequestDto request)
//        {
//            var orderBy = " order by VoucherOn asc";

//            if (string.IsNullOrEmpty(request.OrderBy) || request.OrderBy == "-1")
//            {
//                return orderBy;
//            }

//            return request.OrderBy.ToLower() switch
//            {
//                "main ledger" => orderBy + " , MainLedger",
//                "sub ledger" => orderBy + " , SubLedger1",
//                "debit amount" => orderBy + " , DebitAmount DESC",
//                "credit amount" => orderBy + " , CreditAmount DESC",
//                "balance" => orderBy + " , BalanceAmount DESC",
//                _ => orderBy
//            };
//        }

//        // --------------------------------------------------------------
//        // Root-cause fix: rather than hard-coding which parameters the SP
//        // declares (which caused "too many arguments specified" once the
//        // deployed SP turned out not to match the webform's 9-parameter
//        // assumption), read the SP's actual parameter list from
//        // sys.parameters at call time and only send parameters that
//        // genuinely exist on it. This makes the repository self-correct
//        // regardless of which version of the SP is deployed, instead of
//        // silently breaking again the next time the SP's signature drifts.
//        // --------------------------------------------------------------
//        private static async Task<HashSet<string>> GetStoredProcedureParameterNamesAsync(
//            SqlConnection connection, string procedureName)
//        {
//            const string sql = @"
//                SELECT p.name
//                FROM sys.parameters p
//                INNER JOIN sys.objects o ON p.object_id = o.object_id
//                WHERE o.name = @ProcedureName AND o.type = 'P'";

//            var names = await connection.QueryAsync<string>(sql, new { ProcedureName = procedureName });
//            return new HashSet<string>(names, StringComparer.OrdinalIgnoreCase);
//        }

//        public async Task<FirstLedgerDetailsData> GetFirstLedgerDetailsDataAsync(FirstLedgerDetailsRequestDto request)
//        {
//            try
//            {
//                if (string.IsNullOrEmpty(request.FromDate) || string.IsNullOrEmpty(request.ToDate)
//                    || request.FromDate == "-1" || request.ToDate == "-1")
//                {
//                    throw new ArgumentException("FromDate and ToDate are required.");
//                }

//                var connectionString = _context.Database.GetConnectionString();
//                using var connection = new SqlConnection(connectionString);
//                await connection.OpenAsync();

//                var dateFromAd = await _dateConverter.BsToAdStringAsync(request.FromDate);
//                var dateToAd = await _dateConverter.BsToAdStringAsync(request.ToDate);

//                var sqlFilterExp = await BuildSqlFilterExp(request);
//                var sqlOrderBy = BuildSqlOrderBy(request);
//                var (sqlFilterExpOpening, sqlFilterExpClosing) =
//                    await BuildOpeningClosingFilterExp(connection, request, dateFromAd, dateToAd);
//                var sqlFilterMainLedger = BuildSqlFilterMainLedger(request);

//                string? accountTypeName = null;
//                if (request.LedgerHeadId > 0)
//                {
//                    accountTypeName = await connection.QueryFirstOrDefaultAsync<string>(
//                        "SELECT AccountType FROM AcoAccountType WHERE AcoAccountTypeId = @LedgerHeadId",
//                        new { LedgerHeadId = request.LedgerHeadId });
//                }

//                var isSummary = request.ReportType == "Summary";
//                var spName = isSummary ? "sp_6_56_GetLedgerDetailsSummary" : "sp_6_56_GetLedgerDetails";

//                // Discover which parameters this SP actually declares before building
//                // the call — avoids "too many arguments specified" when the deployed
//                // SP doesn't match the assumed 9-parameter shape from the webform.
//                var declaredParams = await GetStoredProcedureParameterNamesAsync(connection, spName);

//                var parameters = new DynamicParameters();

//                void AddIfDeclared(string name, object value, DbType dbType,
//                    ParameterDirection direction = ParameterDirection.Input, int? size = null)
//                {
//                    if (declaredParams.Contains(name))
//                    {
//                        parameters.Add(name, value, dbType, direction, size);
//                    }
//                    else
//                    {
//                        _logger.LogWarning(
//                            "{ProcedureName} does not declare parameter {ParameterName} — skipping (deployed SP signature differs from expected).",
//                            spName, name);
//                    }
//                }

//                AddIfDeclared("@SqlFilterExp", sqlFilterExp, DbType.String, size: -1);
//                AddIfDeclared("@SqlFilterExpOrderBy", sqlOrderBy, DbType.String, size: -1);
//                AddIfDeclared("@SqlFilterExpOpening", sqlFilterExpOpening, DbType.String, size: -1);
//                AddIfDeclared("@SqlFilterExpClosing", sqlFilterExpClosing, DbType.String, size: -1);
//                AddIfDeclared("@openingBalance", 0d, DbType.Double, ParameterDirection.Output);
//                AddIfDeclared("@closingBalance", 0d, DbType.Double, ParameterDirection.Output);
//                AddIfDeclared("@SqlFilterExpAccountType", "null", DbType.String, ParameterDirection.Output, size: 15);
//                AddIfDeclared("@SqlFilterMainLedger", sqlFilterMainLedger, DbType.String, size: -1);
//                // Both "ShowOpeningBalance" and "@ShowOpeningBalance" are checked since the
//                // legacy webform's SqlParameter used the name without a leading "@" —
//                // sys.parameters always reports names WITH the "@" prefix, so this covers
//                // either convention safely.
//                AddIfDeclared("@ShowOpeningBalance", request.ShowOpeningBalance, DbType.Boolean);

//                var rows = (await connection.QueryAsync<FirstLedgerDetailsRowDto>(
//                    spName,
//                    parameters,
//                    commandType: CommandType.StoredProcedure,
//                    commandTimeout: 120
//                )).ToList();

//                double openingBalanceOut = 0d;
//                double closingBalanceOut = 0d;
//                if (declaredParams.Contains("@openingBalance"))
//                {
//                    openingBalanceOut = parameters.Get<double>("@openingBalance");
//                }
//                if (declaredParams.Contains("@closingBalance"))
//                {
//                    closingBalanceOut = parameters.Get<double>("@closingBalance");
//                }

//                var totalDebit = rows.Sum(r => r.DebitAmount ?? 0);
//                var totalCredit = rows.Sum(r => r.CreditAmount ?? 0);
//                var totalBalance = isSummary ? totalDebit - totalCredit : totalCredit - totalDebit;

//                var data = new FirstLedgerDetailsData
//                {
//                    Rows = rows,
//                    TotalRecords = rows.Count,
//                    TotalDebitAmount = totalDebit,
//                    TotalCreditAmount = totalCredit,
//                    TotalBalance = totalBalance,
//                    OpeningBalance = (decimal)openingBalanceOut,
//                    ClosingBalance = (decimal)closingBalanceOut,
//                    FromDateBs = request.FromDate,
//                    ToDateBs = request.ToDate,
//                    LedgerName = request.LedgerName,
//                    SubLedgerName = request.SubLedgerName,
//                    VoucherType = request.VoucherType,
//                    ReportType = request.ReportType,
//                    ShowOpeningBalance = request.ShowOpeningBalance,
//                    OrderBy = request.OrderBy,
//                    LedgerHeadName = accountTypeName
//                };

//                return data;
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, "Error in GetFirstLedgerDetailsDataAsync");
//                throw;
//            }
//        }
//    }
//}




// Repository/Account/FirstLedgerDetailsReport/FirstLedgerDetailsRepository.cs
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using NexgenCosysReport.DbContext;
using NexgenCosysReport.Dtos.RequestDtos.Account.FirstLedgerDetailsReport;
using NexgenCosysReport.Inteface.ServiceInterface.Account.FirstLedgerDetailsReport;
using NexgenCosysReport.Inteface.ServiceInterface.Common;
using System.Data;

namespace NexgenCosysReport.Repository.Account.FirstLedgerDetailsReport
{
    public class FirstLedgerDetailsRepository : IFirstLedgerDetailsRepository
    {
        private readonly AppDbContext _context;
        private readonly IDateConverterService _dateConverter;
        private readonly ILogger<FirstLedgerDetailsRepository> _logger;

        public FirstLedgerDetailsRepository(
            AppDbContext context,
            IDateConverterService dateConverter,
            ILogger<FirstLedgerDetailsRepository> logger)
        {
            _context = context;
            _dateConverter = dateConverter;
            _logger = logger;
        }

        private async Task<string> BuildSqlFilterExp(FirstLedgerDetailsRequestDto request)
        {
            var filter = string.Empty;

            if (!string.IsNullOrEmpty(request.FromDate) && !string.IsNullOrEmpty(request.ToDate)
                && request.FromDate != "-1" && request.ToDate != "-1")
            {
                string fromDateAd = await _dateConverter.BsToAdStringAsync(request.FromDate);
                string toDateAd = await _dateConverter.BsToAdStringAsync(request.ToDate);

                if (!string.IsNullOrEmpty(fromDateAd) && !string.IsNullOrEmpty(toDateAd))
                {
                    filter += $" AND v.VoucherOn BETWEEN '{fromDateAd}' AND '{toDateAd}'";
                }
            }

            if (!string.IsNullOrEmpty(request.BranchIds) &&
                request.BranchIds != "-1" &&
                request.BranchIds != "string")
            {
                filter += $" AND v.UsmOfficeId IN ({request.BranchIds})";
            }

            if (!string.IsNullOrEmpty(request.VoucherType) && request.VoucherType != "All")
            {
                bool isAuto = request.VoucherType == "Auto";
                filter += $" AND vp.IsAutomatic = {(isAuto ? "1" : "0")}";
            }

            return filter;
        }

        private async Task<(string opening, string closing)> BuildOpeningClosingFilterExp(
            SqlConnection connection,
            FirstLedgerDetailsRequestDto request,
            string dateFrom,
            string dateTo)
        {
            var openingFilter = string.Empty;
            var closingFilter = string.Empty;

            if (request.LedgerHeadId == 3 || request.LedgerHeadId == 4)
            {
                var accountYearClosing = await connection.QueryFirstOrDefaultAsync<int?>(
                    "SELECT MAX(AcoFiscalYearId) FROM AcoAccountYearClosing") ?? 0;

                if (accountYearClosing == 0)
                {
                    openingFilter += $" And v.VoucherOn < '{dateFrom}'";
                    closingFilter += $" And v.VoucherOn <= '{dateTo}'";
                }
                else
                {
                    var fiscalYearToOn = await connection.QueryFirstOrDefaultAsync<DateTime?>(
                        "SELECT FiscalYearToOn FROM AcoFiscalYear WHERE AcoFiscalYearId = @Id",
                        new { Id = accountYearClosing });

                    var dateFromParsed = DateTime.Parse(dateFrom);

                    if (fiscalYearToOn.HasValue && fiscalYearToOn.Value < dateFromParsed.AddDays(-1))
                    {
                        var fiscalYearNext = fiscalYearToOn.Value.AddDays(1).ToString("yyyy-MM-dd");
                        openingFilter += $" And v.VoucherOn between '{fiscalYearNext}' And '{dateFrom}'";
                        closingFilter += $" And v.VoucherOn between '{fiscalYearNext}' And '{dateTo}'";
                    }
                    else
                    {
                        openingFilter += " And v.VoucherOn ='1900-01-01'";
                        closingFilter += $" And v.VoucherOn between '{dateFrom}' And '{dateTo}'";
                    }
                }
            }
            else
            {
                openingFilter += $" And v.VoucherOn < '{dateFrom}'";
                closingFilter += $" And v.VoucherOn <= '{dateTo}'";
            }

            if (!string.IsNullOrEmpty(request.BranchIds) &&
                request.BranchIds != "-1" &&
                request.BranchIds != "string")
            {
                openingFilter += $" And v.UsmOfficeId in ({request.BranchIds})";
                closingFilter += $" And v.UsmOfficeId in ({request.BranchIds})";
            }

            return (openingFilter, closingFilter);
        }

        private static string BuildSqlFilterMainLedger(FirstLedgerDetailsRequestDto request)
        {
            var mainLedger = request.LedgerName ?? string.Empty;
            var subLedger1 = request.SubLedgerName ?? string.Empty;

            var filter = $"And MainLedger = '{mainLedger}'";
            filter += $" And SubLedger1 = N'{subLedger1}'";
            return filter;
        }

        private string BuildSqlOrderBy(FirstLedgerDetailsRequestDto request)
        {
            var orderBy = " order by VoucherOn asc";

            if (string.IsNullOrEmpty(request.OrderBy) || request.OrderBy == "-1")
            {
                return orderBy;
            }

            return request.OrderBy.ToLower() switch
            {
                "main ledger" => orderBy + " , MainLedger",
                "sub ledger" => orderBy + " , SubLedger1",
                "debit amount" => orderBy + " , DebitAmount DESC",
                "credit amount" => orderBy + " , CreditAmount DESC",
                "balance" => orderBy + " , BalanceAmount DESC",
                _ => orderBy
            };
        }



        public async Task<FirstLedgerDetailsData> GetFirstLedgerDetailsDataAsync(FirstLedgerDetailsRequestDto request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.FromDate) || string.IsNullOrEmpty(request.ToDate)
                    || request.FromDate == "-1" || request.ToDate == "-1")
                {
                    throw new ArgumentException("FromDate and ToDate are required.");
                }

                var connectionString = _context.Database.GetConnectionString();
                using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();

                var dateFromAd = await _dateConverter.BsToAdStringAsync(request.FromDate);
                var dateToAd = await _dateConverter.BsToAdStringAsync(request.ToDate);

                var sqlFilterExp = await BuildSqlFilterExp(request);
                var sqlOrderBy = BuildSqlOrderBy(request);
                var (sqlFilterExpOpening, sqlFilterExpClosing) =
                    await BuildOpeningClosingFilterExp(connection, request, dateFromAd, dateToAd);
                var sqlFilterMainLedger = BuildSqlFilterMainLedger(request);

                string? accountTypeName = null;
                if (request.LedgerHeadId > 0)
                {
                    accountTypeName = await connection.QueryFirstOrDefaultAsync<string>(
                        "SELECT AccountType FROM AcoAccountType WHERE AcoAccountTypeId = @LedgerHeadId",
                        new { LedgerHeadId = request.LedgerHeadId });
                }

                var isSummary = request.ReportType == "Summary";
                var spName = isSummary ? "sp_6_56_GetLedgerDetailsSummary" : "sp_6_56_GetLedgerDetails";

                // Confirmed parameter list — both SPs declare the same 8 parameters,
                // in this exact order, per the deployed procedure bodies.
                var parameters = new DynamicParameters();
                parameters.Add("@SqlFilterExp", sqlFilterExp, DbType.String, size: -1);
                parameters.Add("@SqlFilterExpOrderBy", sqlOrderBy, DbType.String, size: -1);
                parameters.Add("@SqlFilterExpOpening", sqlFilterExpOpening, DbType.String, size: -1);
                parameters.Add("@SqlFilterExpClosing", sqlFilterExpClosing, DbType.String, size: -1);
                parameters.Add("@openingBalance", dbType: DbType.Decimal, direction: ParameterDirection.Output);
                parameters.Add("@closingBalance", dbType: DbType.Decimal, direction: ParameterDirection.Output);
                parameters.Add("@SqlFilterExpAccountType", dbType: DbType.String, direction: ParameterDirection.Output, size: -1);
                parameters.Add("@SqlFilterMainLedger", sqlFilterMainLedger, DbType.String, size: -1);

                var rows = (await connection.QueryAsync<FirstLedgerDetailsRowDto>(
                    spName,
                    parameters,
                    commandType: CommandType.StoredProcedure,
                    commandTimeout: 120
                )).ToList();

                var openingBalanceOut = parameters.Get<decimal?>("@openingBalance") ?? 0m;
                var closingBalanceOut = parameters.Get<decimal?>("@closingBalance") ?? 0m;
                var accountTypeOut = parameters.Get<string?>("@SqlFilterExpAccountType")
                                     ?? rows.FirstOrDefault()?.AccountType
                                     ?? accountTypeName;

                var totalDebit = rows.Sum(r => r.DebitAmount ?? 0);
                var totalCredit = rows.Sum(r => r.CreditAmount ?? 0);
                var totalBalance = isSummary ? totalDebit - totalCredit : totalCredit - totalDebit;

                var branchNames = await ResolveBranchNamesAsync(connection, request.BranchIds);
                var data = new FirstLedgerDetailsData
                {
                    Rows = rows,
                    TotalRecords = rows.Count,
                    TotalDebitAmount = totalDebit,
                    TotalCreditAmount = totalCredit,
                    TotalBalance = totalBalance,
                    OpeningBalance = openingBalanceOut,
                    ClosingBalance = closingBalanceOut,
                    AccountType = accountTypeOut,
                    FromDateBs = request.FromDate,
                    ToDateBs = request.ToDate,
                    LedgerName = request.LedgerName,
                    SubLedgerName = request.SubLedgerName,
                    VoucherType = request.VoucherType,
                    ReportType = request.ReportType,
                    ShowOpeningBalance = request.ShowOpeningBalance,
                    OrderBy = request.OrderBy,
                    LedgerHeadName = accountTypeName,
                    BranchNames = branchNames
                };

                return data;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetFirstLedgerDetailsDataAsync");
                throw;
            }
        }

        private static async Task<string> ResolveBranchNamesAsync(SqlConnection connection, string? BranchIds)
        {
            if (string.IsNullOrEmpty(BranchIds) || BranchIds == "-1" || BranchIds == "string")
            {
                return "All Branches";
            }

            var BranchIdsList = BranchIds
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(id => long.TryParse(id, out var parsed) ? (long?)parsed : null)
                .Where(id => id.HasValue)
                .Select(id => id!.Value)
                .Distinct()
                .ToList();

            if (BranchIdsList.Count == 0)
            {
                return "All Branches";
            }

            const string sql = @"
                SELECT OfficeName
                FROM UsmOffice
                WHERE UsmOfficeId IN @Ids
                ORDER BY OfficeName";

            var names = (await connection.QueryAsync<string>(sql, new { Ids = BranchIdsList })).ToList();

            return names.Count > 0 ? string.Join(", ", names) : "All Branches";
        }
    }
}