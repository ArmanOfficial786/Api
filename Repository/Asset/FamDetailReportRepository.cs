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
    public class FamDetailReportRepository : IFamDetailReportRepository
    {
        private readonly AppDbContext _context;
        private readonly IDateConverterService _dateConverter;
        private readonly ILogger<FamDetailReportRepository> _logger;

        public FamDetailReportRepository(
            AppDbContext context,
            IDateConverterService dateConverter,
            ILogger<FamDetailReportRepository> logger)
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

        private static string BuildOrderBy(FamDetailReportRequestDto request)
        {
            if (string.IsNullOrEmpty(request.OrderBy) || request.OrderBy == "-1")
                return string.Empty;

            return request.OrderBy.Trim() switch
            {
                "Code" => "Code",
                "FixedAssetsName" => "FixedAssetsName",
                "TransactionType" => "TransactionType",
                "Amount" => "Amount",
                "TransactionDateBS" => "TransactionDateBS",
                "PurchaseStatus" => "PurchaseStatus",
                "VerifiedTo" => "VerifiedTo",
                "TransferredOut" => "TransferredOut",
                "TransferredIn" => "TransferredIn",
                "Clearance" => "Clearance",
                "Qty" => "Qty",
                "Rate" => "Rate",
                "PurchaseOn" => "PurchaseOn",
                "MemberOut" => "MemberOut",
                "BranchOut" => "BranchOut",
                "BranchIn" => "BranchIn",
                "StockOutOn" => "StockOutOn",
                "PurchaseDate" => "PurchaseDate",
                "PurchaseAmount" => "PurchaseAmount",
                "LastDepreciatedDate" => "LastDepreciatedDate",
                "LastDepreciatedAmount" => "LastDepreciatedAmount",
                "AssetName" => "AssetName",
                "Balance" => "Balance",
                _ => string.Empty
            };
        }

        public async Task<FamDetailReportData> GetReportDataAsync(FamDetailReportRequestDto request)
        {
            try
            {
                var connectionString = _context.Database.GetConnectionString();
                using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();

                var toDateAd = (await _dateConverter.NepaliToEnglishAsync(request.ToDateBs)).ToString("yyyy-MM-dd");
                var branchIds = SanitizeBranchIds(request.BranchIds);
                var orderBy = BuildOrderBy(request);

                var isAsset = string.Equals(request.ReportType, "ASSET", StringComparison.OrdinalIgnoreCase);

                string? categoryName = null;
                if (request.CategoryId != -1)
                {
                    categoryName = await connection.QueryFirstOrDefaultAsync<string>(
                        "SELECT CategoryName FROM FamAssetsCategory WHERE FamAssetsCategoryId = @Id",
                        new { Id = request.CategoryId });
                }

                string? assetName = null;
                if (request.AssetId != -1)
                {
                    assetName = await connection.QueryFirstOrDefaultAsync<string>(
                        "SELECT FixedAssetsName FROM FamFixedAssetsDetail WHERE FamFixedAssetsDetailId = @Id",
                        new { Id = request.AssetId });
                }

                var rows = new List<FamDetailReportRowDto>();
                var summary = new List<FamDetailReportSummaryDto>();

                if (isAsset && !request.IsTypeWise)
                {
                    var parameters = new DynamicParameters();
                    parameters.Add("@toDate", toDateAd, DbType.Date);
                    parameters.Add("@branchId", branchIds, DbType.String, size: -1);
                    parameters.Add("@Orderby", orderBy, DbType.String, size: -1);
                    parameters.Add("@assetId", request.AssetId, DbType.Int32);
                    parameters.Add("@clearanceStatusId", request.ClearanceStatusId, DbType.Int32);
                    parameters.Add("@isSummary", request.IsSummary, DbType.Boolean);
                    parameters.Add("@categoryId", request.CategoryId, DbType.Int32);

                    using var multi = await connection.QueryMultipleAsync(
                        "sp_15_124_GetAssetsDetailReport",
                        parameters,
                        commandType: CommandType.StoredProcedure,
                        commandTimeout: 180
                    );

                    rows = (await multi.ReadAsync<FamDetailReportRowDto>()).AsList();
                    summary = (await multi.ReadAsync<FamDetailReportSummaryDto>()).AsList();
                }
                else if (!isAsset && !request.IsTypeWise)
                {
                    var parameters = new DynamicParameters();
                    parameters.Add("@toDate", toDateAd, DbType.String, size: 50);
                    parameters.Add("@branchId", branchIds, DbType.String, size: -1);
                    parameters.Add("@Orderby", orderBy, DbType.String, size: -1);
                    parameters.Add("@assetId", request.AssetId, DbType.Int32);
                    parameters.Add("@categoryId", request.CategoryId, DbType.Int32);

                    using var multi = await connection.QueryMultipleAsync(
                        "sp_15_124_GetStockDetailReport",
                        parameters,
                        commandType: CommandType.StoredProcedure,
                        commandTimeout: 180
                    );

                    rows = (await multi.ReadAsync<FamDetailReportRowDto>()).AsList();
                    summary = (await multi.ReadAsync<FamDetailReportSummaryDto>()).AsList();
                }
                else if (isAsset && request.IsTypeWise)
                {
                    var parameters = new DynamicParameters();
                    parameters.Add("@toDate", toDateAd, DbType.Date);
                    parameters.Add("@branchId", branchIds, DbType.String, size: -1);
                    parameters.Add("@Orderby", orderBy, DbType.String, size: -1);
                    parameters.Add("@assetId", request.AssetId, DbType.Int32);
                    parameters.Add("@categoryId", request.CategoryId, DbType.Int32);

                    rows = (await connection.QueryAsync<FamDetailReportRowDto>(
                        "sp_15_124_GetTypeWiseAssetsDetailReport",
                        parameters,
                        commandType: CommandType.StoredProcedure,
                        commandTimeout: 180
                    )).AsList();
                }
                else
                {
                    var parameters = new DynamicParameters();
                    parameters.Add("@toDate", toDateAd, DbType.Date);
                    parameters.Add("@branchId", branchIds, DbType.String, size: -1);
                    parameters.Add("@Orderby", orderBy, DbType.String, size: -1);
                    parameters.Add("@assetId", request.AssetId, DbType.Int32);
                    parameters.Add("@categoryId", request.CategoryId, DbType.Int32);

                    rows = (await connection.QueryAsync<FamDetailReportRowDto>(
                        "sp_15_124_GetTypeWiseStockDetailReport",
                        parameters,
                        commandType: CommandType.StoredProcedure,
                        commandTimeout: 180
                    )).AsList();
                }

                string branchName = "All";
                if (branchIds != "-1")
                {
                    var names = await connection.QueryAsync<string>(
                        $"SELECT OfficeName FROM UsmOffice WHERE UsmOfficeId IN ({branchIds})");
                    var nameList = names.Where(n => !string.IsNullOrWhiteSpace(n)).ToList();
                    branchName = nameList.Count > 0 ? string.Join(", ", nameList) : "All";
                }

                return new FamDetailReportData
                {
                    Rows = rows,
                    Summary = summary,
                    TotalRecords = rows.Count,
                    TotalAmount = rows.Sum(r => r.Amount ?? 0),
                    ToDateBs = request.ToDateBs,
                    BranchName = branchName,
                    CategoryName = categoryName ?? "All",
                    AssetName = assetName ?? "All",
                    ReportType = isAsset ? "ASSET" : "STOCK",
                    IsTypeWise = request.IsTypeWise,
                    IsSummary = request.IsSummary,
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