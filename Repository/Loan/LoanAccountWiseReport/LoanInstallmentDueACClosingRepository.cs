
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
    public class LoanInstallmentDueACClosingRepository : ILoanInstallmentDueACClosingRepository
    {
        private readonly AppDbContext _context;
        private readonly IDateConverterService _dateConverter;
        private readonly ILogger<LoanInstallmentDueACClosingRepository> _logger;

        public LoanInstallmentDueACClosingRepository(
            AppDbContext context,
            IDateConverterService dateConverter,
            ILogger<LoanInstallmentDueACClosingRepository> logger)
        {
            _context = context;
            _dateConverter = dateConverter;
            _logger = logger;
        }

        public async Task<LoanInstallmentDueACClosingData> GetReportDataAsync(LoanInstallmentDueACClosingRequestDto request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.AccountNo))
                {
                    throw new ArgumentException("Account No is required.");
                }

                if (string.IsNullOrEmpty(request.TillDateBs) || request.TillDateBs == "-1")
                {
                    throw new ArgumentException("Till Date is required.");
                }

                var connectionString = _context.Database.GetConnectionString();
                using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();

                var loanIssueId = await connection.QueryFirstOrDefaultAsync<long?>(
                    @"SELECT TOP 1 LmtLoanIssueId 
                      FROM LmtLoanIssue 
                      WHERE LoanAccountNo = @AccountNo 
                        AND IsActive = 1 
                        AND IsVerified = 1
                        AND TransStatus IN ('I', 'R')
                        AND LmtLoanStatusId = 1
                      ORDER BY LmtLoanIssueId DESC",
                    new { AccountNo = request.AccountNo.Trim() });

                if (!loanIssueId.HasValue)
                {
                    throw new ArgumentException($"No active loan account found for Account No '{request.AccountNo}'.");
                }

                var tillDateAd = await _dateConverter.NepaliToEnglishAsync(request.TillDateBs);
                var tillDateStr = tillDateAd.ToString("yyyy-MM-dd");

                var memberInfoParams = new DynamicParameters();
                memberInfoParams.Add("@SqlFilterExp", $" And LS.LmtLoanIssueId = {loanIssueId.Value}", DbType.String, size: -1);

                var memberInfo = await connection.QueryFirstOrDefaultAsync<LoanInstallmentDueACClosingMemberInfoDto>(
                    "sp_7_16_MemberInfoDetailsReport",
                    memberInfoParams,
                    commandType: CommandType.StoredProcedure,
                    commandTimeout: 120
                );

                if (memberInfo == null)
                {
                    throw new ArgumentException($"No member info found for Account No '{request.AccountNo}'.");
                }


                var paymentParams = new DynamicParameters();
                paymentParams.Add("@SqlFilterExp", $"'{request.AccountNo.Trim()}'", DbType.String, size: -1);

                var payments = (await connection.QueryAsync<LoanInstallmentDueACClosingPaymentDto>(
                    "sp_7_16_InstallmentACClosingReportForPaymentDetails",
                    paymentParams,
                    commandType: CommandType.StoredProcedure,
                    commandTimeout: 120
                )).AsList();

                var installmentParams = new DynamicParameters();
                installmentParams.Add("@SqlFilterExp1", $"'{request.AccountNo.Trim()}'");
                installmentParams.Add("@SqlFilterExp2", $"'{request.CalcBySwOrAsOnDate}'");
                installmentParams.Add("@SqlFilterExp3", $"'{request.CalcByPenaltyType}'");
                installmentParams.Add("@SqlFilterExp4", $"'{request.CalcByPrincipleInterest}'");
                installmentParams.Add("@SqlFilterExp5", $"'{request.PenaltyAmount}'");
                installmentParams.Add("@SqlTillDate", $"'{tillDateStr}'");

                var installments = (await connection.QueryAsync<LoanInstallmentDueACClosingInstallmentDto>(
                    "sp_7_16_LoanInstallmentForInstallmentDueDetail",
                    installmentParams,
                    commandType: CommandType.StoredProcedure,
                    commandTimeout: 120
                )).AsList();

                var closingDues = new List<LoanInstallmentDueACClosingDueDto>();
                var penaltyDetails = new List<LoanInstallmentDueACClosingDueDto>();

                using (var multi = await connection.QueryMultipleAsync(
                    "sp_7_16_LoanInstallmentForAccountClosingDueDetails",
                    installmentParams,
                    commandType: CommandType.StoredProcedure,
                    commandTimeout: 120))
                {

                    var firstTable = (await multi.ReadAsync<LoanInstallmentDueACClosingDueDto>()).AsList();
                    if (firstTable.Any())
                    {
                        penaltyDetails = firstTable;
                    }


                    try
                    {
                        var secondTable = (await multi.ReadAsync<LoanInstallmentDueACClosingDueDto>()).AsList();
                        if (secondTable.Any())
                        {
                            closingDues = secondTable;
                        }
                        else
                        {
                            closingDues = firstTable;
                        }
                    }
                    catch
                    {
                        closingDues = firstTable;
                    }
                }


                string? lastDueDate = null;
                string? nextDueDate = null;

                var scheduleItems = await connection.QueryAsync<dynamic>(
                    @"SELECT ScheduleDateOn, ScheduleDateOnBs, IsPaid, LoanFrequency, IsActive
                      FROM LmtLoanSchedule
                      WHERE LmtLoanIssueId = @LoanIssueId AND IsActive = 1
                      ORDER BY ScheduleDateOn",
                    new { LoanIssueId = loanIssueId.Value });

                var scheduleList = scheduleItems.AsList();
                var today = DateTime.Now;

                var overdueItem = scheduleList
                    .FirstOrDefault(s => (DateTime)s.ScheduleDateOn < today
                                         && (bool)s.IsPaid == false
                                         && (int)s.LoanFrequency != 0
                                         && (bool)s.IsActive == true);

                if (overdueItem != null)
                {
                    lastDueDate = (string)overdueItem.ScheduleDateOnBs;
                }
                else
                {
                    var nextItem = scheduleList
                        .FirstOrDefault(s => (DateTime)s.ScheduleDateOn >= today
                                             && (bool)s.IsPaid == false
                                             && (int)s.LoanFrequency != 0
                                             && (bool)s.IsActive == true);

                    if (nextItem != null)
                    {
                        nextDueDate = (string)nextItem.ScheduleDateOnBs;
                    }
                }

                return new LoanInstallmentDueACClosingData
                {
                    MemberInfo = memberInfo,
                    Payments = payments,
                    Installments = installments,
                    ClosingDues = closingDues,
                    TotalRecords = installments.Count,
                    TotalPrinciple = installments.Sum(i => i.Principle ?? 0),
                    TotalInterest = installments.Sum(i => i.Interest ?? 0),
                    TotalPenalty = installments.Sum(i => i.NetPenalty ?? 0),
                    TotalDueAmount = installments.Sum(i => i.DueAmount ?? 0),
                    TotalNetPenalty = installments.Sum(i => i.NetPenalty ?? 0),
                    TillDateBs = request.TillDateBs,
                    CalcBySwOrAsOnDate = request.CalcBySwOrAsOnDate,
                    CalcByPenaltyType = request.CalcByPenaltyType,
                    CalcByPrincipleInterest = request.CalcByPrincipleInterest,
                    PenaltyAmount = request.PenaltyAmount,
                    LastDueDate = lastDueDate,
                    NextDueDate = nextDueDate
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