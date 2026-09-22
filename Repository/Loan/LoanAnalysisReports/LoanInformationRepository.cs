// Repository/Loan/LoanAnalysisReport/LoanInformationRepository.cs
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
    public class LoanInformationRepository : ILoanInformationRepository
    {
        private readonly AppDbContext _context;
        private readonly IDateConverterService _dateConverter;
        private readonly ILogger<LoanInformationRepository> _logger;

        public LoanInformationRepository(
            AppDbContext context,
            IDateConverterService dateConverter,
            ILogger<LoanInformationRepository> logger)
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
                "MemberId" => "ORDER BY SUBSTRING(MemberId, 1, LEN(MemberId) - CHARINDEX('-', MemberId) - 1), MemberId",
                "FullName" => "order by FullName",
                "LoanType" => "order by LoanTypeName",
                "LoanAcAmount" => "ORDER BY SUBSTRING(LoanAccountNo, 1, LEN(LoanAccountNo) - CHARINDEX('-', LoanAccountNo) - 1), LoanAccountNo",
                _ => string.Empty
            };
        }

        public async Task<LoanInformationData> GetReportDataAsync(LoanInformationRequestDto request)
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
                    sqlFilterExp.Append(" And Ls.UsmOfficeId in (").Append(branchIds).Append(")");
                }

                if (!string.IsNullOrEmpty(request.MemberGroupId) && request.MemberGroupId != "-1")
                {
                    sqlFilterExp.Append(" AND MR.SycMemberGroupId = ").Append(request.MemberGroupId);
                }

                var transactionDate = new StringBuilder();
                if (!string.IsNullOrEmpty(fromDateStr) && !string.IsNullOrEmpty(toDateStr))
                {
                    transactionDate.Append(" And TransactionOn >= '").Append(fromDateStr)
                                   .Append("' and TransactionOn <= '").Append(toDateStr).Append("'");
                }

                var sqlOrderExp = BuildSqlOrderBy(request.OrderBy);

                var parameters = new DynamicParameters();
                parameters.Add("@SqlFilterExp", sqlFilterExp.ToString(), DbType.String, size: -1);
                parameters.Add("@TransactionDate", transactionDate.ToString(), DbType.String, size: -1);
                parameters.Add("@SqlOrderExp", sqlOrderExp, DbType.String, size: -1);
                parameters.Add("@SqlOrderExpFrom", fromDateStr, DbType.String, size: -1);
                parameters.Add("@SqlOrderExpTill", toDateStr, DbType.String, size: -1);

                var rows = (await connection.QueryAsync<LoanInformationRowDto>(
                    "sp_7_16_LoanDetailsReport",
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

                return new LoanInformationData
                {
                    Rows = rows,
                    TotalRecords = rows.Count,
                    TotalDisburseAmount = rows.Sum(r => r.DisburseAmount ?? 0),
                    TotalRepaidTill = rows.Sum(r => r.RepaidTill ?? 0),
                    TotalRepaid = rows.Sum(r => r.Repaid ?? 0),
                    TotalBalanceAmount = rows.Sum(r => r.BalanceAmount ?? 0),
                    FromDateBs = request.FromDateBs,
                    ToDateBs = request.ToDateBs,
                    BranchName = branchName,
                    LoanTypeName = loanTypeName,
                    MemberGroupName = memberGroupName,
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