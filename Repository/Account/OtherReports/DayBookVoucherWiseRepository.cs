
// Repositories/Implementations/AccountOperation/DayBookVoucherWiseRepository.cs
using Dapper;
using global::NexgenCosysReport.DbContext;
using global::NexgenCosysReport.Dtos.RequestDtos.Account.OtherReports;
using global::NexgenCosysReport.Inteface.ServiceInterface.AccountOperation.OthersReport;
using global::NexgenCosysReport.Inteface.ServiceInterface.Common;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
//using NexgenCosysReport.Dtos.RequestDtos.AccountOperation.OthersReport;
using System.Data;
using System.Text;
namespace NexgenCosysReport.Repository.Account.OtherReports
{
    public class DayBookVoucherWiseRepository : IDayBookVoucherWiseRepository
    {
        private readonly AppDbContext _context;
        private readonly IDateConverterService _dateConverter;
        private readonly ILogger<DayBookVoucherWiseRepository> _logger;

        public DayBookVoucherWiseRepository(
            AppDbContext context,
            IDateConverterService dateConverter,
            ILogger<DayBookVoucherWiseRepository> logger)
        {
            _context = context;
            _dateConverter = dateConverter;
            _logger = logger;
        }

        // --------------------------------------------------------------
        // @SqlFilterExp
        // Appended inside SP as: WHERE vp.IsActive=1 AND vp.DebitCredit='true' + @SqlFilterExp
        // Uses v.VoucherOn and v.UsmOfficeId (aliased "v")
        // --------------------------------------------------------------
        private async Task<string> BuildSqlFilterExp(DayBookVoucherWiseRequestDto request)
        {
            var filter = new StringBuilder();

            if (!string.IsNullOrEmpty(request.FromDateBs) && !string.IsNullOrEmpty(request.ToDateBs)
                && request.FromDateBs != "-1" && request.ToDateBs != "-1")
            {
                var fromDateAd = await _dateConverter.NepaliToEnglishAsync(request.FromDateBs);
                var toDateAd = await _dateConverter.NepaliToEnglishAsync(request.ToDateBs);

                // ISO format avoids SQL Server regional/language ambiguity for string->datetime literals
                var fromDateStr = fromDateAd.ToString("yyyy-MM-dd");
                var toDateStr = toDateAd.ToString("yyyy-MM-dd");

                filter.Append(" And v.VoucherOn between '")
                      .Append(fromDateStr).Append("' And '")
                      .Append(toDateStr).Append("' ");
            }

            if (!string.IsNullOrEmpty(request.BranchId) &&
                request.BranchId != "-1" &&
                request.BranchId != "string" &&
                long.TryParse(request.BranchId, out var branchId))
            {
                filter.Append(" And v.UsmOfficeId = ").Append(branchId);
            }

            return filter.ToString();
        }

        // --------------------------------------------------------------
        // @SqlFilterExpOrderBy
        // Applied to the SP's final aggregated SELECT — column names
        // must match that outer query's aliases (VoucherNo, VoucherDate,
        // Narration, Amount) — note the SP's inner temp table does NOT
        // have a "Type" column, so ordering by Type in the outer query
        // works only because Type is computed in that same outer SELECT.
        // --------------------------------------------------------------
        private static string BuildSqlOrderBy(DayBookVoucherWiseRequestDto request)
        {
            if (string.IsNullOrEmpty(request.OrderBy) ||
                request.OrderBy == "-1" ||
                request.OrderBy == "string")
            {
                return string.Empty; // legacy default — no explicit ORDER BY when nothing selected
            }

            return request.OrderBy.Trim().ToLower() switch
            {
                "voucher date" => " order by VoucherDate",
                "voucher no" => " order by VoucherNo",
                "narration" => " order by Narration",
                "type" => " order by Type",
                "amount" => " order by Amount DESC",
                _ => string.Empty
            };
        }

        public async Task<DayBookVoucherWiseData> GetReportDataAsync(DayBookVoucherWiseRequestDto request)
        {
            try
            {
                var sqlFilterExp = await BuildSqlFilterExp(request);
                var sqlFilterExpOrderBy = BuildSqlOrderBy(request);

                var connectionString = _context.Database.GetConnectionString();
                using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();

                // ---- SP declares two separate parameters ----
                var parameters = new DynamicParameters();
                parameters.Add("@SqlFilterExp", sqlFilterExp, DbType.String, size: -1);
                parameters.Add("@SqlFilterExpOrderBy", sqlFilterExpOrderBy, DbType.String, size: -1);

                var rows = await connection.QueryAsync<DayBookVoucherWiseRowDto>(
                    "sp_6_56_GetDayBookVoucherWise",
                    parameters,
                    commandType: CommandType.StoredProcedure,
                    commandTimeout: 120
                );

                var resultList = rows.AsList();

                string branchName = "All";
                if (!string.IsNullOrEmpty(request.BranchId) &&
                    request.BranchId != "-1" &&
                    request.BranchId != "string" &&
                    long.TryParse(request.BranchId, out var branchId))
                {
                    var name = await connection.QueryFirstOrDefaultAsync<string>(
                        "SELECT OfficeName FROM UsmOffice WHERE UsmOfficeId = @BranchId",
                        new { BranchId = branchId });

                    branchName = string.IsNullOrEmpty(name) ? "All" : name;
                }

                return new DayBookVoucherWiseData
                {
                    Rows = resultList,
                    TotalRecords = resultList.Count,
                    TotalAmount = resultList.Sum(r => r.Amount ?? 0),
                    FromDateBs = request.FromDateBs,
                    ToDateBs = request.ToDateBs,
                    BranchName = branchName,
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