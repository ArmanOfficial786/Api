using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using NexgenCosysReport.DbContext;
using NexgenCosysReport.Dtos.RequestDtos.Microfinance.CenterDetailReports;
using NexgenCosysReport.Inteface.ServiceInterface.Common;
using NexgenCosysReport.Inteface.ServiceInterface.Microfinance.CenterDetailReports;
using System.Data;

namespace NexgenCosysReport.Repository.Microfinance.CenterDetailReports
{
    public class CollectorWiseProjectionRepository : ICollectorWiseProjectionRepository
    {
        private readonly AppDbContext _context;
        private readonly IDateConverterService _dateConverter;
        private readonly ILogger<CollectorWiseProjectionRepository> _logger;

        public CollectorWiseProjectionRepository(
            AppDbContext context,
            IDateConverterService dateConverter,
            ILogger<CollectorWiseProjectionRepository> logger)
        {
            _context = context;
            _dateConverter = dateConverter;
            _logger = logger;
        }

        private record CalendarMonth(DateTime EnglishStartDate, DateTime EnglishEndDate);

        // Mirrors the legacy CComCalender.GetByNepaliYearAndMonthCode lookup —
        // same ComCalendar table used by other reports in this codebase
        // (e.g. sp_5_43_GetFixedDepositSchedule's month-boundary lookups).
        private static async Task<CalendarMonth> GetCalendarMonthAsync(
            SqlConnection connection, int nepaliYear, int monthCode)
        {
            var result = await connection.QueryFirstOrDefaultAsync<CalendarMonth>(
                "SELECT EnglishStartDate, EnglishEndDate FROM ComCalendar WHERE NepaliYear = @NepaliYear AND MonthCode = @MonthCode",
                new { NepaliYear = nepaliYear, MonthCode = monthCode });

            if (result is null)
                throw new ArgumentException($"Calendar data not found for Nepali year {nepaliYear}, month {monthCode}.");

            return result;
        }

        public async Task<CollectorWiseProjectionData> GetReportDataAsync(CollectorWiseProjectionRequestDto request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.TillDateBs) || request.TillDateBs == "-1")
                    throw new ArgumentException("Till Date is required.");

                if (string.IsNullOrWhiteSpace(request.BranchId) || request.BranchId == "-1")
                    throw new ArgumentException("Branch is required.");

                if (!long.TryParse(request.BranchId, out var branchId))
                    throw new ArgumentException("Invalid Branch Id.");

                var tillDateBs = request.TillDateBs.Trim();
                var tillYear = int.Parse(tillDateBs.Substring(0, 4));
                var tillMonth = int.Parse(tillDateBs.Substring(5, 2));
                var monthCode = tillDateBs.Substring(5, 2); // zero-padded, e.g. "05"

                var connectionString = _context.Database.GetConnectionString();
                using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();

                var tillDateAd = await _dateConverter.NepaliToEnglishAsync(tillDateBs);
                var tillDateAdStr = tillDateAd.ToString("yyyy-MM-dd");

                // ---- Previous month boundaries ----
                var prevNepaliYear = tillYear;
                var prevNepaliMonth = tillMonth;
                if (prevNepaliMonth == 1)
                {
                    prevNepaliYear -= 1;
                    prevNepaliMonth = 12;
                }
                else
                {
                    prevNepaliMonth -= 1;
                }

                var preMonth = await GetCalendarMonthAsync(connection, prevNepaliYear, prevNepaliMonth);
                var previousMonthStartDate = preMonth.EnglishStartDate.ToString("yyyy-MM-dd");
                var previousMonthLastDate = preMonth.EnglishEndDate.ToString("yyyy-MM-dd");
                var currentMonthStartDate = preMonth.EnglishEndDate.AddDays(1).ToString("yyyy-MM-dd");

                // ---- Fiscal year boundaries (Nepali FY starts month 4 / Shrawan) ----
                var fiscalYear = tillYear;
                var preFisNepaliYear = tillMonth <= 3 ? fiscalYear - 1 : fiscalYear;

                var preFiscalYearDate = await GetCalendarMonthAsync(connection, preFisNepaliYear, 3);
                var currFiscalYearStartDate = preFiscalYearDate.EnglishEndDate.AddDays(1).ToString("yyyy-MM-dd");

                var fiscalYearLabel = fiscalYear > preFisNepaliYear
                    ? $"{preFisNepaliYear}/{fiscalYear.ToString().Substring(2, 2)}"
                    : $"{fiscalYear}/{(preFisNepaliYear + 1).ToString().Substring(2, 2)}";

                // --------------------------------------------------------------
                // Root-cause fix: see explanation above the code — the SPs never add
                // their own quotes around these six string-literal-ish parameters
                // when concatenating them into dynamic SQL. Adding the quotes here
                // is what makes `ac.CreatedOn >= 'yyyy-MM-dd'` a valid date literal
                // instead of silently misparsed arithmetic. branchId and MonthCode
                // are deliberately left unquoted — they're compared against numeric
                // columns, and SQL Server's implicit numeric conversion handles
                // those correctly unquoted (same as the original C#).
                // --------------------------------------------------------------
                static string Q(string value) => $"'{value}'";

                var sqlExpFiscalYear = Q(fiscalYearLabel);
                var sqlExpPreviousMonthStartDate = Q(previousMonthStartDate);
                var sqlExpPreviousMonthLastDate = Q(previousMonthLastDate);
                var sqlExpCurrentMonthStartDate = Q(currentMonthStartDate);
                var sqlExpTillDate = Q(tillDateAdStr);
                var sqlExpCurrFiscalYearStartDate = Q(currFiscalYearStartDate);

                // ---- sp_4_11_GetCollectorWiseProjectionReport (Member + Loan) ----
                var memberLoanParams = new DynamicParameters();
                memberLoanParams.Add("@SqlExpFiscalYear", sqlExpFiscalYear, DbType.String, size: -1);
                memberLoanParams.Add("@SqlExppreviousMonthStartDate", sqlExpPreviousMonthStartDate, DbType.String, size: -1);
                memberLoanParams.Add("@SqlExppreviousMonthLastDate", sqlExpPreviousMonthLastDate, DbType.String, size: -1);
                memberLoanParams.Add("@SqlExpcurrentMonthStartDate", sqlExpCurrentMonthStartDate, DbType.String, size: -1);
                memberLoanParams.Add("@SqlExptillDate", sqlExpTillDate, DbType.String, size: -1);
                memberLoanParams.Add("@SqlExpcurrFiscalYearStartDate", sqlExpCurrFiscalYearStartDate, DbType.String, size: -1);
                memberLoanParams.Add("@SqlExpbranchId", branchId.ToString(), DbType.String, size: -1);
                memberLoanParams.Add("@SqlExpMonthCode", monthCode, DbType.String, size: -1);

                var memberLoanRows = (await connection.QueryAsync<CollectorWiseProjectionRowDto>(
                    "sp_4_11_GetCollectorWiseProjectionReport",
                    memberLoanParams,
                    commandType: CommandType.StoredProcedure,
                    commandTimeout: 300
                )).AsList();

                // ---- sp_4_11_GetCollectorWiseProjectionForSavingShareReport (Saving + Share) ----
                var savingShareParams = new DynamicParameters();
                savingShareParams.Add("@SqlExpFiscalYear", sqlExpFiscalYear, DbType.String, size: -1);
                savingShareParams.Add("@SqlExppreviousMonthStartDate", sqlExpPreviousMonthStartDate, DbType.String, size: -1);
                savingShareParams.Add("@SqlExppreviousMonthLastDate", sqlExpPreviousMonthLastDate, DbType.String, size: -1);
                savingShareParams.Add("@SqlExpcurrentMonthStartDate", sqlExpCurrentMonthStartDate, DbType.String, size: -1);
                savingShareParams.Add("@SqlExptillDate", sqlExpTillDate, DbType.String, size: -1);
                savingShareParams.Add("@SqlExpcurrFiscalYearStartDate", sqlExpCurrFiscalYearStartDate, DbType.String, size: -1);
                savingShareParams.Add("@SqlExpbranchId", branchId.ToString(), DbType.String, size: -1);
                savingShareParams.Add("@SqlExpMonthCode", monthCode, DbType.String, size: -1);

                var savingShareRows = (await connection.QueryAsync<CollectorWiseProjectionRowDto>(
                    "sp_4_11_GetCollectorWiseProjectionForSavingShareReport",
                    savingShareParams,
                    commandType: CommandType.StoredProcedure,
                    commandTimeout: 300
                )).AsList();

                // ---- Merge saving/share fields into the member/loan rows, by HurCollectorId ----
                var savingShareByCollector = savingShareRows
                    .Where(r => r.HurCollectorId.HasValue)
                    .ToDictionary(r => r.HurCollectorId!.Value);

                foreach (var row in memberLoanRows)
                {
                    if (row.HurCollectorId.HasValue &&
                        savingShareByCollector.TryGetValue(row.HurCollectorId.Value, out var ss))
                    {
                        row.SavingProjection = ss.SavingProjection;
                        row.ShareProjection = ss.ShareProjection;
                        row.SaUptoLstMProjected = ss.SaUptoLstMProjected;
                        row.SaUptoLstMAdd = ss.SaUptoLstMAdd;
                        row.SaThisMProjected = ss.SaThisMProjected;
                        row.SaThisMAdd = ss.SaThisMAdd;
                        row.SaUptoThisMProjected = ss.SaUptoThisMProjected;
                        row.SaUptoThisMAdd = ss.SaUptoThisMAdd;
                        row.ShUptoLstMProjected = ss.ShUptoLstMProjected;
                        row.ShUptoLstMAdd = ss.ShUptoLstMAdd;
                        row.ShThisMProjected = ss.ShThisMProjected;
                        row.ShThisMAdd = ss.ShThisMAdd;
                        row.ShUptoThisMProjected = ss.ShUptoThisMProjected;
                        row.ShUptoThisMAdd = ss.ShUptoThisMAdd;
                        row.BalanceSaving = ss.BalanceSaving;
                        row.BalanceShare = ss.BalanceShare;
                    }
                }

                var rows = memberLoanRows;

                var branchName = await connection.QueryFirstOrDefaultAsync<string>(
                    "SELECT OfficeName FROM UsmOffice WHERE UsmOfficeId = @Id", new { Id = branchId }) ?? "All Branch";

                return new CollectorWiseProjectionData
                {
                    Rows = rows,
                    TotalRecords = rows.Count,

                    TotalMemberProjection = rows.Sum(r => r.MemberProjection ?? 0),
                    TotalMThisMAdd = rows.Sum(r => r.MThisMAdd ?? 0),
                    TotalMUptoThisMAdd = rows.Sum(r => r.MUptoThisMAdd ?? 0),
                    TotalMUptoThisMRemove = rows.Sum(r => r.MUptoThisMRemove ?? 0),
                    TotalLoanProjection = rows.Sum(r => r.LoanProjection ?? 0),
                    TotalLUptoThisMAdd = rows.Sum(r => r.LUptoThisMAdd ?? 0),
                    TotalLUptoThisMProjected = rows.Sum(r => r.LUptoThisMProjected ?? 0),
                    TotalBalanceLoan = rows.Sum(r => r.BalanceLoan ?? 0),
                    TotalCenters = rows.Sum(r => r.TotalCenter ?? 0),
                    TotalMembers = rows.Sum(r => r.TotalMember ?? 0),

                    FiscalYear = fiscalYearLabel,
                    TillDateBs = tillDateBs,
                    TillDateAd = tillDateAdStr,
                    MonthCode = monthCode,
                    PreviousMonthStartDate = previousMonthStartDate,
                    PreviousMonthLastDate = previousMonthLastDate,
                    CurrentMonthStartDate = currentMonthStartDate,
                    CurrentFiscalYearStartDate = currFiscalYearStartDate,
                    BranchName = branchName
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetReportDataAsync (CollectorWiseProjection)");
                throw;
            }
        }
    }
}