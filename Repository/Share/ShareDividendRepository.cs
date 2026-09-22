using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using NexgenCosysReport.DbContext;
using NexgenCosysReport.Dtos.RequestDtos.Share;
using NexgenCosysReport.Inteface.ServiceInterface.Common;
using NexgenCosysReport.Inteface.ServiceInterface.Share;
using System.Data;
using System.Text;

namespace NexgenCosysReport.Repository.Share
{
    public class ShareDividendRepository : IShareDividend
    {
        private readonly AppDbContext _context;
        private readonly IDateConverterService _dateConverter;
        private readonly ILogger<ShareDividendRepository> _logger;

        public ShareDividendRepository(
            AppDbContext context,
            IDateConverterService dateConverter,
            ILogger<ShareDividendRepository> logger)
        {
            _context = context;
            _dateConverter = dateConverter;
            _logger = logger;
        }

        private static string BuildSqlOrderBy(ShareDividendRequestDto request)
        {
            if (string.IsNullOrEmpty(request.OrderBy) || request.OrderBy == "-1")
                return string.Empty;

            return request.OrderBy.Trim() switch
            {
                "MemberId" => " order by substring(MemberId, 1,(len(MemberId)-charindex('-', MemberId))-1), MemberId ",
                "MemberName" => " order by MemberName",
                "PurchaseDate" => " order by PurchaseDate",
                "PurchaseAmount" => " order by PurchaseAmount DESC",
                "HoldingNoDays" => " order by HoldingNoDays DESC",
                "AggregateAmount" => " order by AggregateAmount DESC",
                _ => string.Empty
            };
        }

        public async Task<ShareDividendData> GetReportDataAsync(ShareDividendRequestDto request)
        {
            try
            {
                var connectionString = _context.Database.GetConnectionString();
                using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();

                var fiscalYear = await connection.QueryFirstOrDefaultAsync<(string? FiscalYear, string? FromBs, string? ToBs)>(
                    "SELECT FiscalYear, FiscalYearFromOnBs, FiscalYearToOnBs FROM AcoFiscalYear WHERE AcoFiscalYearId = @Id",
                    new { Id = request.FiscalYearId });

                if (string.IsNullOrEmpty(fiscalYear.FiscalYear))
                {
                    throw new ArgumentException("Invalid fiscal year");
                }

                var fromDateAd = await _dateConverter.NepaliToEnglishAsync(fiscalYear.FromBs);
                var toDateAd = await _dateConverter.NepaliToEnglishAsync(fiscalYear.ToBs);

                var sqlFilterExp = new StringBuilder();
                var sqlFilterExpLedger = new StringBuilder();
                string? officeName = null;
                string? shareTypeName = null;
                string? memberGroupName = null;

                if (request.OfficeId != -1)
                {
                    sqlFilterExp.Append(" And o.UsmOfficeId = ").Append(request.OfficeId);
                    sqlFilterExpLedger.Append(" And v.UsmOfficeId = ").Append(request.OfficeId);

                    officeName = await connection.QueryFirstOrDefaultAsync<string>(
                        "SELECT OfficeName FROM UsmOffice WHERE UsmOfficeId = @Id",
                        new { Id = request.OfficeId });
                }

                if (request.MemberGroupId != -1)
                {
                    sqlFilterExp.Append(" AND m.SycMemberGroupId = ").Append(request.MemberGroupId);
                    memberGroupName = await connection.QueryFirstOrDefaultAsync<string>(
                        "SELECT Name FROM SycMemberGroup WHERE SycMemberGroupId = @Id",
                        new { Id = request.MemberGroupId });
                }

                if (request.ShareTypeId != -1)
                {
                    sqlFilterExp.Append(" And s.ShmShareTypeId = ").Append(request.ShareTypeId);
                    shareTypeName = await connection.QueryFirstOrDefaultAsync<string>(
                        "SELECT ShareTypeName FROM ShmShareType WHERE ShmShareTypeId = @Id",
                        new { Id = request.ShareTypeId });
                }

                var parameters = new DynamicParameters();
                parameters.Add("@SqlFilterExp", sqlFilterExp.ToString(), DbType.String, size: -1);
                parameters.Add("@SqlFilterExpLedger", sqlFilterExpLedger.ToString(), DbType.String, size: -1);
                parameters.Add("@SqlFilterExpOrderBy", BuildSqlOrderBy(request), DbType.String, size: -1);
                parameters.Add("@Fromdate", fromDateAd.ToString("yyyy-MM-dd"), DbType.String, size: 10);
                parameters.Add("@ToDate", toDateAd.ToString("yyyy-MM-dd"), DbType.String, size: 10);
                parameters.Add("@FromdateBs", fiscalYear.FromBs ?? "", DbType.String, size: 10);
                parameters.Add("@ToDateBs", fiscalYear.ToBs ?? "", DbType.String, size: 10);
                // Output parameters are read back as decimal? below (see comment there) -
                // the SP can genuinely return DBNull for these (e.g. SUM() with no matching
                // rows, or a reserve-fund lookup that finds no row), so the DbType/precision
                // registration here stays the same, but Dapper.Get<T> must be nullable.
                parameters.Add("@ShareDividenPercent", dbType: DbType.Decimal, direction: ParameterDirection.Output, precision: 18, scale: 2);
                parameters.Add("@RemainingReserveAmount", dbType: DbType.Decimal, direction: ParameterDirection.Output, precision: 18, scale: 2);
                parameters.Add("@DividendAmount", dbType: DbType.Decimal, direction: ParameterDirection.Output, precision: 18, scale: 2);
                parameters.Add("@TotalPurchaseAmount", dbType: DbType.Decimal, direction: ParameterDirection.Output, precision: 18, scale: 2);

                var rows = await connection.QueryAsync<ShareDividendRowDto>(
                    "sp_8_14_GetShareDividend",
                    parameters,
                    commandType: CommandType.StoredProcedure,
                    commandTimeout: 180
                );

                var resultList = rows.AsList();

                // --------------------------------------------------------------
                // FIX: read as decimal? and coalesce to 0. The SP can return
                // DBNull for any of these (most obviously @TotalPurchaseAmount,
                // whose SUM() has no ISNULL guard and returns NULL when no rows
                // match; @ShareDividenPercent similarly has no fallback if its
                // AcoReserveFundMaster lookup finds nothing). Reading them as
                // non-nullable decimal, as before, throws
                // "Unable to cast a DBNull to a non nullable type" the moment
                // that happens.
                // --------------------------------------------------------------
                var shareDividendPercent = parameters.Get<decimal?>("@ShareDividenPercent") ?? 0;
                var remainingReserveAmount = parameters.Get<decimal?>("@RemainingReserveAmount") ?? 0;
                var dividendAmount = parameters.Get<decimal?>("@DividendAmount") ?? 0;
                var totalPurchaseAmount = parameters.Get<decimal?>("@TotalPurchaseAmount") ?? 0;

                return new ShareDividendData
                {
                    Rows = resultList,
                    TotalRecords = resultList.Count,
                    TotalMembers = resultList
                        .Select(r => r.MemberId)
                        .Distinct()
                        .Count(),
                    TotalPurchaseAmount = totalPurchaseAmount,
                    TotalAggregateAmount = resultList.Sum(r => r.AggregateAmount ?? 0),
                    TotalDividendPayableAmount = resultList.Sum(r => r.DividendPayableAmount ?? 0),
                    ShareDividendPercent = shareDividendPercent,
                    DividendAmount = dividendAmount,
                    RemainingReserveAmount = remainingReserveAmount,
                    FiscalYear = fiscalYear.FiscalYear,
                    FromDateBs = fiscalYear.FromBs,
                    ToDateBs = fiscalYear.ToBs,
                    OfficeName = officeName ?? "All",
                    ShareTypeName = shareTypeName ?? "All",
                    MemberGroupName = memberGroupName,
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