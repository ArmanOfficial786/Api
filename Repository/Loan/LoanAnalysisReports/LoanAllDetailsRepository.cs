// Repository/Loan/LoanAnalysisReport/LoanAllDetailsRepository.cs
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
    public class LoanAllDetailsRepository : ILoanAllDetailsRepository
    {
        private readonly AppDbContext _context;
        private readonly IDateConverterService _dateConverter;
        private readonly ILogger<LoanAllDetailsRepository> _logger;

        public LoanAllDetailsRepository(
            AppDbContext context,
            IDateConverterService dateConverter,
            ILogger<LoanAllDetailsRepository> logger)
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
                "MemberId" => " order by substring(MemberId, 1,(len(MemberId)-charindex('-', MemberId))-1), MemberId ",
                "MemberName" => " order by MemberName",
                "LoanAccountNo" => " order by substring(LoanAccountNo, 1,(len(LoanAccountNo)-charindex('-', LoanAccountNo))-1), LoanAccountNo ",
                "LoanTypeName" => " order by LoanTypeName",
                "LoanIssueAmount" => " order by LoanIssueAmount DESC",
                "IssueDate" => " order by IssueDate",
                "InterestRate" => " order by InterestRate",
                "LoanPaymentTypeCode" => " order by LoanPaymentTypeCode",
                "MaturityDate" => " order by MaturityDate",
                "TotalBalance" => " order by TotalRemaining",
                _ => string.Empty
            };
        }

        public async Task<LoanAllDetailsData> GetReportDataAsync(LoanAllDetailsRequestDto request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.TillDateBs) || request.TillDateBs == "-1")
                {
                    throw new ArgumentException("Till Date is required.");
                }

                var branchIds = SanitizeBranchIds(request.BranchIds);

                var connectionString = _context.Database.GetConnectionString();
                using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();

                var tillDateAd = await _dateConverter.NepaliToEnglishAsync(request.TillDateBs);
                var tillDateStr = $"'{tillDateAd:yyyy-MM-dd}'";

                var sqlFilterExp = new StringBuilder();
                sqlFilterExp.Append(" AND Ls.LoanIssueOn <= ").Append(tillDateStr);

                if (branchIds != "-1")
                {
                    sqlFilterExp.Append(" And LS.UsmOfficeId in(").Append(branchIds).Append(")");
                }

                if (!string.IsNullOrEmpty(request.CollectionCenterId) && request.CollectionCenterId != "-1")
                {
                    sqlFilterExp.Append(" And LS.SycCollectionCenterId in(").Append(request.CollectionCenterId).Append(")");
                }

                if (!string.IsNullOrEmpty(request.MemberGroupId) && request.MemberGroupId != "-1")
                {
                    sqlFilterExp.Append(" And smg.SycMemberGroupId in(").Append(request.MemberGroupId).Append(")");
                }

                if (!string.IsNullOrEmpty(request.LoanTypeId))
                {
                    sqlFilterExp.Append(" AND Lm.LmtLoanTypeMasterId = ").Append(request.LoanTypeId);
                }

                if (!string.IsNullOrEmpty(request.Status) && request.Status != "-1")
                {
                    sqlFilterExp.Append(" AND Ls.LmtLoanStatusId = ").Append(request.Status);
                }
                else
                {
                    sqlFilterExp.Append(" AND Ls.LmtLoanStatusId In (1,3)");
                }

                if (request.MemberRegistrationId != -1)
                {
                    sqlFilterExp.Append(" AND MR.MemMemberRegistrationId = ").Append(request.MemberRegistrationId);
                }

                if (!string.IsNullOrEmpty(request.CollectorId) && request.CollectorId != "-1")
                {
                    sqlFilterExp.Append(" and LS.HurCollectorId = ").Append(request.CollectorId);
                }

                var sqlFilterExpOrderBy = BuildSqlOrderBy(request.OrderBy);

                var parameters = new DynamicParameters();
                parameters.Add("@SqlFilterExp", sqlFilterExp.ToString(), DbType.String, size: -1);
                parameters.Add("@SqlTilldateExp", tillDateStr, DbType.String, size: -1);
                parameters.Add("@SqlFilterExpOrderBy", sqlFilterExpOrderBy, DbType.String, size: -1);

                var rows = (await connection.QueryAsync<LoanAllDetailsRowDto>(
                    "sp_7_16_GetAllLoanDetails",
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

                string? memberGroupName = null;
                if (!string.IsNullOrEmpty(request.MemberGroupId) && request.MemberGroupId != "-1")
                {
                    memberGroupName = await connection.QueryFirstOrDefaultAsync<string>(
                        $"SELECT STRING_AGG(Name, ', ') FROM SycMemberGroup WHERE SycMemberGroupId IN ({request.MemberGroupId})");
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

                string? loanTypeName = null;
                if (!string.IsNullOrEmpty(request.LoanTypeId))
                {
                    loanTypeName = await connection.QueryFirstOrDefaultAsync<string>(
                        "SELECT LoanTypeName FROM LmtLoanTypeMaster WHERE LmtLoanTypeMasterId = @Id",
                        new { Id = request.LoanTypeId });
                }

                var statusName = request.Status switch
                {
                    "1" => "Open",
                    "3" => "Close",
                    _ => "All"
                };

                return new LoanAllDetailsData
                {
                    Rows = rows,
                    TotalRecords = rows.Count,
                    TotalLoanIssueAmount = rows.Sum(r => r.LoanIssueAmount ?? 0),
                    TotalRemainingPrinciple = rows.Sum(r => r.RemainingPrinciple ?? 0),
                    TotalDefaultPrinciple = rows.Sum(r => r.DefaultPrinciple ?? 0),
                    TotalCurrInterest = rows.Sum(r => r.CurrInterest ?? 0),
                    TotalCurrPenalty = rows.Sum(r => r.CurrPenalty ?? 0),
                    TotalDue = rows.Sum(r => r.TotalDue ?? 0),
                    TotalRemaining = rows.Sum(r => r.TotalRemaining ?? 0),
                    TotalProvisionAmount = rows.Sum(r => r.ProvisionalAmount ?? 0),
                    TillDateBs = request.TillDateBs,
                    BranchName = branchName,
                    MemberGroupName = memberGroupName,
                    CollectionCenterName = collectionCenterName,
                    CollectorName = collectorName,
                    LoanTypeName = loanTypeName,
                    StatusName = statusName,
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