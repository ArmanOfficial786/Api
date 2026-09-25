// Repository/Microfinance/MicrofinanceReport/RatioAnalysisRepository.cs
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
    public class RatioAnalysisRepository : IRatioAnalysisRepository
    {
        private readonly AppDbContext _context;
        private readonly IDateConverterService _dateConverter;
        private readonly ILogger<RatioAnalysisRepository> _logger;

        public RatioAnalysisRepository(
            AppDbContext context,
            IDateConverterService dateConverter,
            ILogger<RatioAnalysisRepository> logger)
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

        private static string GetProvisionTypeName(string provisionType) =>
            provisionType?.Trim().ToUpper() switch
            {
                "R" => "Remaining Principal",
                "A" => "After Maturity",
                "S" => "Schedule Wise",
                _ => "Schedule Wise"
            };

        private static string GetViewModeName(string viewMode) =>
            viewMode == "T" ? "Total Only" : "Detail";

        public async Task<RatioAnalysisData> GetReportDataAsync(RatioAnalysisRequestDto request)
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

                var provisionType = request.ProvisionType?.Trim().ToUpper();
                if (provisionType != "S" && provisionType != "R" && provisionType != "A")
                    provisionType = "S";

                var viewMode = request.ViewMode?.Trim().ToUpper();
                if (viewMode != "D" && viewMode != "T")
                    viewMode = "D";

                var connectionString = _context.Database.GetConnectionString();
                using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();

                var fromDateAd = await _dateConverter.NepaliToEnglishAsync(request.FromDateBs);
                var toDateAd = await _dateConverter.NepaliToEnglishAsync(request.ToDateBs);

                var parameters = new DynamicParameters();
                parameters.Add("@FromDate", fromDateAd.Date, DbType.Date);
                parameters.Add("@ToDate", toDateAd.Date, DbType.Date);
                parameters.Add("@BranchIds", branchIds, DbType.String, size: 500);
                parameters.Add("@IsLoanMaturity1to30", request.Enable1To30Days, DbType.Boolean);
                parameters.Add("@ProvisionType", provisionType, DbType.String, size: 100);

                using var multi = await connection.QueryMultipleAsync(
                    "sp_6_113_RatioAnalysisReport",
                    parameters,
                    commandType: CommandType.StoredProcedure,
                    commandTimeout: 600);

                var rawRows = (await multi.ReadAsync<RatioAnalysisRowDto>()).AsList();
                var branches = (await multi.ReadAsync<RatioAnalysisBranchDto>()).AsList();

                var groups = rawRows
                    .GroupBy(r => new { r.GroupOrder, r.GroupName })
                    .OrderBy(g => g.Key.GroupOrder)
                    .Select(g => new RatioAnalysisGroupDto
                    {
                        GroupOrder = g.Key.GroupOrder,
                        GroupName = g.Key.GroupName,
                        Rows = g.OrderBy(r => r.SN).ToList()
                    })
                    .ToList();

                string branchName = "All";
                if (branches.Any())
                {
                    var list = branches
                        .Select(b => b.OfficeName)
                        .Where(n => !string.IsNullOrWhiteSpace(n))
                        .ToList();
                    branchName = list.Count > 0 ? string.Join(", ", list) : "All";
                }

                return new RatioAnalysisData
                {
                    Groups = groups,
                    Branches = branches,
                    TotalRecords = rawRows.Count,
                    FromDateBs = request.FromDateBs,
                    ToDateBs = request.ToDateBs,
                    FromDateAd = fromDateAd.ToString("yyyy-MM-dd"),
                    ToDateAd = toDateAd.ToString("yyyy-MM-dd"),
                    BranchName = branchName,
                    ProvisionType = provisionType,
                    ProvisionTypeName = GetProvisionTypeName(provisionType),
                    ViewMode = viewMode,
                    ViewModeName = GetViewModeName(viewMode),
                    Enable1To30Days = request.Enable1To30Days
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetReportDataAsync (RatioAnalysis)");
                throw;
            }
        }
    }
}