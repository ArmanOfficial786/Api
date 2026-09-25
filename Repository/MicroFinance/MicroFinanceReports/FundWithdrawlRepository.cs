
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using NexgenCosysReport.DbContext;
using NexgenCosysReport.Dtos.RequestDtos.MicroFinance.MicroFinanceReports;
using NexgenCosysReport.Inteface.ServiceInterface.Common;
using NexgenCosysReport.Inteface.ServiceInterface.Microfinance.MicrofinanceReport;
using System.Data;

namespace NexgenCosysReport.Repository.Microfinance.MicrofinanceReport
{
    public class FundWithdrawlRepository : IFundWithdrawlRepository
    {
        private readonly AppDbContext _context;
        private readonly IDateConverterService _dateConverter;
        private readonly ILogger<FundWithdrawlRepository> _logger;

        public FundWithdrawlRepository(
            AppDbContext context,
            IDateConverterService dateConverter,
            ILogger<FundWithdrawlRepository> logger)
        {
            _context = context;
            _dateConverter = dateConverter;
            _logger = logger;
        }

        private static string SanitizeIdList(string? ids)
        {
            if (string.IsNullOrWhiteSpace(ids) || ids == "-1" || ids == "string")
                return "-1";

            var validIds = ids
                .Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Where(id => long.TryParse(id, out _));

            var joined = string.Join(",", validIds);
            return string.IsNullOrEmpty(joined) ? "-1" : joined;
        }


        private static readonly HashSet<string> AllowedOrderBy = new(StringComparer.OrdinalIgnoreCase)
        {
            "CollectionCenterShortCode",
            "MemberId",
            "Name",
            "PermanentAddessDetail",
            "TransactionOnBs",
            "CashWithdrawl"
        };

        private static string BuildOrderBy(string? orderBy)
        {
            if (string.IsNullOrWhiteSpace(orderBy) || orderBy == "-1")
                return "Name";

            var trimmed = orderBy.Trim();
            return AllowedOrderBy.Contains(trimmed) ? trimmed : "Name";
        }

        private static string GetReportModeName(string reportMode) =>
            reportMode == "0" ? "Detail" : "Summary";

        public async Task<FundWithdrawlData> GetReportDataAsync(FundWithdrawlRequestDto request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.FromDateBs) || request.FromDateBs == "-1")
                    throw new ArgumentException("From Date is required.");

                if (string.IsNullOrWhiteSpace(request.ToDateBs) || request.ToDateBs == "-1")
                    throw new ArgumentException("To Date is required.");

                var branchIds = SanitizeIdList(request.BranchIds);
                if (branchIds == "-1")
                    throw new ArgumentException("Please select Branch Name.");

                var connectionString = _context.Database.GetConnectionString();
                using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();

                var fromDateAd = await _dateConverter.NepaliToEnglishAsync(request.FromDateBs);
                var toDateAd = await _dateConverter.NepaliToEnglishAsync(request.ToDateBs);

                var orderBy = BuildOrderBy(request.OrderBy);

                var parameters = new DynamicParameters();
                parameters.Add("@FromDate", fromDateAd.Date, DbType.Date);
                parameters.Add("@ToDate", toDateAd.Date, DbType.Date);
                parameters.Add("@BranchIds", branchIds, DbType.String, size: 500);
                parameters.Add("@OrderBy", orderBy, DbType.String, size: 500);

                var rows = (await connection.QueryAsync<FundWithdrawlRowDto>(
                    "sp_5_113_FundWithdrawlReport",
                    parameters,
                    commandType: CommandType.StoredProcedure,
                    commandTimeout: 600
                )).AsList();


                string branchName = "All";
                if (branchIds != "-1")
                {
                    var names = await connection.QueryAsync<string>(
                        $"SELECT OfficeName FROM UsmOffice WHERE UsmOfficeId IN ({branchIds})");
                    var list = names.Where(n => !string.IsNullOrWhiteSpace(n)).ToList();
                    branchName = list.Count > 0 ? string.Join(", ", list) : "All";
                }

                return new FundWithdrawlData
                {
                    Rows = rows,
                    TotalRecords = rows.Count,
                    TotalCashWithdrawl = rows.Sum(r => r.CashWithdrawl ?? 0),
                    FromDateBs = request.FromDateBs,
                    ToDateBs = request.ToDateBs,
                    FromDateAd = fromDateAd.ToString("yyyy-MM-dd"),
                    ToDateAd = toDateAd.ToString("yyyy-MM-dd"),
                    BranchName = branchName,
                    OrderBy = orderBy,
                    ReportMode = request.ReportMode,
                    ReportModeName = GetReportModeName(request.ReportMode)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetReportDataAsync (FundWithdrawl)");
                throw;
            }
        }
    }
}