using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using NexgenCosysReport.DbContext;
using NexgenCosysReport.Dtos.RequestDtos.Loan.LoanAccountWiseReport;
using NexgenCosysReport.Inteface.ServiceInterface.Common;
using NexgenCosysReport.Inteface.ServiceInterface.Loan.OtherReports;
using System.Data;

namespace NexgenCosysReport.Repository.Loan.LoanAccountWiseReport
{
    public class LoanScheduleRepository : ILoanScheduleRepository
    {
        private readonly AppDbContext _context;
        private readonly IDateConverterService _dateConverter;
        private readonly ILogger<LoanScheduleRepository> _logger;

        public LoanScheduleRepository(
            AppDbContext context,
            IDateConverterService dateConverter,
            ILogger<LoanScheduleRepository> logger)
        {
            _context = context;
            _dateConverter = dateConverter;
            _logger = logger;
        }

        public async Task<LoanScheduleData> GetReportDataAsync(LoanScheduleRequestDto request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.AccountNo))
                {
                    throw new ArgumentException("Account No is required.");
                }

                var accountNo = request.AccountNo.Trim();

                var connectionString = _context.Database.GetConnectionString();
                using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();

                long loanIssueId = request.LoanIssueId;

                if (loanIssueId <= 0)
                {
                    loanIssueId = await connection.QueryFirstOrDefaultAsync<long?>(
                        @"SELECT TOP 1 LmtLoanIssueId 
                          FROM LmtLoanIssue 
                          WHERE LoanAccountNo = @AccountNo 
                            AND IsActive = 1 
                            AND LmtLoanStatusId IN (1, 3)
                          ORDER BY LmtLoanIssueId DESC",
                        new { AccountNo = accountNo }) ?? -1;

                    if (loanIssueId <= 0)
                    {
                        throw new ArgumentException($"No active loan account found for Account No '{accountNo}'.");
                    }
                }

                var memberInfoParams = new DynamicParameters();
                memberInfoParams.Add("@SqlFilterExp", $" AND ls.LoanAccountNo = '{accountNo}'", DbType.String, size: -1);

                var memberInfo = await connection.QueryFirstOrDefaultAsync<LoanScheduleMemberInfoDto>(
                    "sp_7_16_MemberDetailForLoanSchedule",
                    memberInfoParams,
                    commandType: CommandType.StoredProcedure,
                    commandTimeout: 120
                );

                var rows = await GetScheduleRowsAsync(connection, loanIssueId);

                // --------------------------------------------------------------
                // FIX: the LmtLoanIssueId picked above (most recent active row
                // for this account) sometimes has no matching rows in
                // LmtLoanSchedule at all - e.g. after a reschedule created a new
                // LmtLoanIssue row while the schedule is still tied to an older
                // one. Rather than failing outright, retry every LmtLoanIssueId
                // ever recorded for this account and use whichever one actually
                // has schedule rows.
                // --------------------------------------------------------------
                if (!rows.Any())
                {
                    var candidateIds = (await connection.QueryAsync<long>(
                        @"SELECT LmtLoanIssueId 
                          FROM LmtLoanIssue 
                          WHERE LoanAccountNo = @AccountNo 
                          ORDER BY LmtLoanIssueId DESC",
                        new { AccountNo = accountNo })).ToList();

                    foreach (var candidateId in candidateIds)
                    {
                        if (candidateId == loanIssueId) continue;

                        var retryRows = await GetScheduleRowsAsync(connection, candidateId);
                        if (retryRows.Any())
                        {
                            loanIssueId = candidateId;
                            rows = retryRows;
                            break;
                        }
                    }
                }

                return new LoanScheduleData
                {
                    MemberInfo = memberInfo ?? new LoanScheduleMemberInfoDto(),
                    Rows = rows,
                    TotalRecords = rows.Count,
                    TotalInstallmentAmount = rows.Sum(r => r.InstallmentAmount ?? 0),
                    TotalPrincipleAmount = rows.Sum(r => r.PrincipleAmount ?? 0),
                    TotalInterestAmount = rows.Sum(r => r.InterestAmount ?? 0),
                    TotalBalanceAmount = rows.LastOrDefault()?.PrincipleBalanceAmount ?? 0
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetReportDataAsync");
                throw;
            }
        }

        private static async Task<List<LoanScheduleRowDto>> GetScheduleRowsAsync(SqlConnection connection, long loanIssueId)
        {
            var scheduleParams = new DynamicParameters();
            scheduleParams.Add("@SqlFilterExp", $" and ls.LmtLoanIssueId = {loanIssueId}", DbType.String, size: -1);

            return (await connection.QueryAsync<LoanScheduleRowDto>(
                "sp_7_16_LoanScheduleReport",
                scheduleParams,
                commandType: CommandType.StoredProcedure,
                commandTimeout: 120
            )).AsList();
        }
    }
}