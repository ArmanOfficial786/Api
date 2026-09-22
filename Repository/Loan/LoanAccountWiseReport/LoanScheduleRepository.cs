
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using NexgenCosysReport.DbContext;
using NexgenCosysReport.Dtos.RequestDtos.Loan.OtherReports;
using NexgenCosysReport.Inteface.ServiceInterface.Common;
using NexgenCosysReport.Inteface.ServiceInterface.Loan.OtherReports;
using System.Data;

namespace NexgenCosysReport.Repository.Loan.OtherReports
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
                        new { AccountNo = request.AccountNo.Trim() }) ?? -1;

                    if (loanIssueId <= 0)
                    {
                        throw new ArgumentException($"No active loan account found for Account No '{request.AccountNo}'.");
                    }
                }


                var memberInfoParams = new DynamicParameters();
                memberInfoParams.Add("@SqlFilterExp", $" AND ls.LoanAccountNo = '{request.AccountNo.Trim()}'", DbType.String, size: -1);

                var memberInfo = await connection.QueryFirstOrDefaultAsync<LoanScheduleMemberInfoDto>(
                    "sp_7_16_MemberDetailForLoanSchedule",
                    memberInfoParams,
                    commandType: CommandType.StoredProcedure,
                    commandTimeout: 120
                );


                var scheduleParams = new DynamicParameters();
                scheduleParams.Add("@SqlFilterExp", $" and ls.LmtLoanIssueId = {loanIssueId}", DbType.String, size: -1);

                var rows = (await connection.QueryAsync<LoanScheduleRowDto>(
                    "sp_7_16_LoanScheduleReport",
                    scheduleParams,
                    commandType: CommandType.StoredProcedure,
                    commandTimeout: 120
                )).AsList();


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
    }
}