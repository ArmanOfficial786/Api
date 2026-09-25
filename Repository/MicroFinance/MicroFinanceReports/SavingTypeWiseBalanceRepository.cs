// Repository/Microfinance/MicrofinanceReport/SavingTypeWiseBalanceRepository.cs
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
    public class SavingTypeWiseBalanceRepository : ISavingTypeWiseBalanceRepository
    {
        private readonly AppDbContext _context;
        private readonly IDateConverterService _dateConverter;
        private readonly ILogger<SavingTypeWiseBalanceRepository> _logger;

        public SavingTypeWiseBalanceRepository(
            AppDbContext context,
            IDateConverterService dateConverter,
            ILogger<SavingTypeWiseBalanceRepository> logger)
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

        private static string BuildSqlOrderBy(string? orderBy)
        {
            return orderBy?.Trim() switch
            {
                "SavingType" => " order by SavingType",
                "Deposit" => " order by Deposit DESC",
                "Withdraw" => " order by Withdraw DESC",
                "Balance" => " order by Balance DESC",
                _ => " order by SavingType"
            };
        }

        public async Task<SavingTypeWiseBalanceData> GetReportDataAsync(SavingTypeWiseBalanceRequestDto request)
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

                if (request.SameCompanyName)
                    branchIds = "-1";

                var collectionCenterIds = SanitizeIdList(request.CollectionCenterIds);

                long? memberGroupId = null;
                if (!string.IsNullOrEmpty(request.MemberGroupId) &&
                    request.MemberGroupId != "-1" &&
                    long.TryParse(request.MemberGroupId, out var mg))
                {
                    memberGroupId = mg;
                }

                long? collectorId = null;
                if (!string.IsNullOrEmpty(request.CollectorId) &&
                    request.CollectorId != "-1" &&
                    long.TryParse(request.CollectorId, out var col))
                {
                    collectorId = col;
                }

                var connectionString = _context.Database.GetConnectionString();
                using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();

                var fromDateAd = await _dateConverter.NepaliToEnglishAsync(request.FromDateBs);
                var toDateAd = await _dateConverter.NepaliToEnglishAsync(request.ToDateBs);

                var fromDateStr = $"'{fromDateAd:yyyy-MM-dd}'";
                var toDateStr = $"'{toDateAd:yyyy-MM-dd}'";

                var sqlFilterExp = string.Empty;
                var sqlFilterExpCount = string.Empty;
                var sqlFilterExpOpening = string.Empty;
                var sqlFilterExpOrderBy = BuildSqlOrderBy(request.OrderBy);

                sqlFilterExp += $" And t.TransactionOn between {fromDateStr} And {toDateStr} ";
                sqlFilterExpCount += $" And a.AccountOpenOn <= {toDateStr} ";
                sqlFilterExpOpening += $" And t.TransactionOn < {fromDateStr} ";

                if (branchIds != "-1")
                {
                    var branchFragment = $" And a.UsmOfficeId in({branchIds})";
                    sqlFilterExp += branchFragment;
                    sqlFilterExpCount += branchFragment;
                    sqlFilterExpOpening += branchFragment;
                }

                if (memberGroupId.HasValue)
                    sqlFilterExp += $" And m.SycMemberGroupId = {memberGroupId.Value}";

                if (collectorId.HasValue)
                    sqlFilterExp += $" And a.HurCollectorId = {collectorId.Value}";

                if (collectionCenterIds != "-1")
                    sqlFilterExp += $" And CC.SycCollectionCenterId in({collectionCenterIds})";

                var parameters = new DynamicParameters();
                parameters.Add("@SqlFilterExp", sqlFilterExp, DbType.String, size: -1);
                parameters.Add("@SqlFilterExpCount", sqlFilterExpCount, DbType.String, size: -1);
                parameters.Add("@SqlFilterExpOrderBy", sqlFilterExpOrderBy, DbType.String, size: -1);
                parameters.Add("@SqlFilterExpOpening", sqlFilterExpOpening, DbType.String, size: -1);

                var spName = request.ShowOpeningBalance
                    ? "sp_5_43_GetSavingTypeWiseBalance"
                    : "sp_5_43_GetSavingTypeWiseBalanceNoOpening";

                var rows = (await connection.QueryAsync<SavingTypeWiseBalanceRowDto>(
                    spName,
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

                string? collectionCenterName = null;
                if (collectionCenterIds != "-1")
                {
                    var ccNames = await connection.QueryAsync<string>(
                        $"SELECT CollectionCenterName FROM SycCollectionCenter WHERE SycCollectionCenterId IN ({collectionCenterIds})");
                    var list = ccNames.Where(n => !string.IsNullOrWhiteSpace(n)).ToList();
                    collectionCenterName = list.Count > 0 ? string.Join(", ", list) : null;
                }

                string? memberGroupName = null;
                if (memberGroupId.HasValue)
                {
                    memberGroupName = await connection.QueryFirstOrDefaultAsync<string>(
                        "SELECT Name FROM SycMemberGroup WHERE SycMemberGroupId = @Id",
                        new { Id = memberGroupId.Value });
                }

                string? collectorName = null;
                if (collectorId.HasValue)
                {
                    collectorName = await connection.QueryFirstOrDefaultAsync<string>(
                        "SELECT CollectorFullName FROM HurCollector WHERE HurCollectorId = @Id",
                        new { Id = collectorId.Value });
                }

                return new SavingTypeWiseBalanceData
                {
                    Rows = rows,
                    TotalRecords = rows.Count,
                    TotalAccountCount = rows.Sum(r => r.Count ?? 0),
                    TotalOpening = rows.Sum(r => r.Opening ?? 0),
                    TotalDeposit = rows.Sum(r => r.Deposit ?? 0),
                    TotalWithdraw = rows.Sum(r => r.Withdraw ?? 0),
                    TotalBalance = rows.Sum(r => r.Balance ?? 0),
                    TotalClosing = rows.Sum(r => r.Closing ?? 0),
                    FromDateBs = request.FromDateBs,
                    ToDateBs = request.ToDateBs,
                    FromDateAd = fromDateAd.ToString("yyyy-MM-dd"),
                    ToDateAd = toDateAd.ToString("yyyy-MM-dd"),
                    BranchName = branchName,
                    CollectionCenterName = collectionCenterName,
                    MemberGroupName = memberGroupName,
                    CollectorName = collectorName,
                    OrderBy = request.OrderBy,
                    SameCompanyName = request.SameCompanyName,
                    ShowOpeningBalance = request.ShowOpeningBalance,
                    ShowPercentage = request.ShowPercentage,
                    ShowDetail = request.ShowDetail,
                    GroupByBranch = request.GroupByBranch,
                    GroupByCollectionCenter = request.GroupByCollectionCenter,
                    GroupByMemberGroup = request.GroupByMemberGroup,
                    ViewCollector = request.ViewCollector
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetReportDataAsync (SavingTypeWiseBalance)");
                throw;
            }
        }
    }
}