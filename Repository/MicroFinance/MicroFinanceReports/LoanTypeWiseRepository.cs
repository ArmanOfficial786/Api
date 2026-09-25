// Repository/Microfinance/MicrofinanceReport/LoanTypeWiseRepository.cs
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
                "LoanType" => " order by LoanTypeName",
                "DisburseAmount" => " order by DisburseAmount",
                _ => string.Empty
            };
        }

        private static readonly HashSet<string> AllowedLoanGuarantee = new(StringComparer.OrdinalIgnoreCase)
        {
            "Group Guarantee",
            "Collateral"
        };

        public async Task<LoanTypeWiseData> GetReportDataAsync(LoanTypeWiseRequestDto request)
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

                long? loanTypeId = null;
                if (!string.IsNullOrEmpty(request.LoanTypeId) &&
                    request.LoanTypeId != "-1" &&
                    long.TryParse(request.LoanTypeId, out var lt))
                {
                    loanTypeId = lt;
                }

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

                int? shareType = null;
                if (!string.IsNullOrEmpty(request.ShareType) &&
                    request.ShareType != "-1" &&
                    int.TryParse(request.ShareType, out var st))
                {
                    shareType = st;
                }

                var loanGuarantee = "-1";
                if (!string.IsNullOrEmpty(request.LoanGuarantee) && request.LoanGuarantee != "-1")
                {
                    var lg = request.LoanGuarantee.Trim();
                    if (AllowedLoanGuarantee.Contains(lg))
                        loanGuarantee = lg;
                }

                var connectionString = _context.Database.GetConnectionString();
                using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();

                var fromDateAd = await _dateConverter.NepaliToEnglishAsync(request.FromDateBs);
                var toDateAd = await _dateConverter.NepaliToEnglishAsync(request.ToDateBs);

                var fromDateStr = $"'{fromDateAd:MM/dd/yyyy}'";
                var toDateStr = $"'{toDateAd:MM/dd/yyyy}'";

                var sqlFilterExp = string.Empty;
                var transactionDate = string.Empty;
                var sqlOrderExp = BuildSqlOrderBy(request.OrderBy);
                var sqlOrderExpFrom = fromDateStr;
                var sqlOrderExpTill = toDateStr;
                var sqlLoanIssueDate = $" And LL.LoanIssueOn >= {fromDateStr} and LL.LoanIssueOn <= {toDateStr}";

                if (loanTypeId.HasValue)
                    sqlFilterExp += $" And Ltm.LmtLoanTypeMasterId = {loanTypeId.Value}";

                if (branchIds != "-1")
                    sqlFilterExp += $" And LS.UsmOfficeId in({branchIds})";

                if (memberGroupId.HasValue)
                    sqlFilterExp += $" AND MR.SycMemberGroupId = {memberGroupId.Value}";

                if (collectorId.HasValue)
                    sqlFilterExp += $" And LS.HurCollectorId = {collectorId.Value}";

                if (collectionCenterIds != "-1")
                    sqlFilterExp += $" And LS.SycCollectionCenterId in({collectionCenterIds})";

                if (loanGuarantee != "-1")
                    sqlFilterExp += $" And LS.LoanGuarantee = '{loanGuarantee}'";

                if (shareType.HasValue)
                    sqlFilterExp += $" And shm.ShmShareTypeId = {shareType.Value}";

                transactionDate += $" And T.TransactionOn >= {fromDateStr} and T.TransactionOn <= {toDateStr}";

                var parameters = new DynamicParameters();
                parameters.Add("@SqlFilterExp", sqlFilterExp, DbType.String, size: -1);
                parameters.Add("@TransactionDate", transactionDate, DbType.String, size: -1);
                parameters.Add("@SqlOrderExp", sqlOrderExp, DbType.String, size: -1);
                parameters.Add("@SqlOrderExpFrom", sqlOrderExpFrom, DbType.String, size: -1);
                parameters.Add("@SqlOrderExpTill", sqlOrderExpTill, DbType.String, size: -1);
                parameters.Add("@SqlLoanIssueDate", sqlLoanIssueDate, DbType.String, size: -1);

                var rows = (await connection.QueryAsync<LoanTypeWiseRowDto>(
                    "sp_7_16_LoanTypeWiseReport",
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

                string? loanTypeName = null;
                if (loanTypeId.HasValue)
                {
                    loanTypeName = await connection.QueryFirstOrDefaultAsync<string>(
                        "SELECT LoanTypeName FROM LmtLoanTypeMaster WHERE LmtLoanTypeMasterId = @Id",
                        new { Id = loanTypeId.Value });
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

                return new LoanTypeWiseData
                {
                    Rows = rows,
                    TotalRecords = rows.Count,
                    TotalDisburseAmount = rows.Sum(r => r.DisburseAmount ?? 0),
                    TotalOpeningDisburseAmount = rows.Sum(r => r.OpeningDisburseAmount ?? 0),
                    TotalRepaid = rows.Sum(r => r.Repaid ?? 0),
                    TotalBalanceAmount = rows.Sum(r => r.BalanceAmount ?? 0),
                    TotalOpeningPaid = rows.Sum(r => r.OpeningPaid ?? 0),
                    TotalOpeningBalance = rows.Sum(r => r.OpeningBalance ?? 0),
                    TotalClosingBalance = rows.Sum(r => r.ClosingBalance ?? 0),
                    FromDateBs = request.FromDateBs,
                    ToDateBs = request.ToDateBs,
                    FromDateAd = fromDateAd.ToString("yyyy-MM-dd"),
                    ToDateAd = toDateAd.ToString("yyyy-MM-dd"),
                    BranchName = branchName,
                    LoanTypeName = loanTypeName,
                    CollectionCenterName = collectionCenterName,
                    MemberGroupName = memberGroupName,
                    CollectorName = collectorName,
                    LoanGuarantee = loanGuarantee,
                    ShareType = request.ShareType,
                    OrderBy = request.OrderBy,
                    SameCompanyName = request.SameCompanyName,
                    ShowOpeningBalance = request.ShowOpeningBalance,
                    ShowDetail = request.ShowDetail,
                    GroupByBranch = request.GroupByBranch,
                    GroupByCollectionCenter = request.GroupByCollectionCenter,
                    GroupByMemberGroup = request.GroupByMemberGroup,
                    ViewCollector = request.ViewCollector
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetReportDataAsync (LoanTypeWise)");
                throw;
            }
        }
    }
}