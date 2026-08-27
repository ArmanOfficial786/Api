//// Repository/MemberAccount/OthersReport/TellerWiseExpenseRepository.cs
//using Dapper;
//using Microsoft.Data.SqlClient;
//using Microsoft.EntityFrameworkCore;
//using NexgenCosysReport.DbContext;
//using NexgenCosysReport.Dtos.RequestDtos.MemberAccount.OthersReport;
//using NexgenCosysReport.Inteface.ServiceInterface.Common;
//using NexgenCosysReport.Inteface.ServiceInterface.MemberAccount.OthersReport;
//using System.Data;
//using System.Text;

//namespace NexgenCosysAPI.Repository.MemberAccount.OthersReport
//{
//    public class TellerExpenseRepository : ITellerExpense
//    {
//        private readonly AppDbContext _context;
//        private readonly IDateConverterService _dateConverter;
//        private readonly ILogger<TellerExpenseRepository> _logger;

//        public TellerExpenseRepository(AppDbContext context, IDateConverterService dateConverter, ILogger<TellerExpenseRepository> logger)
//        {
//            _context = context;
//            _dateConverter = dateConverter;
//            _logger = logger;
//        }

//        public async Task<TellerWiseExpenseData> GetReportDataAsync(TellerWiseExpenseRequestDto request)
//        {
//            try
//            {
//                // Convert Nepali dates to English (ISO format avoids SQL Server regional/language ambiguity)
//                var fromDateAd = await _dateConverter.NepaliToEnglishAsync(request.FromDateBs);
//                var toDateAd = await _dateConverter.NepaliToEnglishAsync(request.ToDateBs);

//                var fromDateStr = fromDateAd.ToString("yyyy-MM-dd");
//                var toDateStr = toDateAd.ToString("yyyy-MM-dd");

//                long tellerId = request.TellerId ?? -1;

//                // ---- Build @SqlFilterExp exactly like the legacy BLL method did ----
//                var sqlFilterExp = new StringBuilder();
//                if (tellerId != -1)
//                {
//                    sqlFilterExp.Append(" And t.CreatedBy = ").Append(tellerId);
//                }
//                sqlFilterExp.Append(" And t.TransactionOn between '")
//                            .Append(fromDateStr).Append("' And '")
//                            .Append(toDateStr).Append("' ");

//                // ---- Build @SqlFilterExpOrderBy exactly like the legacy BLL method did ----
//                var sqlFilterExpOrderBy = new StringBuilder();
//                switch (request.OrderBy)
//                {
//                    case "Account No":
//                        sqlFilterExpOrderBy.Append(" order by substring(Details, 1,(len(Details)-charindex('-', Details))-1), Details");
//                        break;
//                    case "Member Id":
//                        sqlFilterExpOrderBy.Append(" order by substring(MemberId, 1,(len(MemberId)-charindex('-', MemberId))-1), MemberId");
//                        break;
//                    case "Bill No":
//                        sqlFilterExpOrderBy.Append(" order by BillNo DESC");
//                        break;
//                    case "Savings Withdrawl":
//                        sqlFilterExpOrderBy.Append(" order by SavingWithdrawlAmount DESC");
//                        break;
//                    case "Share Return":
//                        sqlFilterExpOrderBy.Append(" order by ShareReturnAmount DESC");
//                        break;
//                    case "Loan Issue":
//                        sqlFilterExpOrderBy.Append(" order by LoanIssueAmount DESC");
//                        break;
//                    case "Misscellaneous Amt":
//                        sqlFilterExpOrderBy.Append(" order by MiscellaneousAmount DESC");
//                        break;
//                    default:
//                        sqlFilterExpOrderBy.Append(" order by MemberId");
//                        break;
//                }

//                var connectionString = _context.Database.GetConnectionString();
//                using var connection = new SqlConnection(connectionString);
//                await connection.OpenAsync();

//                // ---- Only pass the 2 params the SP actually declares ----
//                var parameters = new DynamicParameters();
//                parameters.Add("@SqlFilterExp", sqlFilterExp.ToString(), DbType.String, size: -1);
//                parameters.Add("@SqlFilterExpOrderBy", sqlFilterExpOrderBy.ToString(), DbType.String, size: -1);

//                var rows = await connection.QueryAsync<TellerWiseExpenseRowDto>(
//                    "sp_5_43_GetTellerWiseExpense",
//                    parameters,
//                    commandType: CommandType.StoredProcedure
//                );

//                var resultList = rows.AsList();

//                // Get teller name from the first row or from the database
//                string? tellerName = resultList.FirstOrDefault()?.TellerName;
//                if (tellerId != -1 && string.IsNullOrEmpty(tellerName))
//                {
//                    tellerName = await GetTellerNameByIdAsync(tellerId);
//                }

//                // Calculate totals from the result set
//                var totalSavingWithdrawl = resultList.Sum(r => r.SavingWithdrawlAmount ?? 0);
//                var totalShareReturn = resultList.Sum(r => r.ShareReturnAmount ?? 0);
//                var totalLoanIssue = resultList.Sum(r => r.LoanIssueAmount ?? 0);
//                var totalMiscellaneous = resultList.Sum(r => r.MiscellaneousAmount ?? 0);
//                var totalAmount = resultList.Sum(r => r.RowTotal ?? 0);

//                return new TellerWiseExpenseData
//                {
//                    Rows = resultList,
//                    TotalRecords = resultList.Count,
//                    TotalSavingWithdrawlAmount = totalSavingWithdrawl,
//                    TotalShareReturnAmount = totalShareReturn,
//                    TotalLoanIssueAmount = totalLoanIssue,
//                    TotalMiscellaneousAmount = totalMiscellaneous,
//                    TotalAmount = totalAmount,
//                    FromDateBs = request.FromDateBs,
//                    ToDateBs = request.ToDateBs,
//                    TellerId = request.TellerId,
//                    TellerName = tellerId == -1 ? "All Tellers" : (tellerName ?? "All Tellers"),
//                    OrderBy = request.OrderBy
//                };
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, "Error in GetReportDataAsync");
//                throw;
//            }
//        }

//        private async Task<string?> GetTellerNameByIdAsync(long tellerId)
//        {
//            try
//            {
//                var connectionString = _context.Database.GetConnectionString();
//                using var connection = new SqlConnection(connectionString);
//                await connection.OpenAsync();
//                const string sql = "SELECT FullName FROM UsmUser WHERE UsmUserId = @TellerId";
//                return await connection.ExecuteScalarAsync<string>(sql, new { TellerId = tellerId });
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, "Error in GetTellerNameByIdAsync");
//                return null;
//            }
//        }
//    }
//}







// Repository/MemberAccount/OthersReport/TellerWiseExpenseRepository.cs
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using NexgenCosysReport.DbContext;
using NexgenCosysReport.Dtos.RequestDtos.MemberAccount.OthersReport;
using NexgenCosysReport.Inteface.ServiceInterface.Common;
using NexgenCosysReport.Inteface.ServiceInterface.MemberAccount.OthersReport;
using System.Data;
using System.Text;

namespace NexgenCosysAPI.Repository.MemberAccount.OthersReport
{
    public class TellerExpenseRepository : ITellerExpense
    {
        private readonly AppDbContext _context;
        private readonly IDateConverterService _dateConverter;
        private readonly ILogger<TellerExpenseRepository> _logger;

        public TellerExpenseRepository(AppDbContext context, IDateConverterService dateConverter, ILogger<TellerExpenseRepository> logger)
        {
            _context = context;
            _dateConverter = dateConverter;
            _logger = logger;
        }

        public async Task<TellerWiseExpenseData> GetReportDataAsync(TellerWiseExpenseRequestDto request)
        {
            try
            {
                // Convert Nepali dates to English (ISO format avoids SQL Server regional/language ambiguity)
                var fromDateAd = await _dateConverter.NepaliToEnglishAsync(request.FromDateBs);
                var toDateAd = await _dateConverter.NepaliToEnglishAsync(request.ToDateBs);

                var fromDateStr = fromDateAd.ToString("yyyy-MM-dd");
                var toDateStr = toDateAd.ToString("yyyy-MM-dd");

                long tellerId = request.TellerId ?? -1;

                // ---- @SqlFilterExp — filters the AcoTransaction ("t") branch ----
                var sqlFilterExp = new StringBuilder();
                if (tellerId != -1)
                {
                    sqlFilterExp.Append(" And t.CreatedBy = ").Append(tellerId);
                }
                sqlFilterExp.Append(" And t.TransactionOn between '")
                            .Append(fromDateStr).Append("' And '")
                            .Append(toDateStr).Append("' ");

                // ---- @SqlFilterExpLoan — filters the LmtLoanIssue ("l") branch ----
                // Mirrors @SqlFilterExp but against the loan-issue table/alias, matching
                // the legacy BLL sample call in the SP header comment
                // (" And l.CreatedBy = 13 And l.LoanIssueOn=... ").
                var sqlFilterExpLoan = new StringBuilder();
                if (tellerId != -1)
                {
                    sqlFilterExpLoan.Append(" And l.CreatedBy = ").Append(tellerId);
                }
                sqlFilterExpLoan.Append(" And l.LoanIssueOn between '")
                                 .Append(fromDateStr).Append("' And '")
                                 .Append(toDateStr).Append("' ");

                // ---- @SqlFilterExpOrder — declared by the SP but never referenced in
                // its body; still required (no default), so pass an empty string. ----
                var sqlFilterExpOrder = string.Empty;

                // ---- @SqlFilterExpLoanOrder — the ORDER BY applied to the final
                // SELECT * FROM #TEMP, so it must reference #TEMP's own columns. ----
                var sqlFilterExpLoanOrder = new StringBuilder();
                switch (request.OrderBy)
                {
                    case "Account No":
                        sqlFilterExpLoanOrder.Append(" order by substring(Details, 1,(len(Details)-charindex('-', Details))-1), Details");
                        break;
                    case "Member Id":
                        sqlFilterExpLoanOrder.Append(" order by MemberIdFirst, MemberIdLast");
                        break;
                    case "Bill No":
                        sqlFilterExpLoanOrder.Append(" order by BillNo DESC");
                        break;
                    case "Savings Withdrawl":
                        sqlFilterExpLoanOrder.Append(" order by SavingWithdrawlAmount DESC");
                        break;
                    case "Share Return":
                        sqlFilterExpLoanOrder.Append(" order by ShareReturnAmount DESC");
                        break;
                    case "Loan Issue":
                        sqlFilterExpLoanOrder.Append(" order by LoanIssueAmount DESC");
                        break;
                    case "Misscellaneous Amt":
                        sqlFilterExpLoanOrder.Append(" order by MiscellaneousAmount DESC");
                        break;
                    default:
                        sqlFilterExpLoanOrder.Append(" order by MemberIdFirst, MemberIdLast");
                        break;
                }

                var connectionString = _context.Database.GetConnectionString();
                using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();

                // ---- Pass all 4 parameters the SP declares, with the exact names ----
                var parameters = new DynamicParameters();
                parameters.Add("@SqlFilterExp", sqlFilterExp.ToString(), DbType.String, size: -1);
                parameters.Add("@SqlFilterExpOrder", sqlFilterExpOrder, DbType.String, size: -1);
                parameters.Add("@SqlFilterExpLoan", sqlFilterExpLoan.ToString(), DbType.String, size: -1);
                parameters.Add("@SqlFilterExpLoanOrder", sqlFilterExpLoanOrder.ToString(), DbType.String, size: -1);

                var rows = await connection.QueryAsync<TellerWiseExpenseRowDto>(
                    "sp_5_43_GetTellerWiseExpense",
                    parameters,
                    commandType: CommandType.StoredProcedure
                );

                var resultList = rows.AsList();

                // Get teller name from the first row or from the database
                string? tellerName = resultList.FirstOrDefault()?.TellerName;
                if (tellerId != -1 && string.IsNullOrEmpty(tellerName))
                {
                    tellerName = await GetTellerNameByIdAsync(tellerId);
                }

                // Calculate totals from the result set
                var totalSavingWithdrawl = resultList.Sum(r => r.SavingWithdrawlAmount ?? 0);
                var totalShareReturn = resultList.Sum(r => r.ShareReturnAmount ?? 0);
                var totalLoanIssue = resultList.Sum(r => r.LoanIssueAmount ?? 0);
                var totalMiscellaneous = resultList.Sum(r => r.MiscellaneousAmount ?? 0);
                var totalAmount = resultList.Sum(r => r.RowTotal ?? 0);

                return new TellerWiseExpenseData
                {
                    Rows = resultList,
                    TotalRecords = resultList.Count,
                    TotalSavingWithdrawlAmount = totalSavingWithdrawl,
                    TotalShareReturnAmount = totalShareReturn,
                    TotalLoanIssueAmount = totalLoanIssue,
                    TotalMiscellaneousAmount = totalMiscellaneous,
                    TotalAmount = totalAmount,
                    FromDateBs = request.FromDateBs,
                    ToDateBs = request.ToDateBs,
                    TellerId = request.TellerId,
                    TellerName = tellerId == -1 ? "All Tellers" : (tellerName ?? "All Tellers"),
                    OrderBy = request.OrderBy
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetReportDataAsync");
                throw;
            }
        }

        private async Task<string?> GetTellerNameByIdAsync(long tellerId)
        {
            try
            {
                var connectionString = _context.Database.GetConnectionString();
                using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();
                const string sql = "SELECT FullName FROM UsmUser WHERE UsmUserId = @TellerId";
                return await connection.ExecuteScalarAsync<string>(sql, new { TellerId = tellerId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetTellerNameByIdAsync");
                return null;
            }
        }
    }
}