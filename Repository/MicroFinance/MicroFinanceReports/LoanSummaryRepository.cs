// Repository/Microfinance/MicrofinanceReport/LoanSummaryRepository.cs
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
    public class LoanSummaryRepository : ILoanSummaryRepository
    {
        private readonly AppDbContext _context;
        private readonly IDateConverterService _dateConverter;
        private readonly ILogger<LoanSummaryRepository> _logger;

        public LoanSummaryRepository(
            AppDbContext context,
            IDateConverterService dateConverter,
            ILogger<LoanSummaryRepository> logger)
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
            "MemberId",
            "FullName",
            "LoanAccountNo",
            "LoanTypeName",
            "DisburseAmount",
            "Repaid",
            "BalanceAmount",
            "CollectionCenterName",
            "GroupName",
            "OfficeName"
        };

        private static string BuildOrderBy(string? orderBy)
        {
            if (string.IsNullOrWhiteSpace(orderBy) || orderBy == "-1")
                return string.Empty;

            var trimmed = orderBy.Trim();
            return AllowedOrderBy.Contains(trimmed) ? trimmed : string.Empty;
        }

        public async Task<LoanSummaryData> GetReportDataAsync(LoanSummaryRequestDto request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.TillDateBs) || request.TillDateBs == "-1")
                    throw new ArgumentException("Till Date is required.");

                var branchIds = SanitizeIdList(request.BranchIds);
                if (branchIds == "-1")
                    throw new ArgumentException("Please select Branch Name.");

                var connectionString = _context.Database.GetConnectionString();
                using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();

                var tillDateAd = await _dateConverter.NepaliToEnglishAsync(request.TillDateBs);
                var orderBy = BuildOrderBy(request.OrderBy);

                var parameters = new DynamicParameters();
                parameters.Add("@tillDate", tillDateAd.Date, DbType.Date);
                parameters.Add("@branchIds", branchIds, DbType.String, size: -1);
                parameters.Add("@orderBy", orderBy, DbType.String, size: -1);

                var rows = (await connection.QueryAsync<LoanSummaryRowDto>(
                    "sp_7_16_LoanSummaryReportForMicrofinance",
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

                return new LoanSummaryData
                {
                    Rows = rows,
                    TotalRecords = rows.Count,
                    TotalDisburseAmount = rows.Sum(r => r.DisburseAmount ?? 0),
                    TotalRepaid = rows.Sum(r => r.Repaid ?? 0),
                    TotalBalanceAmount = rows.Sum(r => r.BalanceAmount ?? 0),
                    TillDateBs = request.TillDateBs,
                    TillDateAd = tillDateAd.ToString("yyyy-MM-dd"),
                    BranchName = branchName,
                    OrderBy = orderBy
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetReportDataAsync (LoanSummary)");
                throw;
            }
        }
    }
}