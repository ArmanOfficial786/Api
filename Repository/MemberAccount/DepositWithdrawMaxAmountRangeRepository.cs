////// Repository/AccountOperation/DepositWithdrawMaxAmountRangeRepository.cs
////using Dapper;
////using Microsoft.Data.SqlClient;
////using Microsoft.EntityFrameworkCore;
////using NexgenCosysReport.Dtos.RequestDtos.MemberAccount;
////using NexgenCosysReport.Inteface.ServiceInterface.Common;
////using NexgenCosysReport.Inteface.ServiceInterface.MemberAccount;
////using System.Data;

////namespace NexgenCosysReport.Repository.MemberAccount
////{
////    public class DepositWithdrawMaxAmountRangeRepository : IDepositWithdrawMaxAmountRange
////    {
////        private readonly AppDbContext _context;
////        private readonly IDateConverterService _dateConverter;
////        private readonly ILogger<DepositWithdrawMaxAmountRangeRepository> _logger;

////        public DepositWithdrawMaxAmountRangeRepository(
////            AppDbContext context,
////            IDateConverterService dateConverter,
////            ILogger<DepositWithdrawMaxAmountRangeRepository> logger)
////        {
////            _context = context;
////            _dateConverter = dateConverter;
////            _logger = logger;
////        }

////        public async Task<DepositWithdrawMaxAmountData> GetDepositWithdrawMaxAmountRange(DepositWithdrawMaxAmountRangeRequest request)
////        {
////            var connectionString = _context.Database.GetConnectionString();
////            using var connection = new SqlConnection(connectionString);
////            await connection.OpenAsync();

////            // --- Office filter --- 
////            // Original webform: officeName can be "-1" for all or comma-separated ids
////            var sqlOfficeFilter = string.Empty;
////            if (request.BranchIds != "-1" && !string.IsNullOrEmpty(request.BranchIds))
////            {
////                sqlOfficeFilter = "AND AT.UsmOfficeId in (" + request.BranchIds + ")";
////            }

////            // --- Amount filter ---
////            // Original webform: amount is passed as string with comma formatting like "1,00,000.00"
////            // We need to clean it and convert to decimal
////            var amountValue = request.Amount;

////            // Original webform logic:
////            // rdbutton == "1" -> Deposit >= amount
////            // rdbutton == "2" -> Withdraw >= amount
////            // rdbutton == "3" -> Both (Deposit >= amount OR Withdraw >= amount)
////            var sqlAmountFilter = request.TransactionType switch
////            {
////                1 => $" And DepositAmount >= {amountValue.ToString(System.Globalization.CultureInfo.InvariantCulture)}",
////                2 => $" And WithdrawAmount >= {amountValue.ToString(System.Globalization.CultureInfo.InvariantCulture)}",
////                3 => $" And (DepositAmount >= {amountValue.ToString(System.Globalization.CultureInfo.InvariantCulture)} OR WithdrawAmount >= {amountValue.ToString(System.Globalization.CultureInfo.InvariantCulture)})",
////                _ => $" And DepositAmount >= {amountValue.ToString(System.Globalization.CultureInfo.InvariantCulture)}"
////            };

////            // --- Date filter --- 
////            // Original webform: fromDateEng and toDateEng are already in English format
////            var fromDateAd = await _dateConverter.NepaliToEnglishAsync(request.FromDate);
////            var toDateAd = await _dateConverter.NepaliToEnglishAsync(request.ToDate);
////            var fromDateStr = fromDateAd.ToString("MM/dd/yyyy");
////            var toDateStr = toDateAd.ToString("MM/dd/yyyy");
////            var sqlDateFilter = $" And AT.TransactionOn between '{fromDateStr}' AND '{toDateStr}' ";

////            // --- Order by ---
////            // Original webform: orderBy values: MemberId, MemberName, Deposit, Withdraw, Account, Date
////            var sqlFilterExpOrderBy = string.Empty;
////            if (!string.IsNullOrEmpty(request.OrderBy) && request.OrderBy != "-1")
////            {
////                sqlFilterExpOrderBy = request.OrderBy switch
////                {
////                    "MemberId" => " order by substring(MemberId, 1,(len(MemberId)-charindex('-', MemberId))-1), MemberId ",
////                    "MemberName" => " order by MemberName",
////                    "Deposit" => " order by DepositAmount DESC",
////                    "Withdraw" => " order by WithdrawAmount DESC",
////                    "Account" => " order by AccountNo",
////                    "Date" => " order by TransactionDate",
////                    _ => string.Empty
////                };
////            }

////            _logger.LogInformation(
////                "DepositWithdrawMaxAmountRange SP params -> OfficeFilter: [{OfficeFilter}] DateFilter: [{DateFilter}] AmountFilter: [{AmountFilter}] OrderBy: [{OrderBy}]",
////                sqlOfficeFilter, sqlDateFilter, sqlAmountFilter, sqlFilterExpOrderBy);

////            var parameters = new DynamicParameters();
////            parameters.Add("@SqlOfficeFilter", sqlOfficeFilter);
////            parameters.Add("@SqlDateFilter", sqlDateFilter);
////            parameters.Add("@SqlAmountFilter", sqlAmountFilter);
////            parameters.Add("@SqlFilterExpOrderBy", sqlFilterExpOrderBy);

////            var rawRows = await connection.QueryAsync(
////                "sp_5_43_GetDepositWithdrawMaximumAmountRange",
////                parameters,
////                commandType: CommandType.StoredProcedure
////            );

////            var list = new List<DepositWithdrawMaxAmountRowDto>();
////            foreach (var r in rawRows)
////            {
////                var dict = (IDictionary<string, object>)r;
////                list.Add(new DepositWithdrawMaxAmountRowDto
////                {
////                    MemberId = GetString(dict, "MemberId"),
////                    MemberName = GetString(dict, "MemberName"),
////                    AccountNo = GetString(dict, "AccountNo"),
////                    TransactionDate = GetDateTime(dict, "TransactionDate") ?? GetDateTime(dict, "Date"),
////                    TransactionDateBs = GetString(dict, "TransactionDateBs"),
////                    DepositAmount = GetDecimal(dict, "DepositAmount"),
////                    WithdrawAmount = GetDecimal(dict, "WithdrawAmount"),
////                    Particulars = GetString(dict, "Particulars") ?? GetString(dict, "Narration")
////                });
////            }

////            _logger.LogInformation("DepositWithdrawMaxAmountRange returned {Count} rows", list.Count);

////            return new DepositWithdrawMaxAmountData
////            {
////                Rows = list,
////                TotalRecords = list.Count,
////                TotalDeposit = list.Sum(x => x.DepositAmount),
////                TotalWithdraw = list.Sum(x => x.WithdrawAmount)
////            };
////        }

////        private static string? GetString(IDictionary<string, object> dict, string key) =>
////            dict.TryGetValue(key, out var val) && val != DBNull.Value ? val?.ToString() : null;

////        private static decimal GetDecimal(IDictionary<string, object> dict, string key) =>
////            dict.TryGetValue(key, out var val) && val != DBNull.Value ? Convert.ToDecimal(val) : 0m;

////        private static DateTime? GetDateTime(IDictionary<string, object> dict, string key) =>
////            dict.TryGetValue(key, out var val) && val != DBNull.Value ? Convert.ToDateTime(val) : null;
////    }
////}




//// Repository/AccountOperation/DepositWithdrawMaxAmountRangeRepository.cs
//using Dapper;
//using Microsoft.Data.SqlClient;
//using Microsoft.EntityFrameworkCore;
//using NexgenCosysReport.Dtos.RequestDtos.MemberAccount;
//using NexgenCosysReport.Inteface.ServiceInterface.Common;
//using NexgenCosysReport.Inteface.ServiceInterface.MemberAccount;
//using System.Data;

//namespace NexgenCosysReport.Repository.MemberAccount
//{
//    public class DepositWithdrawMaxAmountRangeRepository : IDepositWithdrawMaxAmountRange
//    {
//        private readonly AppDbContext _context;
//        private readonly Inteface.ServiceInterface.Common.IDateConverterService _dateConverter;
//        private readonly ILogger<DepositWithdrawMaxAmountRangeRepository> _logger;

//        public DepositWithdrawMaxAmountRangeRepository(
//            AppDbContext context,
//            IDateConverterService dateConverter,
//            ILogger<DepositWithdrawMaxAmountRangeRepository> logger)
//        {
//            _context = context;
//            _dateConverter = dateConverter;
//            _logger = logger;
//        }

//        public async Task<DepositWithdrawMaxAmountData> GetDepositWithdrawMaxAmountRange(DepositWithdrawMaxAmountRangeRequest request)
//        {
//            var connectionString = _context.Database.GetConnectionString();
//            using var connection = new SqlConnection(connectionString);
//            await connection.OpenAsync();

//            // --- Date filter --- 
//            // The date converter should handle Nepali to English conversion
//            // We need to handle potential invalid dates gracefully
//            string fromDateStr = string.Empty;
//            string toDateStr = string.Empty;

//            try
//            {
//                // Check if dates are valid and convert
//                if (!string.IsNullOrEmpty(request.FromDate) && request.FromDate != "-1")
//                {
//                    var fromDateAd = await _dateConverter.NepaliToEnglishAsync(request.FromDate);
//                    fromDateStr = fromDateAd.ToString("MM/dd/yyyy");
//                }
//                else
//                {
//                    // Default to a date if not provided
//                    fromDateStr = DateTime.Now.AddMonths(-6).ToString("MM/dd/yyyy");
//                }

//                if (!string.IsNullOrEmpty(request.ToDate) && request.ToDate != "-1")
//                {
//                    var toDateAd = await _dateConverter.NepaliToEnglishAsync(request.ToDate);
//                    toDateStr = toDateAd.ToString("MM/dd/yyyy");
//                }
//                else
//                {
//                    // Default to today if not provided
//                    toDateStr = DateTime.Now.ToString("MM/dd/yyyy");
//                }
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, "Date conversion failed. FromDate: {FromDate}, ToDate: {ToDate}",
//                    request.FromDate, request.ToDate);

//                // Fallback to default dates if conversion fails
//                fromDateStr = DateTime.Now.AddMonths(-6).ToString("MM/dd/yyyy");
//                toDateStr = DateTime.Now.ToString("MM/dd/yyyy");
//            }

//            var sqlDateFilter = $" And AT.TransactionOn between '{fromDateStr}' AND '{toDateStr}' ";

//            // --- Office filter --- 
//            var sqlOfficeFilter = string.Empty;
//            if (request.BranchIds != "-1" && !string.IsNullOrEmpty(request.BranchIds))
//            {
//                sqlOfficeFilter = "AND AT.UsmOfficeId in (" + request.BranchIds + ")";
//            }

//            // --- Amount filter ---
//            var amountValue = request.Amount;

//            var sqlAmountFilter = request.TransactionType switch
//            {
//                1 => $" And DepositAmount >= {amountValue.ToString(System.Globalization.CultureInfo.InvariantCulture)}",
//                2 => $" And WithdrawAmount >= {amountValue.ToString(System.Globalization.CultureInfo.InvariantCulture)}",
//                3 => $" And (DepositAmount >= {amountValue.ToString(System.Globalization.CultureInfo.InvariantCulture)} OR WithdrawAmount >= {amountValue.ToString(System.Globalization.CultureInfo.InvariantCulture)})",
//                _ => $" And DepositAmount >= {amountValue.ToString(System.Globalization.CultureInfo.InvariantCulture)}"
//            };

//            // --- Order by ---
//            var sqlFilterExpOrderBy = string.Empty;
//            if (!string.IsNullOrEmpty(request.OrderBy) && request.OrderBy != "-1")
//            {
//                sqlFilterExpOrderBy = request.OrderBy switch
//                {
//                    "MemberId" => " order by substring(MemberId, 1,(len(MemberId)-charindex('-', MemberId))-1), MemberId ",
//                    "MemberName" => " order by MemberName",
//                    "Deposit" => " order by DepositAmount DESC",
//                    "Withdraw" => " order by WithdrawAmount DESC",
//                    "Account" => " order by AccountNo",
//                    "Date" => " order by TransactionDate",
//                    _ => string.Empty
//                };
//            }

//            _logger.LogInformation(
//                "DepositWithdrawMaxAmountRange SP params -> OfficeFilter: [{OfficeFilter}] DateFilter: [{DateFilter}] AmountFilter: [{AmountFilter}] OrderBy: [{OrderBy}]",
//                sqlOfficeFilter, sqlDateFilter, sqlAmountFilter, sqlFilterExpOrderBy);

//            var parameters = new DynamicParameters();
//            parameters.Add("@SqlOfficeFilter", sqlOfficeFilter);
//            parameters.Add("@SqlDateFilter", sqlDateFilter);
//            parameters.Add("@SqlAmountFilter", sqlAmountFilter);
//            parameters.Add("@SqlFilterExpOrderBy", sqlFilterExpOrderBy);

//            var rawRows = await connection.QueryAsync(
//                "sp_5_43_GetDepositWithdrawMaximumAmountRange",
//                parameters,
//                commandType: CommandType.StoredProcedure
//            );

//            var list = new List<DepositWithdrawMaxAmountRowDto>();
//            foreach (var r in rawRows)
//            {
//                var dict = (IDictionary<string, object>)r;
//                var row = new DepositWithdrawMaxAmountRowDto
//                {
//                    MemberId = GetString(dict, "MemberId"),
//                    MemberName = GetString(dict, "MemberName"),
//                    AccountNo = GetString(dict, "AccountNo"),
//                    TransactionDate = GetDateTime(dict, "TransactionDate") ?? GetDateTime(dict, "Date"),
//                    TransactionDateBs = GetString(dict, "TransactionDateBs"),
//                    DepositAmount = GetDecimal(dict, "DepositAmount"),
//                    WithdrawAmount = GetDecimal(dict, "WithdrawAmount"),
//                    Particulars = GetString(dict, "Particulars") ?? GetString(dict, "Narration") ?? GetString(dict, "Description")
//                };
//                list.Add(row);
//            }

//            _logger.LogInformation("DepositWithdrawMaxAmountRange returned {Count} rows", list.Count);

//            return new DepositWithdrawMaxAmountData
//            {
//                Rows = list,
//                TotalRecords = list.Count,
//                TotalDeposit = list.Sum(x => x.DepositAmount),
//                TotalWithdraw = list.Sum(x => x.WithdrawAmount)
//            };
//        }

//        private static string? GetString(IDictionary<string, object> dict, string key) =>
//            dict.TryGetValue(key, out var val) && val != DBNull.Value ? val?.ToString() : null;

//        private static decimal GetDecimal(IDictionary<string, object> dict, string key) =>
//            dict.TryGetValue(key, out var val) && val != DBNull.Value ? Convert.ToDecimal(val) : 0m;

//        private static DateTime? GetDateTime(IDictionary<string, object> dict, string key) =>
//            dict.TryGetValue(key, out var val) && val != DBNull.Value ? Convert.ToDateTime(val) : null;
//    }
//}







// Repository/AccountOperation/DepositWithdrawMaxAmountRangeRepository.cs
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using NexgenCosysReport.Dtos.RequestDtos.MemberAccount;
using NexgenCosysReport.Inteface.ServiceInterface.Common;
using NexgenCosysReport.Inteface.ServiceInterface.MemberAccount;
using System.Data;
using System.Globalization;

namespace NexgenCosysReport.Repository.MemberAccount
{
    public class DepositWithdrawMaxAmountRangeRepository : IDepositWithdrawMaxAmountRange
    {
        private readonly AppDbContext _context;
        private readonly IDateConverterService _dateConverter;
        private readonly ILogger<DepositWithdrawMaxAmountRangeRepository> _logger;

        public DepositWithdrawMaxAmountRangeRepository(
            AppDbContext context,
            IDateConverterService dateConverter,
            ILogger<DepositWithdrawMaxAmountRangeRepository> logger)
        {
            _context = context;
            _dateConverter = dateConverter;
            _logger = logger;
        }

        public async Task<DepositWithdrawMaxAmountData> GetDepositWithdrawMaxAmountRange(DepositWithdrawMaxAmountRangeRequest request)
        {
            var connectionString = _context.Database.GetConnectionString();
            using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();

            // --- Date filter ---
            // NepaliToEnglishAsync already tolerates "yyyy/MM/dd" or "yyyy-MM-dd" and
            // returns a fallback DateTime instead of throwing on an unmapped BS date,
            // so we just guard for empty/"-1" inputs here.
            string fromDateStr;
            string toDateStr;

            try
            {
                if (!string.IsNullOrEmpty(request.FromDate) && request.FromDate != "-1")
                {
                    var fromDateAd = await _dateConverter.NepaliToEnglishAsync(request.FromDate);
                    fromDateStr = fromDateAd.ToString("MM/dd/yyyy");
                }
                else
                {
                    fromDateStr = DateTime.Now.AddMonths(-6).ToString("MM/dd/yyyy");
                }

                if (!string.IsNullOrEmpty(request.ToDate) && request.ToDate != "-1")
                {
                    var toDateAd = await _dateConverter.NepaliToEnglishAsync(request.ToDate);
                    toDateStr = toDateAd.ToString("MM/dd/yyyy");
                }
                else
                {
                    toDateStr = DateTime.Now.ToString("MM/dd/yyyy");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Date conversion failed. FromDate: {FromDate}, ToDate: {ToDate}",
                    request.FromDate, request.ToDate);

                fromDateStr = DateTime.Now.AddMonths(-6).ToString("MM/dd/yyyy");
                toDateStr = DateTime.Now.ToString("MM/dd/yyyy");
            }

            var sqlDateFilter = $" And AT.TransactionOn between '{fromDateStr}' AND '{toDateStr}' ";

            // --- Office filter ---
            var sqlOfficeFilter = string.Empty;
            if (request.BranchIds != "-1" && !string.IsNullOrEmpty(request.BranchIds))
            {
                sqlOfficeFilter = "AND AT.UsmOfficeId in (" + request.BranchIds + ")";
            }

            // --- Amount filter ---
            var amountValue = request.Amount;

            var sqlAmountFilter = request.TransactionType switch
            {
                1 => $" And DepositAmount >= {amountValue.ToString(CultureInfo.InvariantCulture)}",
                2 => $" And WithdrawAmount >= {amountValue.ToString(CultureInfo.InvariantCulture)}",
                3 => $" And (DepositAmount >= {amountValue.ToString(CultureInfo.InvariantCulture)} OR WithdrawAmount >= {amountValue.ToString(CultureInfo.InvariantCulture)})",
                _ => $" And DepositAmount >= {amountValue.ToString(CultureInfo.InvariantCulture)}"
            };

            // --- Order by ---
            var sqlFilterExpOrderBy = string.Empty;
            if (!string.IsNullOrEmpty(request.OrderBy) && request.OrderBy != "-1")
            {
                sqlFilterExpOrderBy = request.OrderBy switch
                {
                    "MemberId" => " order by substring(MemberId, 1,(len(MemberId)-charindex('-', MemberId))-1), MemberId ",
                    "MemberName" => " order by MemberName",
                    "Deposit" => " order by DepositAmount DESC",
                    "Withdraw" => " order by WithdrawAmount DESC",
                    "Account" => " order by AccountNo",
                    "Date" => " order by TransactionDate",
                    _ => string.Empty
                };
            }

            _logger.LogInformation(
                "DepositWithdrawMaxAmountRange SP params -> OfficeFilter: [{OfficeFilter}] DateFilter: [{DateFilter}] AmountFilter: [{AmountFilter}] OrderBy: [{OrderBy}]",
                sqlOfficeFilter, sqlDateFilter, sqlAmountFilter, sqlFilterExpOrderBy);

            var parameters = new DynamicParameters();
            parameters.Add("@SqlOfficeFilter", sqlOfficeFilter);
            parameters.Add("@SqlDateFilter", sqlDateFilter);
            parameters.Add("@SqlAmountFilter", sqlAmountFilter);
            parameters.Add("@SqlFilterExpOrderBy", sqlFilterExpOrderBy);

            var rawRows = await connection.QueryAsync(
                "sp_5_43_GetDepositWithdrawMaximumAmountRange",
                parameters,
                commandType: CommandType.StoredProcedure
            );

            var list = new List<DepositWithdrawMaxAmountRowDto>();
            foreach (var r in rawRows)
            {
                var dict = (IDictionary<string, object>)r;

                var memberId = GetString(dict, "MemberId");
                var transactionDateBs = GetString(dict, "TransactionDateBs");

                // The SP's "TransactionDate"/"Date" column may come back as a BS-formatted
                // string (e.g. "2081/04/32") rather than a true AD DateTime. Convert.ToDateTime
                // on that value throws "was not recognized as a valid DateTime" because day 32
                // doesn't exist in the Gregorian calendar. GetDateTime below is now safe and
                // returns null instead of throwing; if it comes back null we fall back to
                // converting the BS string ourselves.
                DateTime? transactionDate = GetDateTime(dict, "TransactionDate") ?? GetDateTime(dict, "Date");

                if (transactionDate == null && !string.IsNullOrEmpty(transactionDateBs))
                {
                    try
                    {
                        transactionDate = await _dateConverter.NepaliToEnglishAsync(transactionDateBs);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex,
                            "Failed to convert BS date {BsDate} to AD for MemberId {MemberId}",
                            transactionDateBs, memberId);
                    }
                }

                var row = new DepositWithdrawMaxAmountRowDto
                {
                    MemberId = memberId,
                    MemberName = GetString(dict, "MemberName"),
                    AccountNo = GetString(dict, "AccountNo"),
                    TransactionDate = transactionDate,
                    TransactionDateBs = transactionDateBs,
                    DepositAmount = GetDecimal(dict, "DepositAmount"),
                    WithdrawAmount = GetDecimal(dict, "WithdrawAmount"),
                    Particulars = GetString(dict, "Particulars") ?? GetString(dict, "Narration") ?? GetString(dict, "Description")
                };
                list.Add(row);
            }

            _logger.LogInformation("DepositWithdrawMaxAmountRange returned {Count} rows", list.Count);

            return new DepositWithdrawMaxAmountData
            {
                Rows = list,
                TotalRecords = list.Count,
                TotalDeposit = list.Sum(x => x.DepositAmount),
                TotalWithdraw = list.Sum(x => x.WithdrawAmount)
            };
        }

        private static string? GetString(IDictionary<string, object> dict, string key) =>
            dict.TryGetValue(key, out var val) && val != DBNull.Value ? val?.ToString() : null;

        private static decimal GetDecimal(IDictionary<string, object> dict, string key) =>
            dict.TryGetValue(key, out var val) && val != DBNull.Value ? Convert.ToDecimal(val) : 0m;

        // Made safe: never throws. Real DateTime/DateTimeOffset values pass through directly;
        // string values are parsed with TryParse (so an unparseable BS-format string like
        // "2081/04/32" just returns null instead of crashing the whole report); anything else
        // falls back to a guarded Convert.ToDateTime.
        private static DateTime? GetDateTime(IDictionary<string, object> dict, string key)
        {
            if (!dict.TryGetValue(key, out var val) || val == null || val == DBNull.Value)
                return null;

            if (val is DateTime dt)
                return dt;

            if (val is DateTimeOffset dto)
                return dto.DateTime;

            if (val is string s)
            {
                return DateTime.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed)
                    ? parsed
                    : null; // e.g. a BS-format string — not a valid Gregorian date, handled by caller
            }

            try
            {
                return Convert.ToDateTime(val);
            }
            catch
            {
                return null;
            }
        }
    }
}