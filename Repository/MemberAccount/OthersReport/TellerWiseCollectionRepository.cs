//// Repositories/Implementations/AccountOperation/TellerWiseCollectionRepository.cs
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
//    public class TellerWiseCollectionRepository : ITellerWiseCollection
//    {
//        private readonly AppDbContext _context;
//        private readonly IDateConverterService _dateConverter;
//        private readonly ILogger<TellerWiseCollectionRepository> _logger;

//        public TellerWiseCollectionRepository(AppDbContext context, IDateConverterService dateConverter, ILogger<TellerWiseCollectionRepository> logger)
//        {
//            _context = context;
//            _dateConverter = dateConverter;
//            _logger = logger;
//        }

//        public async Task<TellerWiseCollectionData> GetReportDataAsync(TellerWiseCollectionRequestDto request)
//        {
//            try
//            {
//                var fromDateAd = await _dateConverter.NepaliToEnglishAsync(request.FromDateBs);
//                var toDateAd = await _dateConverter.NepaliToEnglishAsync(request.ToDateBs);

//                var fromDateStr = fromDateAd.ToString("MM/dd/yyyy");
//                var toDateStr = toDateAd.ToString("MM/dd/yyyy");

//                var connectionString = _context.Database.GetConnectionString();
//                using var connection = new SqlConnection(connectionString);
//                await connection.OpenAsync();

//                var parameters = new DynamicParameters();
//                parameters.Add("@SqlFromDate", fromDateStr);
//                parameters.Add("@SqlToDate", toDateStr);
//                parameters.Add("@SqlTellerId", request.TellerId ?? -1);
//                parameters.Add("@SqlOrderBy", request.OrderBy);

//                var rows = await connection.QueryAsync<TellerWiseCollectionRowDto>(
//                    "sp_5_43_GetTellerWiseCollection",
//                    parameters,
//                    commandType: CommandType.StoredProcedure
//                );

//                var resultList = rows.AsList();
//                decimal totalAmount = resultList.Sum(r => r.Amount ?? 0);

//                string? tellerName = null;
//                if (request.TellerId.HasValue && request.TellerId.Value > 0)
//                {
//                    tellerName = await GetTellerNameByIdAsync(request.TellerId.Value);
//                }

//                return new TellerWiseCollectionData
//                {
//                    Rows = resultList,
//                    TotalRecords = resultList.Count,
//                    TotalAmount = totalAmount,
//                    FromDateBs = request.FromDateBs,
//                    ToDateBs = request.ToDateBs,
//                    TellerId = request.TellerId,
//                    TellerName = tellerName ?? "All Tellers",
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






// Repositories/Implementations/AccountOperation/TellerWiseCollectionRepository.cs
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using NexgenCosysReport.DbContext;
using NexgenCosysReport.Dtos.RequestDtos.MemberAccount.OthersReport;
using NexgenCosysReport.Inteface.ServiceInterface.Common;
using NexgenCosysReport.Inteface.ServiceInterface.MemberAccount.OthersReport;
using System.Data;
using System.Text;

namespace NexgenCosysReport.Repository.MemberAccount.OthersReport
{
    public class TellerWiseCollectionRepository : ITellerWiseCollection
    {
        private readonly AppDbContext _context;
        private readonly IDateConverterService _dateConverter;
        private readonly ILogger<TellerWiseCollectionRepository> _logger;

        public TellerWiseCollectionRepository(AppDbContext context, IDateConverterService dateConverter, ILogger<TellerWiseCollectionRepository> logger)
        {
            _context = context;
            _dateConverter = dateConverter;
            _logger = logger;
        }

        public async Task<TellerWiseCollectionData> GetReportDataAsync(TellerWiseCollectionRequestDto request)
        {
            try
            {
                var fromDateAd = await _dateConverter.NepaliToEnglishAsync(request.FromDateBs);
                var toDateAd = await _dateConverter.NepaliToEnglishAsync(request.ToDateBs);

                // ISO format avoids SQL Server regional/language ambiguity for string->datetime literals
                var fromDateStr = fromDateAd.ToString("yyyy-MM-dd");
                var toDateStr = toDateAd.ToString("yyyy-MM-dd");

                long tellerId = request.TellerId ?? -1;

                // ---- Build @SqlFilterExp exactly like the legacy BLL method did ----
                var sqlFilterExp = new StringBuilder();
                if (tellerId != -1)
                {
                    sqlFilterExp.Append(" And t.CreatedBy = ").Append(tellerId);
                }
                sqlFilterExp.Append(" And t.TransactionOn between '")
                            .Append(fromDateStr).Append("' And '")
                            .Append(toDateStr).Append("' ");

                // ---- Build @SqlFilterExpOrderBy exactly like the legacy BLL method did ----
                var sqlFilterExpOrderBy = new StringBuilder();
                switch (request.OrderBy)
                {
                    case "Account No":
                        sqlFilterExpOrderBy.Append(" order by substring(Details, 1,(len(Details)-charindex('-', Details))-1), Details");
                        break;
                    case "Member Id":
                        sqlFilterExpOrderBy.Append(" order by substring(MemberId, 1,(len(MemberId)-charindex('-', MemberId))-1), MemberId");
                        break;
                    case "Bill No":
                        sqlFilterExpOrderBy.Append(" order by BillNo DESC");
                        break;
                    case "Saving Amount":
                        sqlFilterExpOrderBy.Append(" order by SavingAmount DESC");
                        break;
                    case "Share Amount":
                        sqlFilterExpOrderBy.Append(" order by ShareAmount DESC");
                        break;
                    case "Loan Principle":
                        sqlFilterExpOrderBy.Append(" order by LoanPrinciple DESC");
                        break;
                    case "Loan Interest":
                        sqlFilterExpOrderBy.Append(" order by LoanInterest DESC");
                        break;
                    case "Loan Penalty":
                        sqlFilterExpOrderBy.Append(" order by LoanPenalty DESC");
                        break;
                    case "Miscellaneous Amount":
                        sqlFilterExpOrderBy.Append(" order by MiscellaneousAmount DESC");
                        break;
                    default:
                        sqlFilterExpOrderBy.Append(" order by MemberId");
                        break;
                }

                var connectionString = _context.Database.GetConnectionString();
                using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();

                // ---- Only pass the 2 params the SP actually declares ----
                var parameters = new DynamicParameters();
                parameters.Add("@SqlFilterExp", sqlFilterExp.ToString(), DbType.String, size: -1);
                parameters.Add("@SqlFilterExpOrderBy", sqlFilterExpOrderBy.ToString(), DbType.String, size: -1);

                var rows = await connection.QueryAsync<TellerWiseCollectionRowDto>(
                    "sp_5_43_GetTellerWiseCollection",
                    parameters,
                    commandType: CommandType.StoredProcedure
                );

                var resultList = rows.AsList();

                string? tellerName = resultList.FirstOrDefault()?.TellerName;
                if (tellerId != -1 && string.IsNullOrEmpty(tellerName))
                {
                    tellerName = await GetTellerNameByIdAsync(tellerId);
                }

                return new TellerWiseCollectionData
                {
                    Rows = resultList,
                    TotalRecords = resultList.Count,
                    TotalShareAmount = resultList.Sum(r => r.ShareAmount ?? 0),
                    TotalSavingAmount = resultList.Sum(r => r.SavingAmount ?? 0),
                    TotalLoanPrinciple = resultList.Sum(r => r.LoanPrinciple ?? 0),
                    TotalLoanInterest = resultList.Sum(r => r.LoanInterest ?? 0),
                    TotalLoanPenalty = resultList.Sum(r => r.LoanPenalty ?? 0),
                    TotalMiscellaneousAmount = resultList.Sum(r => r.MiscellaneousAmount ?? 0),
                    TotalAmount = resultList.Sum(r => r.RowTotal),
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