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
        private const string BranchColumn = "OfficeName";
        private const string CollectionCenterColumn = "CollectionCenterName";
        private const string MemberGroupColumn = "GroupName";

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
                .Where(id => long.TryParse(id, out _))
                .Select(id => long.Parse(id).ToString());

            var joined = string.Join(",", validIds);
            return string.IsNullOrEmpty(joined) ? "-1" : joined;
        }

        private static long? ParseSingleId(string? id)
        {
            if (string.IsNullOrWhiteSpace(id) || id == "-1" || id == "string")
                return null;

            return long.TryParse(id.Trim(), out var value) && value != -1 ? value : null;
        }

        private static string? NormalizeGuarantee(string? loanGuarantee)
        {
            if (string.IsNullOrWhiteSpace(loanGuarantee))
                return null;

            var value = loanGuarantee.Trim();

            if (value.Equals("Group Guarantee", StringComparison.OrdinalIgnoreCase))
                return "Group Guarantee";

            if (value.Equals("Collateral", StringComparison.OrdinalIgnoreCase))
                return "Collateral";

            return null;
        }

        private static string BuildSqlOrderBy(LoanTypeWiseRequestDto request)
        {
            var parts = new List<string>();

            if (request.GroupByBranch)
                parts.Add(BranchColumn);

            if (request.EnableCollectionCenter)
                parts.Add(CollectionCenterColumn);

            if (request.GroupByMemberGroup)
                parts.Add(MemberGroupColumn);

            var orderBy = request.OrderBy?.Trim();

            if (orderBy == "LoanType")
                parts.Add("LoanTypeName");
            else if (orderBy == "DisburseAmount")
                parts.Add("DisburseAmount");

            return parts.Count == 0 ? string.Empty : " order by " + string.Join(", ", parts);
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

                if (string.IsNullOrWhiteSpace(request.BranchIds))
                {
                    throw new ArgumentException("Please select Branch Name");
                }

                var branchIds = SanitizeIdList(request.BranchIds);
                var collectionCenterIds = SanitizeIdList(request.CollectionCenterId);
                var loanTypeId = ParseSingleId(request.LoanTypeId);
                var memberGroupId = ParseSingleId(request.MemberGroupId);
                var collectorId = ParseSingleId(request.CollectorId);
                var guarantee = NormalizeGuarantee(request.LoanGuarantee);

                var connectionString = _context.Database.GetConnectionString();
                using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();

                var fromDateAd = await _dateConverter.NepaliToEnglishAsync(request.FromDateBs);
                var toDateAd = await _dateConverter.NepaliToEnglishAsync(request.ToDateBs);

                var fromDateStr = fromDateAd.ToString("yyyy-MM-dd");
                var toDateStr = toDateAd.ToString("yyyy-MM-dd");

                var sqlFilterExp = new StringBuilder();

                if (loanTypeId.HasValue)
                {
                    sqlFilterExp.Append(" And Ltm.LmtLoanTypeMasterId = ").Append(loanTypeId.Value);
                }

                if (branchIds != "-1")
                {
                    sqlFilterExp.Append(" And LS.UsmOfficeId in(").Append(branchIds).Append(")");
                }

                if (memberGroupId.HasValue)
                {
                    sqlFilterExp.Append(" AND MR.SycMemberGroupId = ").Append(memberGroupId.Value);
                }

                if (collectorId.HasValue)
                {
                    sqlFilterExp.Append(" And LS.HurCollectorId = ").Append(collectorId.Value);
                }

                if (collectionCenterIds != "-1")
                {
                    sqlFilterExp.Append(" And LS.SycCollectionCenterId in(").Append(collectionCenterIds).Append(")");
                }

                if (guarantee != null)
                {
                    sqlFilterExp.Append(" And LS.LoanGuarantee = '").Append(guarantee).Append("'");
                }

                if (request.ShareType > 0)
                {
                    sqlFilterExp.Append(" And exists (select 1 from ShmShareAllotment shm")
                                .Append(" where shm.MemMemberRegistrationId = Mr.MemMemberRegistrationId")
                                .Append(" and shm.IsActive = 1")
                                .Append(" and shm.ShmShareTypeId = ").Append(request.ShareType)
                                .Append(")");
                }

                var sqlLoanIssueDate = new StringBuilder();
                sqlLoanIssueDate.Append(" And LL.LoanIssueOn >= '").Append(fromDateStr)
                                .Append("' and LL.LoanIssueOn <= '").Append(toDateStr).Append("'");

                var transactionDate = new StringBuilder();
                transactionDate.Append(" And T.TransactionOn >= '").Append(fromDateStr)
                               .Append("' and T.TransactionOn <= '").Append(toDateStr).Append("'");

                var sqlOrderExp = BuildSqlOrderBy(request);

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
                long companyBranchId = -1;

                if (branchIds != "-1")
                {
                    var names = await connection.QueryAsync<string>(
                        $"SELECT OfficeName FROM UsmOffice WHERE UsmOfficeId IN ({branchIds})");
                    var nameList = names.Where(n => !string.IsNullOrWhiteSpace(n)).ToList();
                    branchName = nameList.Count > 0 ? string.Join(", ", nameList) : "All";

                    var idParts = branchIds.Split(',');
                    if (!request.SameCompanyName && idParts.Length == 1)
                    {
                        companyBranchId = long.Parse(idParts[0]);
                    }
                }

                string? loanTypeName = null;
                if (loanTypeId.HasValue)
                {
                    loanTypeName = await connection.QueryFirstOrDefaultAsync<string>(
                        "SELECT LoanTypeName FROM LmtLoanTypeMaster WHERE LmtLoanTypeMasterId = @Id",
                        new { Id = loanTypeId.Value });
                }

                string? memberGroupName = null;
                if (memberGroupId.HasValue)
                {
                    memberGroupName = await connection.QueryFirstOrDefaultAsync<string>(
                        "SELECT Name FROM SycMemberGroup WHERE SycMemberGroupId = @Id",
                        new { Id = memberGroupId.Value });
                }

                string? collectionCenterName = null;
                if (collectionCenterIds != "-1")
                {
                    collectionCenterName = await connection.QueryFirstOrDefaultAsync<string>(
                        $"SELECT STRING_AGG(CollectionCenterName, ', ') FROM SycCollectionCenter WHERE SycCollectionCenterId IN ({collectionCenterIds})");
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
                    ShowOpeningBalance = request.ShowOpeningBalance,
                    ShowDetail = request.ShowDetail,
                    EnableCollectionCenter = request.EnableCollectionCenter,
                    GroupByBranch = request.GroupByBranch,
                    GroupByMemberGroup = request.GroupByMemberGroup,
                    ViewCollector = request.ViewCollector,
                    SameCompanyName = request.SameCompanyName,
                    CompanyBranchId = companyBranchId
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