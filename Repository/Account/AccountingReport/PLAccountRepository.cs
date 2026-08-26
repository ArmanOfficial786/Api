////using Dapper;
using NexgenCosysReport.DbContext;
////using Microsoft.Data.SqlClient;
////using Microsoft.EntityFrameworkCore;
////using NexgenCosysReport.Dtos.RequestDtos.Account;
////using NexgenCosysReport.Inteface.ServiceInterface.Account;
////using NexgenCosysReport.Inteface.ServiceInterface.Common;
////using System.Data;

////namespace NexgenCosysReport.Repository.Account
////{
////    public class PLAccountRepository : IPLAccount
////    {
////        private readonly AppDbContext _context;
////        private readonly IDateConverterService _dateConverter;
////        private readonly ILogger<PLAccountRepository> _logger;

////        public PLAccountRepository(
////            AppDbContext context,
////            IDateConverterService dateConverter,
////            ILogger<PLAccountRepository> logger)
////        {
////            _context = context;
////            _dateConverter = dateConverter;
////            _logger = logger;
////        }

////        public async Task<object> GetPLAccountReport(PLAccountRequest request)
////        {
////            var connectionString = _context.Database.GetConnectionString();
////            using var connection = new SqlConnection(connectionString);
////            await connection.OpenAsync();

////            // Convert Nepali dates to English (MM/dd/yyyy)
////            var fromDateAd = await _dateConverter.NepaliToEnglishAsync(request.FromDate);
////            var toDateAd = await _dateConverter.NepaliToEnglishAsync(request.ToDate);
////            string fromDateStr = fromDateAd.ToString("MM/dd/yyyy");
////            string toDateStr = toDateAd.ToString("MM/dd/yyyy");

////            // Build filter strings (exactly as BLL)
////            string sqlFilterExp = $" And v.VoucherOn between '{fromDateStr}' And '{toDateStr}' ";
////            if (!request.SameCompanyName && !string.IsNullOrEmpty(request.BranchIds) && request.BranchIds != "-1")
////            {
////                sqlFilterExp += $" And v.UsmOfficeId in ({request.BranchIds})";
////            }

////            // Order by mapping
////            string orderByClause = MapOrderBy(request.OrderBy);
////            string sqlFilterExpOrderBy = $" order by {orderByClause}";

////            if (request.DisplayType == "Horizontal")
////            {
////                return await GetHorizontalReport(connection, sqlFilterExp, sqlFilterExpOrderBy, request.IsNepaliReport);
////            }
////            else // Vertical
////            {
////                return await GetVerticalReport(connection, sqlFilterExp, sqlFilterExpOrderBy, request.IsNepaliReport);
////            }
////        }

////        private async Task<PLAccountHorizontalData> GetHorizontalReport(
////            SqlConnection connection,
////            string sqlFilterExp,
////            string sqlFilterExpOrderBy,
////            bool isNepali)
////        {
////            // Choose SPs based on language
////            string incomeSp = isNepali ? "sp_6_56_GetNepaliPLAccountIncome" : "sp_6_56_GetPLAccountIncome";
////            string expenseSp = isNepali ? "sp_6_56_GetNepaliPLAccountExpense" : "sp_6_56_GetPLAccountExpense";

////            // ---- Income ----
////            var incomeParams = new DynamicParameters();
////            incomeParams.Add("@SqlFilterExp", sqlFilterExp);
////            incomeParams.Add("@SqlFilterExpOrderBy", sqlFilterExpOrderBy);
////            incomeParams.Add("@incomeBalance", dbType: DbType.Decimal, direction: ParameterDirection.Output, precision: 18, scale: 2);

////            var incomeRows = await connection.QueryAsync<PLAccountRowDto>(
////                incomeSp,
////                incomeParams,
////                commandType: CommandType.StoredProcedure
////            );

////            decimal totalIncome = incomeParams.Get<decimal?>("@incomeBalance") ?? 0;

////            // ---- Expense ----
////            var expenseParams = new DynamicParameters();
////            expenseParams.Add("@SqlFilterExp", sqlFilterExp);
////            expenseParams.Add("@SqlFilterExpOrderBy", sqlFilterExpOrderBy);
////            expenseParams.Add("@expenseBalance", dbType: DbType.Decimal, direction: ParameterDirection.Output, precision: 18, scale: 2);

////            var expenseRows = await connection.QueryAsync<PLAccountRowDto>(
////                expenseSp,
////                expenseParams,
////                commandType: CommandType.StoredProcedure
////            );

////            decimal totalExpense = expenseParams.Get<decimal?>("@expenseBalance") ?? 0;

////            return new PLAccountHorizontalData
////            {
////                IncomeRows = incomeRows.AsList(),
////                ExpenseRows = expenseRows.AsList(),
////                TotalIncome = totalIncome,
////                TotalExpense = totalExpense
////            };
////        }

////        private async Task<PLAccountVerticalData> GetVerticalReport(
////            SqlConnection connection,
////            string sqlFilterExp,
////            string sqlFilterExpOrderBy,
////            bool isNepali)
////        {
////            string spName = isNepali ? "sp_6_56_GetPLAccountReportNepali" : "sp_6_56_GetPLAccountReport";

////            var parameters = new DynamicParameters();
////            parameters.Add("@SqlFilterExp", sqlFilterExp);
////            parameters.Add("@SqlFilterExpOrderBy", sqlFilterExpOrderBy);

////            var rows = await connection.QueryAsync<PLAccountRowDto>(
////                spName,
////                parameters,
////                commandType: CommandType.StoredProcedure
////            );

////            var list = rows.AsList();
////            decimal totalIncome = list.Where(r => r.LedgerHead?.Equals("INCOME", StringComparison.OrdinalIgnoreCase) == true).Sum(r => r.Balance);
////            decimal totalExpense = list.Where(r => r.LedgerHead?.Equals("EXPENSES", StringComparison.OrdinalIgnoreCase) == true).Sum(r => r.Balance);

////            return new PLAccountVerticalData
////            {
////                Rows = list,
////                TotalIncome = totalIncome,
////                TotalExpense = totalExpense
////            };
////        }

////        private string MapOrderBy(string orderBy)
////        {
////            return orderBy switch
////            {
////                "Balance" => "Amount DESC",
////                _ => "SubLedger"   // Ledger Name
////            };
////        }
////    }
////}







//// Repository/AccountOperation/PLAccountRepository.cs
//using Dapper;
//using Microsoft.Data.SqlClient;
//using Microsoft.EntityFrameworkCore;
//using NexgenCosysReport.Dtos.RequestDtos.Account;
//using NexgenCosysReport.Inteface.ServiceInterface.Account;
//using NexgenCosysReport.Inteface.ServiceInterface.Common;
//using System.Data;

//namespace NexgenCosysReport.Repository.Account
//{
//    public class PLAccountRepository : IPLAccount
//    {
//        private readonly AppDbContext _context;
//        private readonly IDateConverterService _dateConverter;
//        private readonly ILogger<PLAccountRepository> _logger;

//        public PLAccountRepository(
//            AppDbContext context,
//            IDateConverterService dateConverter,
//            ILogger<PLAccountRepository> logger)
//        {
//            _context = context;
//            _dateConverter = dateConverter;
//            _logger = logger;
//        }

//        public async Task<object> GetPLAccountReport(PLAccountRequest request)
//        {
//            var connectionString = _context.Database.GetConnectionString();
//            using var connection = new SqlConnection(connectionString);
//            await connection.OpenAsync();

//            // Convert Nepali dates to English (MM/dd/yyyy)
//            var fromDateAd = await _dateConverter.NepaliToEnglishAsync(request.FromDate);
//            var toDateAd = await _dateConverter.NepaliToEnglishAsync(request.ToDate);
//            string fromDateStr = fromDateAd.ToString("MM/dd/yyyy");
//            string toDateStr = toDateAd.ToString("MM/dd/yyyy");

//            // Build filter strings
//            string sqlFilterExp = $" And v.VoucherOn between '{fromDateStr}' And '{toDateStr}' ";
//            if (!request.SameCompanyName && !string.IsNullOrEmpty(request.BranchIds) && request.BranchIds != "-1")
//            {
//                sqlFilterExp += $" And v.UsmOfficeId in ({request.BranchIds})";
//            }

//            string orderByClause = MapOrderBy(request.OrderBy);
//            string sqlFilterExpOrderBy = $" order by {orderByClause}";

//            if (request.DisplayType == "Horizontal")
//            {
//                return await GetHorizontalReport(connection, sqlFilterExp, sqlFilterExpOrderBy, request.IsNepaliReport);
//            }
//            else // Vertical
//            {
//                return await GetVerticalReport(connection, sqlFilterExp, sqlFilterExpOrderBy, request.IsNepaliReport);
//            }
//        }

//        private async Task<PLAccountHorizontalData> GetHorizontalReport(
//            SqlConnection connection,
//            string sqlFilterExp,
//            string sqlFilterExpOrderBy,
//            bool isNepali)
//        {
//            string incomeSp = isNepali ? "sp_6_56_GetNepaliPLAccountIncome" : "sp_6_56_GetPLAccountIncome";
//            string expenseSp = isNepali ? "sp_6_56_GetNepaliPLAccountExpense" : "sp_6_56_GetPLAccountExpense";

//            var parameters = new DynamicParameters();
//            parameters.Add("@SqlFilterExp", sqlFilterExp);
//            parameters.Add("@SqlFilterExpOrderBy", sqlFilterExpOrderBy);

//            // Output parameters
//            var incomeTotalParam = new SqlParameter("@incomeBalance", SqlDbType.Decimal) { Direction = ParameterDirection.Output, Precision = 18, Scale = 2 };
//            var expenseTotalParam = new SqlParameter("@expenseBalance", SqlDbType.Decimal) { Direction = ParameterDirection.Output, Precision = 18, Scale = 2 };

//            // Income
//            var incomeParams = new DynamicParameters(parameters);
//            incomeParams.Add("@incomeBalance", incomeTotalParam, dbType: DbType.Decimal, direction: ParameterDirection.Output);
//            var incomeRows = await connection.QueryAsync<PLAccountRowDto>(incomeSp, incomeParams, commandType: CommandType.StoredProcedure);

//            // Expense
//            var expenseParams = new DynamicParameters(parameters);
//            expenseParams.Add("@expenseBalance", expenseTotalParam, dbType: DbType.Decimal, direction: ParameterDirection.Output);
//            var expenseRows = await connection.QueryAsync<PLAccountRowDto>(expenseSp, expenseParams, commandType: CommandType.StoredProcedure);

//            decimal totalIncome = incomeTotalParam.Value != DBNull.Value ? Convert.ToDecimal(incomeTotalParam.Value) : 0;
//            decimal totalExpense = expenseTotalParam.Value != DBNull.Value ? Convert.ToDecimal(expenseTotalParam.Value) : 0;

//            return new PLAccountHorizontalData
//            {
//                IncomeRows = incomeRows.AsList(),
//                ExpenseRows = expenseRows.AsList(),
//                TotalIncome = totalIncome,
//                TotalExpense = totalExpense
//            };
//        }

//        private async Task<PLAccountVerticalData> GetVerticalReport(
//            SqlConnection connection,
//            string sqlFilterExp,
//            string sqlFilterExpOrderBy,
//            bool isNepali)
//        {
//            string spName = isNepali ? "sp_6_56_GetPLAccountReportNepali" : "sp_6_56_GetPLAccountReport";

//            var parameters = new DynamicParameters();
//            parameters.Add("@SqlFilterExp", sqlFilterExp);
//            parameters.Add("@SqlFilterExpOrderBy", sqlFilterExpOrderBy);

//            // The vertical SP returns columns: LedgerHead, MainLedger, SubLedger, SubLedger1, SubLedger2, Amount
//            // We'll map Amount to Balance in PLAccountRowDto
//            var rows = await connection.QueryAsync<dynamic>(spName, parameters, commandType: CommandType.StoredProcedure);

//            var mappedRows = rows.Select(r => new PLAccountRowDto
//            {
//                LedgerHead = r.LedgerHead,
//                MainLedger = r.MainLedger,
//                SubLedger = r.SubLedger,
//                SubLedger1 = r.SubLedger1,
//                SubLedger2 = r.SubLedger2,
//                DebitAmount = r.DebitAmount != null ? Convert.ToDecimal(r.DebitAmount) : 0,
//                CreditAmount = r.CreditAmount != null ? Convert.ToDecimal(r.CreditAmount) : 0,
//                Balance = r.Amount != null ? Convert.ToDecimal(r.Amount) : 0   // Map Amount to Balance
//            }).ToList();

//            decimal totalIncome = mappedRows.Where(r => r.LedgerHead?.Equals("INCOME", StringComparison.OrdinalIgnoreCase) == true).Sum(r => r.Balance);
//            decimal totalExpense = mappedRows.Where(r => r.LedgerHead?.Equals("EXPENSES", StringComparison.OrdinalIgnoreCase) == true).Sum(r => r.Balance);

//            return new PLAccountVerticalData
//            {
//                Rows = mappedRows,
//                TotalIncome = totalIncome,
//                TotalExpense = totalExpense,
//                NetProfit = totalIncome - totalExpense
//            };
//        }

//        private string MapOrderBy(string orderBy)
//        {
//            return orderBy switch
//            {
//                "Balance" => "Amount DESC",
//                _ => "SubLedger"   // Ledger Name
//            };
//        }
//    }
//}








using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using NexgenCosysReport.Inteface.ServiceInterface.Common;
using System.Data;
using NexgenCosysReport.Dtos.RequestDtos.Account.AccountingReport;
using NexgenCosysReport.Inteface.ServiceInterface.Account.AccountingReport;

namespace NexgenCosysReport.Repository.Account.AccountingReport
{
    public class PLAccountRepository : IPLAccount
    {
        private readonly AppDbContext _context;
        private readonly IDateConverterService _dateConverter;
        private readonly ILogger<PLAccountRepository> _logger;

        public PLAccountRepository(
            AppDbContext context,
            IDateConverterService dateConverter,
            ILogger<PLAccountRepository> logger)
        {
            _context = context;
            _dateConverter = dateConverter;
            _logger = logger;
        }

        public async Task<object> GetPLAccountReport(PLAccountRequest request)
        {
            var connectionString = _context.Database.GetConnectionString();
            using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();

            // Convert Nepali dates to English (MM/dd/yyyy) - exactly as BLL
            var fromDateAd = await _dateConverter.NepaliToEnglishAsync(request.FromDate);
            var toDateAd = await _dateConverter.NepaliToEnglishAsync(request.ToDate);
            string fromDateStr = fromDateAd.ToString("MM/dd/yyyy");
            string toDateStr = toDateAd.ToString("MM/dd/yyyy");

            // Build filter strings
            string sqlFilterExp = $" And v.VoucherOn between '{fromDateStr}' And '{toDateStr}' ";
            if (!request.SameCompanyName && !string.IsNullOrEmpty(request.BranchIds) && request.BranchIds != "-1")
            {
                sqlFilterExp += $" And v.UsmOfficeId in ({request.BranchIds})";
            }

            string orderByClause = MapOrderBy(request.OrderBy);
            string sqlFilterExpOrderBy = $" order by {orderByClause}";

            if (request.DisplayType == "Horizontal")
            {
                return await GetHorizontalReport(connection, sqlFilterExp, sqlFilterExpOrderBy, request.IsNepaliReport);
            }
            else // Vertical
            {
                return await GetVerticalReport(connection, sqlFilterExp, sqlFilterExpOrderBy, request.IsNepaliReport);
            }
        }

        private async Task<PLAccountHorizontalData> GetHorizontalReport(
            SqlConnection connection,
            string sqlFilterExp,
            string sqlFilterExpOrderBy,
            bool isNepali)
        {
            string incomeSp = isNepali ? "sp_6_56_GetNepaliPLAccountIncome" : "sp_6_56_GetPLAccountIncome";
            string expenseSp = isNepali ? "sp_6_56_GetNepaliPLAccountExpense" : "sp_6_56_GetPLAccountExpense";

            // ---- Income SP ----
            var incomeParams = new DynamicParameters();
            incomeParams.Add("@SqlFilterExp", sqlFilterExp);
            incomeParams.Add("@SqlFilterExpOrderBy", sqlFilterExpOrderBy);
            incomeParams.Add("@incomeBalance", dbType: DbType.Decimal, direction: ParameterDirection.Output, precision: 18, scale: 2);

            var incomeRows = await connection.QueryAsync<PLAccountRowDto>(
                incomeSp,
                incomeParams,
                commandType: CommandType.StoredProcedure
            );

            decimal totalIncome = incomeParams.Get<decimal?>("@incomeBalance") ?? 0;

            // ---- Expense SP ----
            var expenseParams = new DynamicParameters();
            expenseParams.Add("@SqlFilterExp", sqlFilterExp);
            expenseParams.Add("@SqlFilterExpOrderBy", sqlFilterExpOrderBy);
            expenseParams.Add("@expenseBalance", dbType: DbType.Decimal, direction: ParameterDirection.Output, precision: 18, scale: 2);

            var expenseRows = await connection.QueryAsync<PLAccountRowDto>(
                expenseSp,
                expenseParams,
                commandType: CommandType.StoredProcedure
            );

            decimal totalExpense = expenseParams.Get<decimal?>("@expenseBalance") ?? 0;

            return new PLAccountHorizontalData
            {
                IncomeRows = incomeRows.AsList(),
                ExpenseRows = expenseRows.AsList(),
                TotalIncome = totalIncome,
                TotalExpense = totalExpense
            };
        }

        private async Task<PLAccountVerticalData> GetVerticalReport(
            SqlConnection connection,
            string sqlFilterExp,
            string sqlFilterExpOrderBy,
            bool isNepali)
        {
            string spName = isNepali ? "sp_6_56_GetPLAccountReportNepali" : "sp_6_56_GetPLAccountReport";

            var parameters = new DynamicParameters();
            parameters.Add("@SqlFilterExp", sqlFilterExp);
            parameters.Add("@SqlFilterExpOrderBy", sqlFilterExpOrderBy);

            // Vertical SP returns Amount column; we'll map it to Balance
            var rows = await connection.QueryAsync<dynamic>(
                spName,
                parameters,
                commandType: CommandType.StoredProcedure
            );

            var mappedRows = rows.Select(r => new PLAccountRowDto
            {
                LedgerHead = r.LedgerHead,
                MainLedger = r.MainLedger,
                SubLedger = r.SubLedger,
                SubLedger1 = r.SubLedger1,
                SubLedger2 = r.SubLedger2,
                DebitAmount = r.DebitAmount != null ? Convert.ToDecimal(r.DebitAmount) : 0,
                CreditAmount = r.CreditAmount != null ? Convert.ToDecimal(r.CreditAmount) : 0,
                Balance = r.Amount != null ? Convert.ToDecimal(r.Amount) : 0
            }).ToList();

            decimal totalIncome = mappedRows
                .Where(r => r.LedgerHead?.Equals("INCOME", StringComparison.OrdinalIgnoreCase) == true)
                .Sum(r => r.Balance);
            decimal totalExpense = mappedRows
                .Where(r => r.LedgerHead?.Equals("EXPENSES", StringComparison.OrdinalIgnoreCase) == true)
                .Sum(r => r.Balance);

            return new PLAccountVerticalData
            {
                Rows = mappedRows,
                TotalIncome = totalIncome,
                TotalExpense = totalExpense,
                NetProfit = totalIncome - totalExpense
            };
        }

        private string MapOrderBy(string orderBy)
        {
            return orderBy switch
            {
                "Balance" => "Amount DESC",
                _ => "SubLedger"   // Ledger Name
            };
        }
    }
}
