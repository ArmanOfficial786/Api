// Repository/Loan/OtherReports/LoanInterestDiscountRepository.cs
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using NexgenCosysReport.DbContext;
using NexgenCosysReport.Dtos.RequestDtos.Loan.OtherReports;
using NexgenCosysReport.Inteface.ServiceInterface.Common;
using NexgenCosysReport.Inteface.ServiceInterface.Loan.OtherReports;
using System.Data;
using System.Text;

namespace NexgenCosysReport.Repository.Loan.OtherReports
{
    public class LoanInterestDiscountRepository : ILoanInterestDiscountRepository
    {
        private readonly AppDbContext _context;
        private readonly IDateConverterService _dateConverter;
        private readonly ILogger<LoanInterestDiscountRepository> _logger;

        public LoanInterestDiscountRepository(
            AppDbContext context,
            IDateConverterService dateConverter,
            ILogger<LoanInterestDiscountRepository> logger)
        {
            _context = context;
            _dateConverter = dateConverter;
            _logger = logger;
        }

        private static string BuildSqlOrderBy(LoanInterestDiscountRequestDto request)
        {
            if (string.IsNullOrEmpty(request.OrderBy) || request.OrderBy == "-1")
                return string.Empty;

            return request.OrderBy.Trim() switch
            {
                "Date" => " ORDER BY TransactionOnBs",
                "MemberId" => " ORDER BY SUBSTRING(MemberId, 1, LEN(MemberId) - CHARINDEX('-', MemberId) - 1), MemberId",
                "FullName" => " order by FullName",
                "LoanType" => " order by LoanTypeName",
                "LoanAcAmount" => " ORDER BY SUBSTRING(LoanAccountNo, 1, LEN(LoanAccountNo) - CHARINDEX('-', LoanAccountNo) - 1), LoanAccountNo",
                "Amount" => " ORDER BY CashReceived",
                _ => string.Empty
            };
        }

        private static string SanitizeBranchIds(string? branchIds)
        {
            if (string.IsNullOrWhiteSpace(branchIds) || branchIds == "-1" || branchIds == "string")
                return string.Empty;

            var validIds = branchIds
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Where(id => long.TryParse(id, out _));

            return string.Join(",", validIds);
        }

        public async Task<LoanInterestDiscountData> GetReportDataAsync(LoanInterestDiscountRequestDto request)
        {
            try
            {
                var branchIds = SanitizeBranchIds(request.BranchIds);

                var connectionString = _context.Database.GetConnectionString();
                using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();

                var sqlFilterExp = new StringBuilder();
                string? memberName = null;
                string? loanTypeName = null;

                // FIX: "string" placeholder guard added for MemberId, matching the
                // pattern already used for BranchIds — a Swagger placeholder value
                // left unreplaced would otherwise filter on a literal MemberId of
                // "string", zeroing out the result set.
                if (!string.IsNullOrWhiteSpace(request.MemberId) && request.MemberId.Trim() != "string")
                {
                    sqlFilterExp.Append(" And MR.MemberId = '").Append(request.MemberId.Trim()).Append("'");

                    var name = await connection.QueryFirstOrDefaultAsync<string>(
                        @"SELECT FirstName + ' ' +
                                 CASE WHEN MiddleName = '' THEN '' ELSE MiddleName + ' ' END +
                                 LastName
                          FROM MemMemberRegistration 
                          WHERE MemberId = @MemberId AND IsActive = 1",
                        new { MemberId = request.MemberId.Trim() });
                    memberName = name;
                }

                if (!string.IsNullOrEmpty(branchIds))
                {
                    sqlFilterExp.Append(" And LIs.UsmOfficeId in (").Append(branchIds).Append(")");
                }

                // FIX: root cause of "No data found" — LoanTypeId was checked against
                // -1 only, so a default/unset value of 0 (as sent in the reported
                // request) was treated as an explicit filter for loan type id 0,
                // which almost certainly matches nothing and zeroed out every row.
                // Now both -1 (explicit "no filter" sentinel) and <= 0 (default/unset)
                // are treated as "no filter".
                if (request.LoanTypeId > 0)
                {
                    sqlFilterExp.Append(" And LTMr.LmtLoanTypeMasterId = ").Append(request.LoanTypeId);

                    loanTypeName = await connection.QueryFirstOrDefaultAsync<string>(
                        "SELECT LoanTypeName FROM LmtLoanTypeMaster WHERE LmtLoanTypeMasterId = @Id",
                        new { Id = request.LoanTypeId });
                }

                if (!string.IsNullOrEmpty(request.FromDateBs) && !string.IsNullOrEmpty(request.ToDateBs)
                    && request.FromDateBs != "-1" && request.ToDateBs != "-1")
                {
                    var fromDateAd = await _dateConverter.NepaliToEnglishAsync(request.FromDateBs);
                    var toDateAd = await _dateConverter.NepaliToEnglishAsync(request.ToDateBs);

                    var fromDateStr = fromDateAd.ToString("yyyy-MM-dd");
                    var toDateStr = toDateAd.ToString("yyyy-MM-dd");

                    sqlFilterExp.Append(" And Ac.TransactionOn >= '").Append(fromDateStr).Append("'");
                    sqlFilterExp.Append(" And Ac.TransactionOn <= '").Append(toDateStr).Append("'");
                }

                var sqlOrderBy = BuildSqlOrderBy(request);
                sqlFilterExp.Append(sqlOrderBy);

                var parameters = new DynamicParameters();
                parameters.Add("@SqlFilterExp", sqlFilterExp.ToString(), DbType.String, size: -1);

                var rows = await connection.QueryAsync<LoanInterestDiscountRowDto>(
                    "sp_7_16_LoanInterestDiscountReport",
                    parameters,
                    commandType: CommandType.StoredProcedure,
                    commandTimeout: 120
                );

                var resultList = rows.AsList();

                string branchName = "All";
                if (!string.IsNullOrEmpty(branchIds))
                {
                    var names = await connection.QueryAsync<string>(
                        $"SELECT OfficeName FROM UsmOffice WHERE UsmOfficeId IN ({branchIds})");
                    var nameList = names.Where(n => !string.IsNullOrWhiteSpace(n)).ToList();
                    branchName = nameList.Count > 0 ? string.Join(", ", nameList) : "All";
                }

                return new LoanInterestDiscountData
                {
                    Rows = resultList,
                    TotalRecords = resultList.Count,
                    TotalAmount = resultList.Sum(r => r.CashReceived ?? 0),
                    FromDateBs = request.FromDateBs,
                    ToDateBs = request.ToDateBs,
                    BranchName = branchName,
                    MemberName = memberName,
                    LoanTypeName = loanTypeName,
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