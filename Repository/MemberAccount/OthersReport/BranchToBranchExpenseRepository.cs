//// Repository/MemberAccount/OthersReport/BranchToBranchExpenseRepository.cs
//using Dapper;
//using Microsoft.Data.SqlClient;
//using Microsoft.EntityFrameworkCore;
//using NexgenCosysReport.DbContext;
//using NexgenCosysReport.Dtos.RequestDtos.MemberAccount.OthersReport;
//using NexgenCosysReport.Inteface.ReportInterface;
//using NexgenCosysReport.Inteface.ServiceInterface.Common;
//using System.Data;
//using System.Text;

//namespace NexgenCosysAPI.Repository.MemberAccount.OthersReport
//{
//    public class BranchToBranchExpenseRepository : IBranchToBranchExpenseRepository
//    {
//        private readonly AppDbContext _context;
//        private readonly IDateConverterService _dateConverter;
//        private readonly ILogger<BranchToBranchExpenseRepository> _logger;

//        public BranchToBranchExpenseRepository(AppDbContext context, IDateConverterService dateConverter, ILogger<BranchToBranchExpenseRepository> logger)
//        {
//            _context = context;
//            _dateConverter = dateConverter;
//            _logger = logger;
//        }

//        public async Task<BranchToBranchExpenseData> GetReportDataAsync(BranchToBranchExpenseRequestDto request)
//        {
//            try
//            {
//                // Convert Nepali dates to English
//                var fromDateAd = await _dateConverter.NepaliToEnglishAsync(request.FromDateBs);
//                var toDateAd = await _dateConverter.NepaliToEnglishAsync(request.ToDateBs);

//                var fromDateStr = fromDateAd.ToString("yyyy-MM-dd");
//                var toDateStr = toDateAd.ToString("yyyy-MM-dd");

//                // Build @SqlFilterExp like the legacy BLL
//                var sqlFilterExp = new StringBuilder();
//                sqlFilterExp.Append(" And t.TransactionOn between '")
//                            .Append(fromDateStr).Append("' And '")
//                            .Append(toDateStr).Append("' ");

//                if (request.BranchFromId != -1)
//                {
//                    sqlFilterExp.Append(" And t.UsmOfficeId = ").Append(request.BranchFromId);
//                }

//                if (request.CollectorId.HasValue && request.CollectorId.Value != -1)
//                {
//                    sqlFilterExp.Append(" And t.HurCollectorId = ").Append(request.CollectorId.Value);
//                }

//                // Build Account Expression based on Report Type (Expense only - no Loan)
//                var sqlAccountExp = new StringBuilder();
//                switch (request.ReportType)
//                {
//                    case "Saving Wise":
//                        sqlAccountExp.Append(" And a.UsmOfficeId = ").Append(request.BranchToId);
//                        break;
//                    case "Miscellaneous Wise":
//                        sqlAccountExp.Append(" And m.UsmOfficeId = ").Append(request.BranchToId);
//                        break;
//                    case "Share Wise":
//                        sqlAccountExp.Append(" And m.UsmOfficeId = ").Append(request.BranchToId);
//                        break;
//                    default: // All
//                        break;
//                }

//                // Build Order By
//                var sqlOrderBy = new StringBuilder();
//                switch (request.OrderBy)
//                {
//                    case "Member Id":
//                        sqlOrderBy.Append(" order by substring(MemberId, 1,(len(MemberId)-charindex('-', MemberId))-1), MemberId");
//                        break;
//                    case "Member Name":
//                        sqlOrderBy.Append(" order by MemberName");
//                        break;
//                    case "Account No":
//                        sqlOrderBy.Append(" order by substring(AccountNo, 1,(len(AccountNo)-charindex('-', AccountNo))-1), AccountNo");
//                        break;
//                    case "Amount":
//                        sqlOrderBy.Append(" order by Amount DESC");
//                        break;
//                    default:
//                        sqlOrderBy.Append(" order by MemberId");
//                        break;
//                }

//                var connectionString = _context.Database.GetConnectionString();
//                using var connection = new SqlConnection(connectionString);
//                await connection.OpenAsync();

//                var parameters = new DynamicParameters();
//                parameters.Add("@SqlFilterExp", sqlFilterExp.ToString(), DbType.String, size: -1);
//                parameters.Add("@SqlFilterAccountExp", sqlAccountExp.ToString(), DbType.String, size: -1);
//                parameters.Add("@SqlFilterOrderBy", sqlOrderBy.ToString(), DbType.String, size: -1);

//                DataTable dt = new DataTable();

//                // Based on report type, call different SP or use different queries
//                if (request.ReportType == "All")
//                {
//                    var allRows = await GetAllReportDataAsync(connection, parameters, request);
//                    return allRows;
//                }
//                else
//                {
//                    string spName = GetStoredProcedureName(request.ReportType);
//                    var rows = await connection.QueryAsync<BranchToBranchExpenseRowDto>(
//                        spName,
//                        parameters,
//                        commandType: CommandType.StoredProcedure
//                    );

//                    var resultList = rows.AsList();

//                    var branchFromName = await GetOfficeNameByIdAsync(request.BranchFromId);
//                    var branchToName = await GetOfficeNameByIdAsync(request.BranchToId);
//                    string? collectorName = null;
//                    if (request.CollectorId.HasValue && request.CollectorId.Value != -1)
//                    {
//                        collectorName = await GetCollectorNameByIdAsync(request.CollectorId.Value);
//                    }

//                    return new BranchToBranchExpenseData
//                    {
//                        Rows = resultList,
//                        TotalRecords = resultList.Count,
//                        TotalAmount = resultList.Sum(r => r.Amount ?? 0),
//                        FromDateBs = request.FromDateBs,
//                        ToDateBs = request.ToDateBs,
//                        BranchFromId = request.BranchFromId,
//                        BranchFromName = branchFromName ?? "Unknown",
//                        BranchToId = request.BranchToId,
//                        BranchToName = branchToName ?? "Unknown",
//                        CollectorId = request.CollectorId,
//                        CollectorName = collectorName,
//                        ReportType = request.ReportType,
//                        OrderBy = request.OrderBy
//                    };
//                }
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, "Error in GetReportDataAsync");
//                throw;
//            }
//        }

//        private async Task<BranchToBranchExpenseData> GetAllReportDataAsync(
//            SqlConnection connection,
//            DynamicParameters baseParams,
//            BranchToBranchExpenseRequestDto request)
//        {
//            // For "All" type, call multiple SPs and merge (No Loan in Expense)
//            var allRows = new List<BranchToBranchExpenseRowDto>();

//            string[] spNames = {
//                "sp_5_43_GetBranchToBranchExpenseSaving",
//                "sp_5_43_GetBranchToBranchExpenseMiscellineous",
//                "sp_5_43_GetBranchToBranchExpenseShare"
//            };

//            foreach (var spName in spNames)
//            {
//                var rows = await connection.QueryAsync<BranchToBranchExpenseRowDto>(
//                    spName,
//                    baseParams,
//                    commandType: CommandType.StoredProcedure
//                );
//                allRows.AddRange(rows);
//            }

//            var sortedRows = ApplySorting(allRows, request.OrderBy);

//            var branchFromName = await GetOfficeNameByIdAsync(request.BranchFromId);
//            var branchToName = await GetOfficeNameByIdAsync(request.BranchToId);
//            string? collectorName = null;
//            if (request.CollectorId.HasValue && request.CollectorId.Value != -1)
//            {
//                collectorName = await GetCollectorNameByIdAsync(request.CollectorId.Value);
//            }

//            return new BranchToBranchExpenseData
//            {
//                Rows = sortedRows,
//                TotalRecords = sortedRows.Count,
//                TotalAmount = sortedRows.Sum(r => r.Amount ?? 0),
//                FromDateBs = request.FromDateBs,
//                ToDateBs = request.ToDateBs,
//                BranchFromId = request.BranchFromId,
//                BranchFromName = branchFromName ?? "Unknown",
//                BranchToId = request.BranchToId,
//                BranchToName = branchToName ?? "Unknown",
//                CollectorId = request.CollectorId,
//                CollectorName = collectorName,
//                ReportType = request.ReportType,
//                OrderBy = request.OrderBy
//            };
//        }

//        private List<BranchToBranchExpenseRowDto> ApplySorting(List<BranchToBranchExpenseRowDto> rows, string orderBy)
//        {
//            return orderBy switch
//            {
//                "Member Id" => rows.OrderBy(r => r.MemberId).ToList(),
//                "Member Name" => rows.OrderBy(r => r.MemberName).ToList(),
//                "Account No" => rows.OrderBy(r => r.AccountNo).ToList(),
//                "Amount" => rows.OrderByDescending(r => r.Amount).ToList(),
//                _ => rows.OrderBy(r => r.MemberId).ToList()
//            };
//        }

//        private string GetStoredProcedureName(string reportType)
//        {
//            return reportType switch
//            {
//                "Saving Wise" => "sp_5_43_GetBranchToBranchExpenseSaving",
//                "Miscellaneous Wise" => "sp_5_43_GetBranchToBranchExpenseMiscellineous",
//                "Share Wise" => "sp_5_43_GetBranchToBranchExpenseShare",
//                _ => "sp_5_43_GetBranchToBranchExpenseSaving"
//            };
//        }

//        public async Task<string?> GetOfficeNameByIdAsync(long officeId)
//        {
//            try
//            {
//                var connectionString = _context.Database.GetConnectionString();
//                using var connection = new SqlConnection(connectionString);
//                await connection.OpenAsync();
//                const string sql = "SELECT OfficeName FROM UsmOffice WHERE UsmOfficeId = @OfficeId";
//                return await connection.ExecuteScalarAsync<string>(sql, new { OfficeId = officeId });
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, "Error in GetOfficeNameByIdAsync");
//                return null;
//            }
//        }

//        public async Task<string?> GetCollectorNameByIdAsync(long collectorId)
//        {
//            try
//            {
//                var connectionString = _context.Database.GetConnectionString();
//                using var connection = new SqlConnection(connectionString);
//                await connection.OpenAsync();
//                const string sql = "SELECT CollectorFullName FROM HurCollector WHERE HurCollectorId = @CollectorId";
//                return await connection.ExecuteScalarAsync<string>(sql, new { CollectorId = collectorId });
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, "Error in GetCollectorNameByIdAsync");
//                return null;
//            }
//        }
//    }
//}







// Repository/MemberAccount/OthersReport/BranchToBranchExpenseRepository.cs
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using NexgenCosysReport.DbContext;
using NexgenCosysReport.Dtos.RequestDtos.MemberAccount.OthersReport;
using NexgenCosysReport.Inteface.ReportInterface;
using NexgenCosysReport.Inteface.ServiceInterface.Common;
using System.Data;
using System.Text;

namespace NexgenCosysAPI.Repository.MemberAccount.OthersReport
{
    public class BranchToBranchExpenseRepository : IBranchToBranchExpenseRepository
    {
        private readonly AppDbContext _context;
        private readonly IDateConverterService _dateConverter;
        private readonly ILogger<BranchToBranchExpenseRepository> _logger;

        public BranchToBranchExpenseRepository(AppDbContext context, IDateConverterService dateConverter, ILogger<BranchToBranchExpenseRepository> logger)
        {
            _context = context;
            _dateConverter = dateConverter;
            _logger = logger;
        }

        public async Task<BranchToBranchExpenseData> GetReportDataAsync(BranchToBranchExpenseRequestDto request)
        {
            try
            {
                // Convert Nepali dates to English
                var fromDateAd = await _dateConverter.NepaliToEnglishAsync(request.FromDateBs);
                var toDateAd = await _dateConverter.NepaliToEnglishAsync(request.ToDateBs);

                var fromDateStr = fromDateAd.ToString("yyyy-MM-dd");
                var toDateStr = toDateAd.ToString("yyyy-MM-dd");

                // Build @SqlFilterExp like the legacy BLL (date + from-branch only)
                var sqlFilterExp = new StringBuilder();
                sqlFilterExp.Append(" And t.TransactionOn between '")
                            .Append(fromDateStr).Append("' And '")
                            .Append(toDateStr).Append("' ");

                if (request.BranchFromId != -1)
                {
                    sqlFilterExp.Append(" And t.UsmOfficeId = ").Append(request.BranchFromId);
                }

                // Build @SqlCollectorExp separately — SP declares its own parameter for this
                var sqlCollectorExp = new StringBuilder();
                if (request.CollectorId.HasValue && request.CollectorId.Value != -1)
                {
                    sqlCollectorExp.Append(" And t.HurCollectorId = ").Append(request.CollectorId.Value);
                }

                // Build Account Expression based on Report Type (Expense only - no Loan)
                var sqlAccountExp = new StringBuilder();
                switch (request.ReportType)
                {
                    case "Saving Wise":
                        sqlAccountExp.Append(" And a.UsmOfficeId = ").Append(request.BranchToId);
                        break;
                    case "Miscellaneous Wise":
                        sqlAccountExp.Append(" And m.UsmOfficeId = ").Append(request.BranchToId);
                        break;
                    case "Share Wise":
                        sqlAccountExp.Append(" And m.UsmOfficeId = ").Append(request.BranchToId);
                        break;
                    default: // All
                        break;
                }

                // Build Order By
                var sqlOrderBy = new StringBuilder();
                switch (request.OrderBy)
                {
                    case "Member Id":
                        sqlOrderBy.Append(" order by substring(MemberId, 1,(len(MemberId)-charindex('-', MemberId))-1), MemberId");
                        break;
                    case "Member Name":
                        sqlOrderBy.Append(" order by MemberName");
                        break;
                    case "Account No":
                        sqlOrderBy.Append(" order by substring(AccountNo, 1,(len(AccountNo)-charindex('-', AccountNo))-1), AccountNo");
                        break;
                    case "Amount":
                        sqlOrderBy.Append(" order by Amount DESC");
                        break;
                    default:
                        sqlOrderBy.Append(" order by MemberId");
                        break;
                }

                var connectionString = _context.Database.GetConnectionString();
                using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();

                var parameters = new DynamicParameters();
                parameters.Add("@SqlFilterExp", sqlFilterExp.ToString(), DbType.String, size: -1);
                parameters.Add("@SqlCollectorExp", sqlCollectorExp.ToString(), DbType.String, size: -1);
                parameters.Add("@SqlFilterAccountExp", sqlAccountExp.ToString(), DbType.String, size: -1);
                parameters.Add("@SqlFilterOrderBy", sqlOrderBy.ToString(), DbType.String, size: -1);

                // Based on report type, call different SP or use different queries
                if (request.ReportType == "All")
                {
                    var allRows = await GetAllReportDataAsync(connection, parameters, request);
                    return allRows;
                }
                else
                {
                    string spName = GetStoredProcedureName(request.ReportType);
                    var rows = await connection.QueryAsync<BranchToBranchExpenseRowDto>(
                        spName,
                        parameters,
                        commandType: CommandType.StoredProcedure
                    );

                    var resultList = rows.AsList();

                    var branchFromName = await GetOfficeNameByIdAsync(request.BranchFromId);
                    var branchToName = await GetOfficeNameByIdAsync(request.BranchToId);
                    string? collectorName = null;
                    if (request.CollectorId.HasValue && request.CollectorId.Value != -1)
                    {
                        collectorName = await GetCollectorNameByIdAsync(request.CollectorId.Value);
                    }

                    return new BranchToBranchExpenseData
                    {
                        Rows = resultList,
                        TotalRecords = resultList.Count,
                        TotalAmount = resultList.Sum(r => r.Amount ?? 0),
                        FromDateBs = request.FromDateBs,
                        ToDateBs = request.ToDateBs,
                        BranchFromId = request.BranchFromId,
                        BranchFromName = branchFromName ?? "Unknown",
                        BranchToId = request.BranchToId,
                        BranchToName = branchToName ?? "Unknown",
                        CollectorId = request.CollectorId,
                        CollectorName = collectorName,
                        ReportType = request.ReportType,
                        OrderBy = request.OrderBy
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetReportDataAsync");
                throw;
            }
        }

        private async Task<BranchToBranchExpenseData> GetAllReportDataAsync(
            SqlConnection connection,
            DynamicParameters baseParams,
            BranchToBranchExpenseRequestDto request)
        {
            // For "All" type, call multiple SPs and merge (No Loan in Expense)
            var allRows = new List<BranchToBranchExpenseRowDto>();

            string[] spNames = {
                "sp_5_43_GetBranchToBranchExpenseSaving",
                "sp_5_43_GetBranchToBranchExpenseMiscellineous",
                "sp_5_43_GetBranchToBranchExpenseShare"
            };

            foreach (var spName in spNames)
            {
                var rows = await connection.QueryAsync<BranchToBranchExpenseRowDto>(
                    spName,
                    baseParams,
                    commandType: CommandType.StoredProcedure
                );
                allRows.AddRange(rows);
            }

            var sortedRows = ApplySorting(allRows, request.OrderBy);

            var branchFromName = await GetOfficeNameByIdAsync(request.BranchFromId);
            var branchToName = await GetOfficeNameByIdAsync(request.BranchToId);
            string? collectorName = null;
            if (request.CollectorId.HasValue && request.CollectorId.Value != -1)
            {
                collectorName = await GetCollectorNameByIdAsync(request.CollectorId.Value);
            }

            return new BranchToBranchExpenseData
            {
                Rows = sortedRows,
                TotalRecords = sortedRows.Count,
                TotalAmount = sortedRows.Sum(r => r.Amount ?? 0),
                FromDateBs = request.FromDateBs,
                ToDateBs = request.ToDateBs,
                BranchFromId = request.BranchFromId,
                BranchFromName = branchFromName ?? "Unknown",
                BranchToId = request.BranchToId,
                BranchToName = branchToName ?? "Unknown",
                CollectorId = request.CollectorId,
                CollectorName = collectorName,
                ReportType = request.ReportType,
                OrderBy = request.OrderBy
            };
        }

        private List<BranchToBranchExpenseRowDto> ApplySorting(List<BranchToBranchExpenseRowDto> rows, string orderBy)
        {
            return orderBy switch
            {
                "Member Id" => rows.OrderBy(r => r.MemberId).ToList(),
                "Member Name" => rows.OrderBy(r => r.MemberName).ToList(),
                "Account No" => rows.OrderBy(r => r.AccountNo).ToList(),
                "Amount" => rows.OrderByDescending(r => r.Amount).ToList(),
                _ => rows.OrderBy(r => r.MemberId).ToList()
            };
        }

        private string GetStoredProcedureName(string reportType)
        {
            return reportType switch
            {
                "Saving Wise" => "sp_5_43_GetBranchToBranchExpenseSaving",
                "Miscellaneous Wise" => "sp_5_43_GetBranchToBranchExpenseMiscellineous",
                "Share Wise" => "sp_5_43_GetBranchToBranchExpenseShare",
                _ => "sp_5_43_GetBranchToBranchExpenseSaving"
            };
        }

        public async Task<string?> GetOfficeNameByIdAsync(long officeId)
        {
            try
            {
                var connectionString = _context.Database.GetConnectionString();
                using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();
                const string sql = "SELECT OfficeName FROM UsmOffice WHERE UsmOfficeId = @OfficeId";
                return await connection.ExecuteScalarAsync<string>(sql, new { OfficeId = officeId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetOfficeNameByIdAsync");
                return null;
            }
        }

        public async Task<string?> GetCollectorNameByIdAsync(long collectorId)
        {
            try
            {
                var connectionString = _context.Database.GetConnectionString();
                using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();
                const string sql = "SELECT CollectorFullName FROM HurCollector WHERE HurCollectorId = @CollectorId";
                return await connection.ExecuteScalarAsync<string>(sql, new { CollectorId = collectorId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetCollectorNameByIdAsync");
                return null;
            }
        }
    }
}