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
    public class FamPurchaseReportRepository : IFamPurchaseReportRepository
    {
        private readonly AppDbContext _context;
        private readonly IDateConverterService _dateConverter;
        private readonly ILogger<FamPurchaseReportRepository> _logger;

        public FamPurchaseReportRepository(
            AppDbContext context,
            IDateConverterService dateConverter,
            ILogger<FamPurchaseReportRepository> logger)
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

        private static string BuildOrderBy(FamPurchaseReportRequestDto request)
        {
            if (string.IsNullOrEmpty(request.OrderBy) || request.OrderBy == "-1")
                return string.Empty;

            var isAsset = string.Equals(request.ReportType, "ASSET", StringComparison.OrdinalIgnoreCase);

            return request.OrderBy.Trim() switch
            {
                "Code" => isAsset ? "Code" : string.Empty,
                "FixedAssetsName" => "FixedAssetsName",
                "SerialNumber" => isAsset ? "SerialNumber" : string.Empty,
                "ModelName" => isAsset ? "ModelName" : string.Empty,
                "Amount" => "Amount",
                "PurchaseType" => isAsset ? "PurchaseType" : string.Empty,
                "PurchaseDateOn" => isAsset ? "PurchaseDateOn" : string.Empty,
                "TransactionOn" => "TransactionOn",
                "LastDepreciationDateOn" => isAsset ? "LastDepreciationDateOn" : string.Empty,
                "LastDepreciatedValue" => isAsset ? "LastDepreciatedValue" : string.Empty,
                "Quantity" => !isAsset ? "Quantity" : string.Empty,
                "Rate" => !isAsset ? "Rate" : string.Empty,
                _ => string.Empty
            };
        }

        public async Task<FamPurchaseReportData> GetReportDataAsync(FamPurchaseReportRequestDto request)
        {
            try
            {
                var connectionString = _context.Database.GetConnectionString();
                using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();

                var fromDateAd = (await _dateConverter.NepaliToEnglishAsync(request.FromDateBs)).ToString("yyyy-MM-dd");
                var toDateAd = (await _dateConverter.NepaliToEnglishAsync(request.ToDateBs)).ToString("yyyy-MM-dd");

                var branchIds = SanitizeBranchIds(request.BranchIds);

                string? categoryName = null;
                if (request.CategoryId != -1)
                {
                    categoryName = await connection.QueryFirstOrDefaultAsync<string>(
                        "SELECT CategoryName FROM FamAssetsCategory WHERE FamAssetsCategoryId = @Id",
                        new { Id = request.CategoryId });
                }

                var reportType = string.IsNullOrWhiteSpace(request.ReportType)
                    ? "ASSET"
                    : request.ReportType.ToUpper();

                var spName = reportType == "STOCK"
                    ? "sp_15_124_GetStockPurchaseReport"
                    : "sp_15_124_GetAssetsPurchaseReport";

                var parameters = new DynamicParameters();
                parameters.Add("@fromDate", fromDateAd, DbType.String, size: 1000);
                parameters.Add("@toDate", toDateAd, DbType.String, size: 1000);
                parameters.Add("@branchId", branchIds, DbType.String, size: -1);
                parameters.Add("@Orderby", BuildOrderBy(request), DbType.String, size: -1);
                parameters.Add("@categoryId", request.CategoryId, DbType.Int32);

                var rows = await connection.QueryAsync<FamPurchaseReportRowDto>(
                    spName,
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

                return new FamPurchaseReportData
                {
                    Rows = resultList,
                    TotalRecords = resultList.Count,
                    TotalAmount = resultList.Sum(r => r.Amount ?? 0),
                    TotalLastDepreciatedValue = resultList.Sum(r => r.LastDepreciatedValue ?? 0),
                    FromDateBs = request.FromDateBs,
                    ToDateBs = request.ToDateBs,
                    BranchName = branchName,
                    CategoryName = categoryName ?? "All",
                    ReportType = reportType,
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