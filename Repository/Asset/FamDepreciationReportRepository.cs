using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using NexgenCosysReport.DbContext;
using NexgenCosysReport.Dtos.RequestDtos.Asset;
using NexgenCosysReport.Inteface.ServiceInterface.Asset;
using NexgenCosysReport.Inteface.ServiceInterface.Common;
using System.Data;

namespace NexgenCosysReport.Repository.Asset
{
    public class FamDepreciationReportRepository : IFamDepreciationReportRepository
    {
        private readonly AppDbContext _context;
        private readonly IDateConverterService _dateConverter;
        private readonly ILogger<FamDepreciationReportRepository> _logger;

        public FamDepreciationReportRepository(
            AppDbContext context,
            IDateConverterService dateConverter,
            ILogger<FamDepreciationReportRepository> logger)
        {
            _context = context;
            _dateConverter = dateConverter;
            _logger = logger;
        }

        private static string SanitizeBranchIds(string? branchIds)
        {
            if (string.IsNullOrWhiteSpace(branchIds) || branchIds == "-1" || branchIds == "string")
                return "-1";

            var validIds = branchIds
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Where(id => long.TryParse(id, out _));

            var joined = string.Join(",", validIds);
            return string.IsNullOrEmpty(joined) ? "-1" : joined;
        }

        private static string BuildOrderBy(FamDepreciationReportRequestDto request)
        {
            if (string.IsNullOrEmpty(request.OrderBy) || request.OrderBy == "-1")
                return string.Empty;

            return request.OrderBy.Trim() switch
            {
                "Code" => "Code",
                "ad.FixedAssetsName" => "ad.FixedAssetsName",
                "pd.PurchaseDateOn" => "pd.PurchaseDateOn",
                "pd.Amount" => "pd.Amount",
                "dm.MethodName" => "dm.MethodName",
                "pod.Percentage" => "pod.Percentage",
                "dt.InitialAmount" => "dt.InitialAmount",
                "dt.Amount" => "dt.Amount",
                "NewAssetValue" => "NewAssetValue",
                "uo.OfficeName" => "uo.OfficeName",
                _ => string.Empty
            };
        }

        public async Task<FamDepreciationReportData> GetReportDataAsync(FamDepreciationReportRequestDto request)
        {
            try
            {
                var connectionString = _context.Database.GetConnectionString();
                using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();

                var fromDateAd = (await _dateConverter.NepaliToEnglishAsync(request.FromDateBs)).ToString("yyyy-MM-dd");
                var toDateAd = (await _dateConverter.NepaliToEnglishAsync(request.ToDateBs)).ToString("yyyy-MM-dd");

                var branchIds = SanitizeBranchIds(request.BranchIds);

                var parameters = new DynamicParameters();
                parameters.Add("@fromDate", fromDateAd, DbType.String, size: 1000);
                parameters.Add("@toDate", toDateAd, DbType.String, size: 1000);
                parameters.Add("@branchId", branchIds, DbType.String, size: -1);
                parameters.Add("@Orderby", BuildOrderBy(request), DbType.String, size: -1);

                var rows = await connection.QueryAsync<FamDepreciationReportRowDto>(
                    "sp_15_124_GetAllDepreciation",
                    parameters,
                    commandType: CommandType.StoredProcedure,
                    commandTimeout: 120
                );

                var resultList = rows.AsList();

                string branchName = "All";
                if (branchIds != "-1")
                {
                    var names = await connection.QueryAsync<string>(
                        $"SELECT OfficeName FROM UsmOffice WHERE UsmOfficeId IN ({branchIds})");
                    var nameList = names.Where(n => !string.IsNullOrWhiteSpace(n)).ToList();
                    branchName = nameList.Count > 0 ? string.Join(", ", nameList) : "All";
                }

                return new FamDepreciationReportData
                {
                    Rows = resultList,
                    TotalRecords = resultList.Count,
                    TotalPurchaseAmt = resultList.Sum(r => r.PurchaseAmt ?? 0),
                    TotalDepreciationAmt = resultList.Sum(r => r.DepreciationAmt ?? 0),
                    TotalNewAssetValue = resultList.Sum(r => r.NewAssetValue ?? 0),
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