// Repository/Loan/LoanAnalysisReport/LoanRegisterRepository.cs
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
    public class LoanRegisterRepository : ILoanRegisterRepository
    {
        private readonly AppDbContext _context;
        private readonly IDateConverterService _dateConverter;
        private readonly ILogger<LoanRegisterRepository> _logger;

        public LoanRegisterRepository(
            AppDbContext context,
            IDateConverterService dateConverter,
            ILogger<LoanRegisterRepository> logger)
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
                "MemberId" => " order by substring(MemberId, 1,(len(MemberId)-charindex('-', MemberId))-1), MemberId ",
                "FullName" => " order by FullName ",
                "LoanAccountNo" => " order by substring(LoanAccountNo, 1,(len(LoanAccountNo)-charindex('-', LoanAccountNo))-1), LoanAccountNo ",
                "LoanTypeName" => " order by LoanTypeName",
                "LoanIssueAmount" => " order by LoanIssueAmount DESC",
                "LoanIssueOnBs" => " order by LoanIssueOnBs",
                "InterestRate" => " order by InterestRate",
                "LoanPaymentTypeCode" => " order by LoanPaymentTypeCode",
                "PriBalance" => " order by PriBalance DESC",
                "IntBalance" => " order by IntBalance DESC",
                "MaturityOnBs" => " order by MaturityOnBs",
                "PenaltyBalance" => " order by PenaltyBalance",
                "TotalBalance" => " order by TotalBalance",
                _ => string.Empty
            };
        }

        private static string GetSelectLoanName(string selectLoan)
        {
            return selectLoan?.Trim().ToUpper() switch
            {
                "N" => "Normal",
                "O" => "Over Draft",
                _ => "All"
            };
        }

        public async Task<LoanRegisterData> GetReportDataAsync(LoanRegisterRequestDto request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.TillDateBs) || request.TillDateBs == "-1")
                {
                    throw new ArgumentException("Till Date is required.");
                }

                var branchIds = SanitizeIdList(request.BranchIds);
                var collectionCenterIds = SanitizeIdList(request.CollectionCenterIds);

                var connectionString = _context.Database.GetConnectionString();
                using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();

                var tillDateAd = await _dateConverter.NepaliToEnglishAsync(request.TillDateBs);
                var tillDateStr = $"'{tillDateAd:yyyy-MM-dd}'";

                var sqlFilterExp = string.Empty;
                var sqlFilterExpBranchId = string.Empty;

                if (branchIds != "-1")
                    sqlFilterExpBranchId += $" And LS.UsmOfficeId in({branchIds})";

                if (request.EnableCollectionCenter && collectionCenterIds != "-1")
                    sqlFilterExpBranchId += $" And LS.SycCollectionCenterId in({collectionCenterIds})";

                if (!string.IsNullOrEmpty(request.LoanTypeMasterId) &&
                    request.LoanTypeMasterId != "-1" &&
                    request.LoanTypeMasterId != "string" &&
                    long.TryParse(request.LoanTypeMasterId, out var loanTypeId))
                {
                    sqlFilterExp += $" AND LS.LmtLoanTypeMasterId = {loanTypeId}";
                }

                if (!string.IsNullOrEmpty(request.CollectorId) &&
                    request.CollectorId != "-1" &&
                    request.CollectorId != "string" &&
                    long.TryParse(request.CollectorId, out var collectorId))
                {
                    sqlFilterExp += $" and LS.HurCollectorId = {collectorId}";
                }

                if (!string.IsNullOrEmpty(request.SelectLoan) && request.SelectLoan != "-1")
                {
                    // whitelist values to avoid injection
                    var sl = request.SelectLoan.Trim().ToUpper();
                    if (sl == "N" || sl == "O")
                        sqlFilterExp += $" and LS.LoanODorNormal = '{sl}'";
                }

                var sqlFilterExpOrderBy = BuildSqlOrderBy(request.OrderBy);

                var parameters = new DynamicParameters();
                parameters.Add("@SqlFiltertilldate", tillDateStr, DbType.String, size: -1);
                parameters.Add("@SqlFilterExp", sqlFilterExp, DbType.String, size: -1);
                parameters.Add("@SqlFilterExpbranchId", sqlFilterExpBranchId, DbType.String, size: -1);
                parameters.Add("@SqlFilterExpOrderBy", sqlFilterExpOrderBy, DbType.String, size: -1);

                var rows = (await connection.QueryAsync<LoanRegisterRowDto>(
                    "sp_7_16_LoanRegisterReport",
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
                if (!string.IsNullOrEmpty(request.LoanTypeMasterId) &&
                    request.LoanTypeMasterId != "-1" &&
                    long.TryParse(request.LoanTypeMasterId, out var loanTypeIdForName))
                {
                    loanTypeName = await connection.QueryFirstOrDefaultAsync<string>(
                        "SELECT LoanTypeName FROM LmtLoanTypeMaster WHERE LmtLoanTypeMasterId = @Id",
                        new { Id = loanTypeIdForName });
                }

                // Resolve Collector name
                string? collectorName = null;
                if (!string.IsNullOrEmpty(request.CollectorId) &&
                    request.CollectorId != "-1" &&
                    long.TryParse(request.CollectorId, out var collectorIdForName))
                {
                    collectorName = await connection.QueryFirstOrDefaultAsync<string>(
                        "SELECT CollectorFullName FROM HurCollector WHERE HurCollectorId = @Id",
                        new { Id = collectorIdForName });
                }

                // Resolve Collection Center name
                string? collectionCenterName = null;
                if (request.EnableCollectionCenter && collectionCenterIds != "-1")
                {
                    var ccNames = await connection.QueryAsync<string>(
                        $"SELECT CollectionCenterName FROM SycCollectionCenter WHERE SycCollectionCenterId IN ({collectionCenterIds})");
                    var ccList = ccNames.Where(n => !string.IsNullOrWhiteSpace(n)).ToList();
                    collectionCenterName = ccList.Count > 0 ? string.Join(", ", ccList) : null;
                }

                return new LoanRegisterData
                {
                    Rows = rows,
                    TotalRecords = rows.Count,
                    TotalLoanIssueAmount = rows.Sum(r => r.LoanIssueAmount ?? 0),
                    TotalPriBalance = rows.Sum(r => r.PriBalance ?? 0),
                    TotalIntBalance = rows.Sum(r => r.IntBalance ?? 0),
                    TotalPenaltyBalance = rows.Sum(r => r.PenaltyBalance ?? 0),
                    TotalBalance = rows.Sum(r => r.TotalBalance ?? 0),
                    TotalPriPaid = rows.Sum(r => r.PriPaid ?? 0),
                    TillDateBs = request.TillDateBs,
                    TillDateAd = tillDateAd.ToString("yyyy-MM-dd"),
                    BranchName = branchName,
                    LoanTypeName = loanTypeName,
                    CollectorName = collectorName,
                    CollectionCenterName = collectionCenterName,
                    SelectLoan = request.SelectLoan,
                    SelectLoanName = GetSelectLoanName(request.SelectLoan),
                    OrderBy = request.OrderBy,
                    EnableCollectionCenter = request.EnableCollectionCenter,
                    NepaliReport = request.NepaliReport
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetReportDataAsync (LoanRegister)");
                throw;
            }
        }
    }
}