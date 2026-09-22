// Repository/Loan/LoanAnalysisReport/LoanCICRepository.cs
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using NexgenCosysReport.DbContext;
using NexgenCosysReport.Dtos.RequestDtos.Loan.LoanAnalysisReport;
using NexgenCosysReport.Inteface.ServiceInterface.Common;
using NexgenCosysReport.Inteface.ServiceInterface.Loan.LoanAnalysisReport;
using System.Data;

namespace NexgenCosysReport.Repository.Loan.LoanAnalysisReport
{
    public class LoanCICRepository : ILoanCICRepository
    {
        private readonly AppDbContext _context;
        private readonly IDateConverterService _dateConverter;
        private readonly ILogger<LoanCICRepository> _logger;

        public LoanCICRepository(
            AppDbContext context,
            IDateConverterService dateConverter,
            ILogger<LoanCICRepository> logger)
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
            if (string.IsNullOrEmpty(orderBy) || orderBy == "-1")
                return string.Empty;

            return orderBy.Trim() switch
            {
                "MemberId" => "MemberId",
                "FullName" => "FullName",
                "PermanentAddessDetail" => "PermanentAddessDetail",
                "TemporaryAddressDetail" => "TemporaryAddressDetail",
                "CitizenshipNo" => "CitizenshipNo",
                "CitizenShipIssuedOnBs" => "CitizenShipIssuedOnBs",
                "CitizenShipIssuedDistrict" => "CitizenShipIssuedDistrict",
                "LoanTypeName" => "LoanTypeName",
                "LoanAccountNo" => "LoanAccountNo",
                "LoanIssueOnBs" => "LoanIssueOnBs",
                "MaturityOnBs" => "MaturityOnBs",
                "LoanIssueAmount" => "LoanIssueAmount",
                "PriBalance" => "PriBalance",
                "IntBalance" => "IntBalance",
                "GoodLoan" => "GoodLoan",
                _ => string.Empty
            };
        }

        private static string GetProvisionTypeName(string provisionType)
        {
            return provisionType?.Trim().ToUpper() switch
            {
                "R" => "Remaining Principal",
                "A" => "After Maturity",
                "S" => "Schedule Wise",
                _ => "Schedule Wise"
            };
        }

        public async Task<LoanCICData> GetReportDataAsync(LoanCICRequestDto request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.TillDateBs) || request.TillDateBs == "-1")
                {
                    throw new ArgumentException("Till Date is required.");
                }

                var branchIds = SanitizeIdList(request.BranchIds);
                if (branchIds == "-1")
                {
                    throw new ArgumentException("Please select Branch Name.");
                }

                // Validate loan type id
                int loanTypeId = -1;
                if (!string.IsNullOrEmpty(request.LoanTypeId) &&
                    request.LoanTypeId != "-1" &&
                    int.TryParse(request.LoanTypeId, out var ltId))
                {
                    loanTypeId = ltId;
                }

                // Validate provision type against whitelist
                var provisionType = request.ProvisionType?.Trim().ToUpper();
                if (provisionType != "S" && provisionType != "R" && provisionType != "A")
                    provisionType = "S";

                var connectionString = _context.Database.GetConnectionString();
                using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();

                var tillDateAd = await _dateConverter.NepaliToEnglishAsync(request.TillDateBs);
                // SP takes a `date` parameter - Dapper will map DateTime correctly
                var tillDate = tillDateAd.Date;

                var orderByFilter = BuildSqlOrderBy(request.OrderBy);
                if (string.IsNullOrEmpty(orderByFilter))
                    orderByFilter = "-1";

                var parameters = new DynamicParameters();
                parameters.Add("@tillDate", tillDate, DbType.Date);
                parameters.Add("@branchIds", branchIds, DbType.String, size: 500);
                parameters.Add("@loanTypeId", loanTypeId, DbType.Int32);
                parameters.Add("@ProvisionType", provisionType, DbType.String, size: 100);
                parameters.Add("@orderBy", orderByFilter, DbType.String, size: 400);

                var rows = (await connection.QueryAsync<LoanCICRowDto>(
                    "sp_7_16_LoanCICReport",
                    parameters,
                    commandType: CommandType.StoredProcedure,
                    commandTimeout: 600
                )).AsList();

                // Resolve Branch name
                string branchName = "All";
                if (branchIds != "-1")
                {
                    var names = await connection.QueryAsync<string>(
                        $"SELECT OfficeName FROM UsmOffice WHERE UsmOfficeId IN ({branchIds})");
                    var nameList = names.Where(n => !string.IsNullOrWhiteSpace(n)).ToList();
                    branchName = nameList.Count > 0 ? string.Join(", ", nameList) : "All";
                }

                // Resolve Loan Type name
                string? loanTypeName = null;
                if (loanTypeId != -1)
                {
                    loanTypeName = await connection.QueryFirstOrDefaultAsync<string>(
                        "SELECT LoanTypeName FROM LmtLoanTypeMaster WHERE LmtLoanTypeMasterId = @Id",
                        new { Id = loanTypeId });
                }

                return new LoanCICData
                {
                    Rows = rows,
                    TotalRecords = rows.Count,
                    TotalLoanIssueAmount = rows.Sum(r => r.LoanIssueAmount ?? 0),
                    TotalPriPaid = rows.Sum(r => r.PriPaid ?? 0),
                    TotalPriBalance = rows.Sum(r => r.PriBalance ?? 0),
                    TotalIntBalance = rows.Sum(r => r.IntBalance ?? 0),
                    TotalPenaltyBalance = rows.Sum(r => r.PenaltyBalance ?? 0),
                    TotalBalance = rows.Sum(r => r.TotalBalance ?? 0),
                    TotalGoodLoan = rows.Sum(r => r.GoodLoan ?? 0),
                    TotalLoanRisk30 = rows.Sum(r => r.LoanRisk30 ?? 0),
                    TotalLoanRisk31to365 = rows.Sum(r => r.LoanRisk31to365 ?? 0),
                    TotalLoanRisk1to365 = rows.Sum(r => r.LoanRisk1to365 ?? 0),
                    TotalLoanRiskGreaterThan365 = rows.Sum(r => r.LoanRiskGreaterThan365 ?? 0),
                    TillDateBs = request.TillDateBs,
                    TillDateAd = tillDate.ToString("yyyy-MM-dd"),
                    BranchName = branchName,
                    LoanTypeName = loanTypeName,
                    ProvisionType = provisionType,
                    ProvisionTypeName = GetProvisionTypeName(provisionType),
                    OrderBy = request.OrderBy,
                    Enable1To30Days = request.Enable1To30Days
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetReportDataAsync (LoanCIC)");
                throw;
            }
        }
    }
}