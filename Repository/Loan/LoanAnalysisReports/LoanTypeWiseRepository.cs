// Repository/Loan/LoanAnalysisReport/LoanTypeWiseRepository.cs
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using NexgenCosysReport.DbContext;
using NexgenCosysReport.Dtos.RequestDtos.Loan.LoanAnalysisReport;
using NexgenCosysReport.Inteface.ServiceInterface.Common;
using NexgenCosysReport.Inteface.ServiceInterface.Loan.LoanAnalysisReport;
using System.Data;
using System.Text;

namespace NexgenCosysReport.Repository.Loan.LoanAnalysisReport
{
    public class LoanTypeWiseRepository : ILoanTypeWiseRepository
    {
        private readonly AppDbContext _context;
        private readonly IDateConverterService _dateConverter;
        private readonly ILogger<LoanTypeWiseRepository> _logger;

        public LoanTypeWiseRepository(
            AppDbContext context,
            IDateConverterService dateConverter,
            ILogger<LoanTypeWiseRepository> logger)
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
                "LoanType" => " order by LoanTypeName",
                "DisburseAmount" => " order by DisburseAmount",
                _ => string.Empty
            };
        }

        public async Task<LoanTypeWiseData> GetReportDataAsync(LoanTypeWiseRequestDto request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.FromDateBs) || string.IsNullOrWhiteSpace(request.ToDateBs)
                    || request.FromDateBs == "-1" || request.ToDateBs == "-1")
                {
                    throw new ArgumentException("From Date and To Date are required.");
                }

                var branchIds = SanitizeBranchIds(request.BranchIds);

                var connectionString = _context.Database.GetConnectionString();
                using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();

                var fromDateAd = await _dateConverter.NepaliToEnglishAsync(request.FromDateBs);
                var toDateAd = await _dateConverter.NepaliToEnglishAsync(request.ToDateBs);

                var fromDateStr = fromDateAd.ToString("yyyy-MM-dd");
                var toDateStr = toDateAd.ToString("yyyy-MM-dd");

                var sqlFilterExp = new StringBuilder();

                if (!string.IsNullOrEmpty(request.LoanTypeId) && request.LoanTypeId != "-1")
                {
                    sqlFilterExp.Append(" And Ltm.LmtLoanTypeMasterId = ").Append(request.LoanTypeId);
                }

                if (branchIds != "-1")
                {
                    sqlFilterExp.Append(" And LS.UsmOfficeId in(").Append(branchIds).Append(")");
                }

                if (!string.IsNullOrEmpty(request.MemberGroupId) && request.MemberGroupId != "-1")
                {
                    sqlFilterExp.Append(" AND MR.SycMemberGroupId = ").Append(request.MemberGroupId);
                }

                if (!string.IsNullOrEmpty(request.CollectorId) && request.CollectorId != "-1")
                {
                    sqlFilterExp.Append(" And LS.HurCollectorId = ").Append(request.CollectorId);
                }

                if (!string.IsNullOrEmpty(request.CollectionCenterId) && request.CollectionCenterId != "-1")
                {
                    sqlFilterExp.Append(" And LS.SycCollectionCenterId in(").Append(request.CollectionCenterId).Append(")");
                }

                if (!string.IsNullOrEmpty(request.LoanGuarantee) && request.LoanGuarantee != "-1")
                {
                    sqlFilterExp.Append(" And LS.LoanGuarantee = '").Append(request.LoanGuarantee).Append("'");
                }

                if (request.ShareType != -1)
                {
                    sqlFilterExp.Append(" And shm.ShmShareTypeId = ").Append(request.ShareType);
                }

                var sqlLoanIssueDate = new StringBuilder();
                if (!string.IsNullOrEmpty(fromDateStr) && !string.IsNullOrEmpty(toDateStr))
                {
                    sqlLoanIssueDate.Append(" And LL.LoanIssueOn >= '").Append(fromDateStr)
                                    .Append("' and LL.LoanIssueOn <= '").Append(toDateStr).Append("'");
                }

                var transactionDate = new StringBuilder();
                if (!string.IsNullOrEmpty(fromDateStr) && !string.IsNullOrEmpty(toDateStr))
                {
                    transactionDate.Append(" And T.TransactionOn >= '").Append(fromDateStr)
                                   .Append("' and T.TransactionOn <= '").Append(toDateStr).Append("'");
                }

                var sqlOrderExp = BuildSqlOrderBy(request.OrderBy);

                var parameters = new DynamicParameters();
                parameters.Add("@SqlFilterExp", sqlFilterExp.ToString(), DbType.String, size: -1);
                parameters.Add("@TransactionDate", transactionDate.ToString(), DbType.String, size: -1);
                parameters.Add("@SqlOrderExp", sqlOrderExp, DbType.String, size: -1);
                parameters.Add("@SqlOrderExpFrom", $"'{fromDateStr}'", DbType.String, size: -1);
                parameters.Add("@SqlOrderExpTill", $"'{toDateStr}'", DbType.String, size: -1);
                parameters.Add("@SqlLoanIssueDate", sqlLoanIssueDate.ToString(), DbType.String, size: -1);

                var spName = request.ShowOpeningBalance
                    ? "sp_7_16_LoanTypeWiseReport"
                    : "sp_7_16_LoanTypeWiseReportNoOpening";

                var rows = (await connection.QueryAsync<LoanTypeWiseRowDto>(
                    spName,
                    parameters,
                    commandType: CommandType.StoredProcedure,
                    commandTimeout: 300
                )).AsList();

                string branchName = "All";
                if (branchIds != "-1")
                {
                    var names = await connection.QueryAsync<string>(
                        $"SELECT OfficeName FROM UsmOffice WHERE UsmOfficeId IN ({branchIds})");
                    var nameList = names.Where(n => !string.IsNullOrWhiteSpace(n)).ToList();
                    branchName = nameList.Count > 0 ? string.Join(", ", nameList) : "All";
                }

                string? loanTypeName = null;
                if (!string.IsNullOrEmpty(request.LoanTypeId) && request.LoanTypeId != "-1")
                {
                    loanTypeName = await connection.QueryFirstOrDefaultAsync<string>(
                        "SELECT LoanTypeName FROM LmtLoanTypeMaster WHERE LmtLoanTypeMasterId = @Id",
                        new { Id = request.LoanTypeId });
                }

                string? memberGroupName = null;
                if (!string.IsNullOrEmpty(request.MemberGroupId) && request.MemberGroupId != "-1")
                {
                    memberGroupName = await connection.QueryFirstOrDefaultAsync<string>(
                        "SELECT Name FROM SycMemberGroup WHERE SycMemberGroupId = @Id",
                        new { Id = request.MemberGroupId });
                }

                string? collectionCenterName = null;
                if (!string.IsNullOrEmpty(request.CollectionCenterId) && request.CollectionCenterId != "-1")
                {
                    collectionCenterName = await connection.QueryFirstOrDefaultAsync<string>(
                        $"SELECT STRING_AGG(CollectionCenterName, ', ') FROM SycCollectionCenter WHERE SycCollectionCenterId IN ({request.CollectionCenterId})");
                }

                string? collectorName = null;
                if (!string.IsNullOrEmpty(request.CollectorId) && request.CollectorId != "-1")
                {
                    collectorName = await connection.QueryFirstOrDefaultAsync<string>(
                        "SELECT CollectorFullName FROM HurCollector WHERE HurCollectorId = @Id",
                        new { Id = request.CollectorId });
                }

                return new LoanTypeWiseData
                {
                    Rows = rows,
                    TotalRecords = rows.Count,
                    TotalDisburseAmount = rows.Sum(r => r.DisburseAmount ?? 0),
                    TotalRepaid = rows.Sum(r => r.Repaid ?? 0),
                    TotalBalanceAmount = rows.Sum(r => r.BalanceAmount ?? 0),
                    TotalOpeningBalance = rows.Sum(r => r.OpeningBalance ?? 0),
                    TotalClosingBalance = rows.Sum(r => r.ClosingBalance ?? 0),
                    FromDateBs = request.FromDateBs,
                    ToDateBs = request.ToDateBs,
                    BranchName = branchName,
                    LoanTypeName = loanTypeName,
                    MemberGroupName = memberGroupName,
                    CollectionCenterName = collectionCenterName,
                    CollectorName = collectorName,
                    LoanGuarantee = request.LoanGuarantee,
                    OrderBy = request.OrderBy,
                    ShowOpeningBalance = request.ShowOpeningBalance
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