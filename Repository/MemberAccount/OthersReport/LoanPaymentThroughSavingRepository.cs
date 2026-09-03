
////using Dapper;
////using Microsoft.Data.SqlClient;
////using Microsoft.EntityFrameworkCore;
////using NexgenCosysReport.DbContext;
////using NexgenCosysReport.Dtos.RequestDtos.MemberAccount.OthersReport;
////using NexgenCosysReport.Inteface.ServiceInterface.Common;
////using NexgenCosysReport.Inteface.ServiceInterface.MemberAccount.OthersReport;
////using System.Data;

////namespace NexgenCosysReport.Repository.MemberAccount.OthersReport
////{
////    public class LoanPaymentThroughSavingRepository : ILoanPaymentThroughSavingRepository
////    {
////        private readonly AppDbContext _context;
////        private readonly IDateConverterService _dateConverter;
////        private readonly ILogger<LoanPaymentThroughSavingRepository> _logger;

////        public LoanPaymentThroughSavingRepository(AppDbContext context, IDateConverterService dateConverter, ILogger<LoanPaymentThroughSavingRepository> logger)
////        {
////            _context = context;
////            _dateConverter = dateConverter;
////            _logger = logger;
////        }

////        public async Task<LoanPaymentThroughSavingData> GetReportDataAsync(LoanPaymentThroughSavingRequestDto request)
////        {
////            try
////            {
////                // Convert Nepali dates to English
////                var fromDateAd = await _dateConverter.NepaliToEnglishAsync(request.FromDateBs);
////                var toDateAd = await _dateConverter.NepaliToEnglishAsync(request.ToDateBs);

////                var fromDateStr = fromDateAd.ToString("yyyy-MM-dd");
////                var toDateStr = toDateAd.ToString("yyyy-MM-dd");

////                // Build branch filter string
////                string branchIdsStr = "-1";
////                if (request.BranchIds.Count > 0)
////                {
////                    branchIdsStr = string.Join(",", request.BranchIds);
////                }

////                // Get branch names for header
////                var branchNames = await GetBranchNamesListAsync(request.BranchIds);
////                string branchNameDisplay = branchNames.Count > 0
////                    ? (branchNames.Count == 1 ? branchNames[0] : string.Join(", ", branchNames))
////                    : "All Branches";

////                var connectionString = _context.Database.GetConnectionString();
////                using var connection = new SqlConnection(connectionString);
////                await connection.OpenAsync();

////                // Determine stored procedure based on report view
////                string spName = request.ReportView == "Horizontal"
////                    ? "sp_5_43_GetLoanPaymentThroughSavingHorizontal"
////                    : "sp_5_43_GetLoanPaymentThroughSaving";

////                // Build filter expression
////                var sqlFilterExp = $" And t.TransactionOn between '{fromDateStr}' And '{toDateStr}' AND t.UsmOfficeId in ({branchIdsStr})";

////                // Build Order By clause
////                var orderByClause = BuildOrderByClause(request.OrderBy);

////                var parameters = new DynamicParameters();
////                parameters.Add("@SqlFilterExp", sqlFilterExp, DbType.String, size: -1);
////                parameters.Add("@SqlFilterExpOrder", orderByClause, DbType.String, size: -1);

////                var rows = await connection.QueryAsync<LoanPaymentThroughSavingRowDto>(
////                    spName,
////                    parameters,
////                    commandType: CommandType.StoredProcedure
////                );

////                var resultList = rows.AsList();

////                return new LoanPaymentThroughSavingData
////                {
////                    Rows = resultList,
////                    TotalRecords = resultList.Count,
////                    TotalNetAmount = resultList.Sum(r => r.NetAmount ?? 0),
////                    FromDateBs = request.FromDateBs,
////                    ToDateBs = request.ToDateBs,
////                    BranchNames = branchNameDisplay,
////                    BranchIds = request.BranchIds,
////                    OrderBy = request.OrderBy,
////                    ReportView = request.ReportView
////                };
////            }
////            catch (Exception ex)
////            {
////                _logger.LogError(ex, "Error in GetReportDataAsync");
////                throw;
////            }
////        }

////        public async Task<List<string>> GetBranchNamesListAsync(List<long> branchIds)
////        {
////            try
////            {
////                if (branchIds.Count == 0)
////                    return ["All Branches"];

////                var connectionString = _context.Database.GetConnectionString();
////                using var connection = new SqlConnection(connectionString);
////                await connection.OpenAsync();

////                var ids = string.Join(",", branchIds);
////                var sql = $"SELECT OfficeName FROM UsmOffice WHERE UsmOfficeId IN ({ids}) ORDER BY OfficeName";

////                var names = await connection.QueryAsync<string>(sql);
////                return names.AsList();
////            }
////            catch (Exception ex)
////            {
////                _logger.LogError(ex, "Error in GetBranchNamesListAsync");
////                return ["All Branches"];
////            }
////        }

////        private static string BuildOrderByClause(string orderBy)
////        {
////            return orderBy switch
////            {
////                "Member Id" => " order by substring(MemberId, 1,(len(MemberId)-charindex('-', MemberId))-1), MemberId",
////                "Member Name" => " order by MemberName",
////                "Account No" => " order by substring(AccountNo, 1,(len(AccountNo)-charindex('-', AccountNo))-1), AccountNo",
////                "Type" => " order by Type",
////                "Amount" => " order by NetAmount DESC",
////                "Date" => " order by Date",
////                "Operator" => " order by Operator",
////                _ => " order by MemberId"
////            };
////        }
////    }
////}



//// Repository/MemberAccount/OthersReport/LoanPaymentThroughSavingRepository.cs
//using Dapper;
//using Microsoft.Data.SqlClient;
//using Microsoft.EntityFrameworkCore;
//using NexgenCosysReport.DbContext;
//using NexgenCosysReport.Dtos.RequestDtos.MemberAccount.OthersReport;
//using NexgenCosysReport.Inteface.ServiceInterface.Common;
//using NexgenCosysReport.Inteface.ServiceInterface.MemberAccount.OthersReport;
//using System.Data;

//namespace NexgenCosysReport.Repository.MemberAccount.OthersReport
//{
//    public class LoanPaymentThroughSavingRepository : ILoanPaymentThroughSavingRepository
//    {
//        private readonly AppDbContext _context;
//        private readonly IDateConverterService _dateConverter;
//        private readonly ILogger<LoanPaymentThroughSavingRepository> _logger;

//        public LoanPaymentThroughSavingRepository(AppDbContext context, IDateConverterService dateConverter, ILogger<LoanPaymentThroughSavingRepository> logger)
//        {
//            _context = context;
//            _dateConverter = dateConverter;
//            _logger = logger;
//        }

//        // --------------------------------------------------------------
//        // @SqlFilterExp
//        // Appended inside SP filter. Uses t.TransactionOn + t.UsmOfficeId
//        // --------------------------------------------------------------
//        private async Task<string> BuildSqlFilterExp(LoanPaymentThroughSavingRequestDto request)
//        {
//            var filter = string.Empty;

//            if (!string.IsNullOrEmpty(request.FromDateBs) &&
//                !string.IsNullOrEmpty(request.ToDateBs) &&
//                request.FromDateBs != "-1" &&
//                request.ToDateBs != "-1")
//            {
//                var fromDateAd = await _dateConverter.NepaliToEnglishAsync(request.FromDateBs);
//                var toDateAd = await _dateConverter.NepaliToEnglishAsync(request.ToDateBs);

//                filter += $" And t.TransactionOn between '{fromDateAd:yyyy-MM-dd}' And '{toDateAd:yyyy-MM-dd}'";
//            }

//            if (!string.IsNullOrEmpty(request.BranchIds) &&
//                request.BranchIds != "-1" &&
//                request.BranchIds != "string")
//            {
//                filter += $" AND t.UsmOfficeId in ({request.BranchIds})";
//            }
//            else
//            {
//                filter += " AND t.UsmOfficeId in (-1)";
//            }

//            return filter;
//        }

//        public async Task<LoanPaymentThroughSavingData> GetReportDataAsync(LoanPaymentThroughSavingRequestDto request)
//        {
//            try
//            {
//                var sqlFilterExp = await BuildSqlFilterExp(request);
//                var orderByClause = BuildOrderByClause(request.OrderBy);

//                var connectionString = _context.Database.GetConnectionString();
//                using var connection = new SqlConnection(connectionString);
//                await connection.OpenAsync();

//                // Determine stored procedure based on report view
//                string spName = request.ReportView == "Horizontal"
//                    ? "sp_5_43_GetLoanPaymentThroughSavingHorizontal"
//                    : "sp_5_43_GetLoanPaymentThroughSaving";

//                var parameters = new DynamicParameters();
//                parameters.Add("@SqlFilterExp", sqlFilterExp, DbType.String, size: -1);
//                parameters.Add("@SqlFilterExpOrder", orderByClause, DbType.String, size: -1);

//                var rows = await connection.QueryAsync<LoanPaymentThroughSavingRowDto>(
//                    spName,
//                    parameters,
//                    commandType: CommandType.StoredProcedure,
//                    commandTimeout: 120
//                );

//                var resultList = rows.AsList();

//                return new LoanPaymentThroughSavingData
//                {
//                    Rows = resultList,
//                    TotalRecords = resultList.Count,
//                    TotalNetAmount = resultList.Sum(r => r.NetAmount ?? 0),
//                    FromDateBs = request.FromDateBs,
//                    ToDateBs = request.ToDateBs,
//                    BranchIds = request.BranchIds,
//                    OrderBy = request.OrderBy,
//                    ReportView = request.ReportView
//                };
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, "Error in GetReportDataAsync");
//                throw;
//            }
//        }

//        private static string BuildOrderByClause(string orderBy)
//        {
//            return orderBy switch
//            {
//                "Member Id" => " order by substring(MemberId, 1,(len(MemberId)-charindex('-', MemberId))-1), MemberId",
//                "Member Name" => " order by MemberName",
//                "Account No" => " order by substring(AccountNo, 1,(len(AccountNo)-charindex('-', AccountNo))-1), AccountNo",
//                "Type" => " order by Type",
//                "Amount" => " order by NetAmount DESC",
//                "Date" => " order by Date",
//                "Operator" => " order by Operator",
//                _ => " order by MemberId"
//            };
//        }
//    }
//}






// Repository/MemberAccount/OthersReport/LoanPaymentThroughSavingRepository.cs
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using NexgenCosysReport.DbContext;
using NexgenCosysReport.Dtos.RequestDtos.MemberAccount.OthersReport;
using NexgenCosysReport.Inteface.ServiceInterface.Common;
using NexgenCosysReport.Inteface.ServiceInterface.MemberAccount.OthersReport;
using System.Data;

namespace NexgenCosysReport.Repository.MemberAccount.OthersReport
{
    public class LoanPaymentThroughSavingRepository : ILoanPaymentThroughSavingRepository
    {
        private readonly AppDbContext _context;
        private readonly IDateConverterService _dateConverter;
        private readonly ILogger<LoanPaymentThroughSavingRepository> _logger;

        public LoanPaymentThroughSavingRepository(AppDbContext context, IDateConverterService dateConverter, ILogger<LoanPaymentThroughSavingRepository> logger)
        {
            _context = context;
            _dateConverter = dateConverter;
            _logger = logger;
        }

        // --------------------------------------------------------------
        // @SqlFilterExp
        // Appended inside SP filter. Uses t.TransactionOn + t.UsmOfficeId
        // --------------------------------------------------------------
        private async Task<string> BuildSqlFilterExp(LoanPaymentThroughSavingRequestDto request)
        {
            var filter = string.Empty;

            if (!string.IsNullOrEmpty(request.FromDateBs) &&
                !string.IsNullOrEmpty(request.ToDateBs) &&
                request.FromDateBs != "-1" &&
                request.ToDateBs != "-1")
            {
                var fromDateAd = await _dateConverter.NepaliToEnglishAsync(request.FromDateBs);
                var toDateAd = await _dateConverter.NepaliToEnglishAsync(request.ToDateBs);

                filter += $" And t.TransactionOn between '{fromDateAd:yyyy-MM-dd}' And '{toDateAd:yyyy-MM-dd}'";
            }

            if (!string.IsNullOrEmpty(request.BranchIds) &&
                request.BranchIds != "-1" &&
                request.BranchIds != "string")
            {
                filter += $" AND t.UsmOfficeId in ({request.BranchIds})";
            }
            else
            {
                filter += " AND t.UsmOfficeId in (-1)";
            }

            return filter;
        }

        // --------------------------------------------------------------
        // Branch name for header display. Restored — this was present in
        // the old implementation but dropped in the current one, which is
        // why "Branch Name" always fell back to "All Branches" in the view.
        // --------------------------------------------------------------
        private async Task<string> GetBranchNamesDisplayAsync(string? branchIds)
        {
            if (string.IsNullOrWhiteSpace(branchIds) ||
                branchIds == "-1" ||
                branchIds == "string")
            {
                return "All Branches";
            }

            try
            {
                var connectionString = _context.Database.GetConnectionString();
                using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();

                var sql = $"SELECT OfficeName FROM UsmOffice WHERE UsmOfficeId IN ({branchIds}) ORDER BY OfficeName";
                var names = (await connection.QueryAsync<string>(sql)).AsList();

                return names.Count > 0 ? string.Join(", ", names) : "All Branches";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetBranchNamesDisplayAsync");
                return "All Branches";
            }
        }

        public async Task<LoanPaymentThroughSavingData> GetReportDataAsync(LoanPaymentThroughSavingRequestDto request)
        {
            try
            {
                var sqlFilterExp = await BuildSqlFilterExp(request);
                var orderByClause = BuildOrderByClause(request.OrderBy);

                var connectionString = _context.Database.GetConnectionString();
                using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();

                // Determine stored procedure based on report view
                string spName = request.ReportView == "Horizontal"
                    ? "sp_5_43_GetLoanPaymentThroughSavingHorizontal"
                    : "sp_5_43_GetLoanPaymentThroughSaving";

                var parameters = new DynamicParameters();
                parameters.Add("@SqlFilterExp", sqlFilterExp, DbType.String, size: -1);
                parameters.Add("@SqlFilterExpOrder", orderByClause, DbType.String, size: -1);

                // Run branch-name lookup and the SP call in parallel.
                var branchNamesTask = GetBranchNamesDisplayAsync(request.BranchIds);

                // NOTE: queried as dynamic (not typed to LoanPaymentThroughSavingRowDto)
                // because the SP's column names for the amount/breakdown fields don't
                // reliably match the DTO property names 1:1 (this is what was causing
                // NetAmount to come back empty in the Vertical view — Dapper's typed
                // mapping silently leaves unmatched properties null). Mapping manually
                // below with GetString/GetDecimal lets us coalesce across the column
                // name variants the SP might actually use.
                var rawRowsTask = connection.QueryAsync(
                    spName,
                    parameters,
                    commandType: CommandType.StoredProcedure,
                    commandTimeout: 120
                );

                await Task.WhenAll(branchNamesTask, rawRowsTask);

                var branchNameDisplay = await branchNamesTask;
                var rawRows = await rawRowsTask;

                var resultList = new List<LoanPaymentThroughSavingRowDto>();
                foreach (IDictionary<string, object> r in rawRows)
                {
                    resultList.Add(new LoanPaymentThroughSavingRowDto
                    {
                        Date = GetString(r, "Date", "TransactionOn", "TransDate"),
                        MemberId = GetString(r, "MemberId"),
                        MemberName = GetString(r, "MemberName"),

                        AccountNo = GetString(r, "AccountNo", "SavingAccount", "SavingAccountNo", "Saving"),
                        Type = GetString(r, "Type", "SavingType"),

                        SavingAccountNo = GetString(r, "SavingAccount", "Saving", "SavingAccountNo", "SavingAcNo"),
                        LoanAccountNo = GetString(r, "LoanAccount", "Loan", "LoanAccountNo", "LoanAcNo"),
                        Fine = GetDecimal(r, "Fine"),
                        Interest = GetDecimal(r, "Interest"),
                        Principal = GetDecimal(r, "Principle", "Principal"),

                        NetAmount = GetDecimal(r, "NetAmount", "Amount", "Total", "TotalAmount"),
                        Operator = GetString(r, "Operator", "OperatorName"),
                        Details = GetString(r, "Details")
                    });
                }

                return new LoanPaymentThroughSavingData
                {
                    Rows = resultList,
                    TotalRecords = resultList.Count,
                    TotalNetAmount = resultList.Sum(r => r.NetAmount ?? 0),
                    FromDateBs = request.FromDateBs,
                    ToDateBs = request.ToDateBs,
                    BranchNames = branchNameDisplay,
                    BranchIds = request.BranchIds,
                    OrderBy = request.OrderBy,
                    ReportView = request.ReportView
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetReportDataAsync");
                throw;
            }
        }

        // --------------------------------------------------------------
        // Case-insensitive, multi-alias lookups into a Dapper dynamic row.
        // --------------------------------------------------------------
        private static string? GetString(IDictionary<string, object> row, params string[] keys)
        {
            foreach (var key in keys)
            {
                var match = row.Keys.FirstOrDefault(k => string.Equals(k, key, StringComparison.OrdinalIgnoreCase));
                if (match != null && row[match] != null && row[match] != DBNull.Value)
                    return row[match].ToString();
            }
            return null;
        }

        private static decimal? GetDecimal(IDictionary<string, object> row, params string[] keys)
        {
            foreach (var key in keys)
            {
                var match = row.Keys.FirstOrDefault(k => string.Equals(k, key, StringComparison.OrdinalIgnoreCase));
                if (match != null && row[match] != null && row[match] != DBNull.Value &&
                    decimal.TryParse(row[match].ToString(), out var val))
                {
                    return val;
                }
            }
            return null;
        }

        private static string BuildOrderByClause(string orderBy)
        {
            return orderBy switch
            {
                "Member Id" => " order by substring(MemberId, 1,(len(MemberId)-charindex('-', MemberId))-1), MemberId",
                "Member Name" => " order by MemberName",
                "Account No" => " order by substring(AccountNo, 1,(len(AccountNo)-charindex('-', AccountNo))-1), AccountNo",
                "Type" => " order by Type",
                "Amount" => " order by NetAmount DESC",
                "Date" => " order by Date",
                "Operator" => " order by Operator",
                _ => " order by MemberId"
            };
        }
    }
}