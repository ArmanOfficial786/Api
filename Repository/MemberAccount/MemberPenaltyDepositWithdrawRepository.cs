// Repository/AccountOperation/MemberPenaltyDepositWithdrawRepository.cs
using Dapper;
using NexgenCosysReport.DbContext;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using NexgenCosysReport.Dtos.RequestDtos.MemberAccount;
using NexgenCosysReport.Inteface.ServiceInterface.Common;
using NexgenCosysReport.Inteface.ServiceInterface.MemberAccount;
using System.Data;
using System.Globalization;

namespace NexgenCosysReport.Repository.MemberAccount
{
    public class MemberPenaltyDepositWithdrawRepository : IMemberPenaltyDepositWithdraw
    {
        private readonly AppDbContext _context;
        private readonly IDateConverterService _dateConverter;
        private readonly ILogger<MemberPenaltyDepositWithdrawRepository> _logger;

        public MemberPenaltyDepositWithdrawRepository(
            AppDbContext context,
            IDateConverterService dateConverter,
            ILogger<MemberPenaltyDepositWithdrawRepository> logger)
        {
            _context = context;
            _dateConverter = dateConverter;
            _logger = logger;
        }

        public async Task<MemberPenaltyDepositWithdrawData> GetMemberPenaltyDepositWithdrawReport(MemberPenaltyDepositWithdrawRequest request)
        {
            var connectionString = _context.Database.GetConnectionString();
            using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();

            // --- Date filter --- 
            string fromDateStr = string.Empty;
            string toDateStr = string.Empty;

            try
            {
                if (!string.IsNullOrEmpty(request.FromDate) && request.FromDate != "-1")
                {
                    var fromDateAd = await _dateConverter.NepaliToEnglishAsync(request.FromDate);
                    fromDateStr = fromDateAd.ToString("yyyy-MM-dd");
                }
                else
                {
                    fromDateStr = DateTime.Now.AddMonths(-6).ToString("yyyy-MM-dd");
                }

                if (!string.IsNullOrEmpty(request.ToDate) && request.ToDate != "-1")
                {
                    var toDateAd = await _dateConverter.NepaliToEnglishAsync(request.ToDate);
                    toDateStr = toDateAd.ToString("yyyy-MM-dd");
                }
                else
                {
                    toDateStr = DateTime.Now.ToString("yyyy-MM-dd");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Date conversion failed. FromDate: {FromDate}, ToDate: {ToDate}",
                    request.FromDate, request.ToDate);

                fromDateStr = DateTime.Now.AddMonths(-6).ToString("yyyy-MM-dd");
                toDateStr = DateTime.Now.ToString("yyyy-MM-dd");
            }

            var sqlDateFilter = $" And AT.TransactionOn between '{fromDateStr}' AND '{toDateStr}' ";

            // --- Office filter --- 
            var sqlOfficeFilter = string.Empty;
            if (request.BranchIds != "-1" && !string.IsNullOrEmpty(request.BranchIds))
            {
                sqlOfficeFilter = " AND AT.UsmOfficeId in (" + request.BranchIds + ")";
            }

            // --- Amount filter based on transaction type ---
            var amountValue = request.Amount;
            var sqlAmountFilter = string.Empty;

            // For Penalty Report (Type 1), we use DepositAmount as Penalty
            if (request.TransactionType == 1)
            {
                sqlAmountFilter = $" AND DepositAmount >= {amountValue.ToString(CultureInfo.InvariantCulture)}";
            }
            else
            {
                sqlAmountFilter = request.TransactionType switch
                {
                    2 => $" AND DepositAmount >= {amountValue.ToString(CultureInfo.InvariantCulture)}",
                    3 => $" AND WithdrawAmount >= {amountValue.ToString(CultureInfo.InvariantCulture)}",
                    4 => $" AND BalanceAmount >= {amountValue.ToString(CultureInfo.InvariantCulture)}",
                    _ => $" AND DepositAmount >= {amountValue.ToString(CultureInfo.InvariantCulture)}"
                };
            }

            // --- Order by ---
            var sqlFilterExpOrderBy = string.Empty;
            if (!string.IsNullOrEmpty(request.OrderBy) && request.OrderBy != "-1")
            {
                sqlFilterExpOrderBy = request.OrderBy switch
                {
                    "MemberId" => " order by MemberIdFirst, MemberIdLast ",
                    "MemberName" => " order by MemberName",
                    "Penalty" => " order by DepositAmount DESC",
                    "Deposit" => " order by DepositAmount DESC",
                    "Withdraw" => " order by WithdrawAmount DESC",
                    "Balance" => " order by BalanceAmount DESC",
                    _ => string.Empty
                };
            }

            _logger.LogInformation(
                "MemberPenaltyDepositWithdraw SP params -> OfficeFilter: [{OfficeFilter}] DateFilter: [{DateFilter}] AmountFilter: [{AmountFilter}] OrderBy: [{OrderBy}]",
                sqlOfficeFilter, sqlDateFilter, sqlAmountFilter, sqlFilterExpOrderBy);

            var parameters = new DynamicParameters();
            parameters.Add("@SqlOfficeFilter", sqlOfficeFilter);
            parameters.Add("@SqlDateFilter", sqlDateFilter);
            parameters.Add("@SqlAmountFilter", sqlAmountFilter);
            parameters.Add("@SqlFilterExpOrderBy", sqlFilterExpOrderBy);

            try
            {
                var rawRows = await connection.QueryAsync(
                    "sp_5_43_GetMemberPenaltyDepositWithdrawBalance",
                    parameters,
                    commandType: CommandType.StoredProcedure
                );

                var list = new List<MemberPenaltyDepositWithdrawRowDto>();
                foreach (var r in rawRows)
                {
                    var dict = (IDictionary<string, object>)r;
                    var row = new MemberPenaltyDepositWithdrawRowDto
                    {
                        MemberId = GetString(dict, "MemberId"),
                        MemberName = GetString(dict, "MemberName"),
                        Address = GetString(dict, "MemberAddress"),
                        Contact = GetString(dict, "Contact"),
                        PenaltyAmount = GetDecimal(dict, "DepositAmount"), // Penalty uses DepositAmount
                        DepositAmount = GetDecimal(dict, "DepositAmount"),
                        WithdrawAmount = GetDecimal(dict, "WithdrawAmount"),
                        BalanceAmount = GetDecimal(dict, "BalanceAmount")
                    };
                    list.Add(row);
                }

                _logger.LogInformation("MemberPenaltyDepositWithdraw returned {Count} rows", list.Count);

                return new MemberPenaltyDepositWithdrawData
                {
                    Rows = list,
                    TotalRecords = list.Count,
                    TotalPenalty = list.Sum(x => x.PenaltyAmount),
                    TotalDeposit = list.Sum(x => x.DepositAmount),
                    TotalWithdraw = list.Sum(x => x.WithdrawAmount),
                    TotalBalance = list.Sum(x => x.BalanceAmount)
                };
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "SQL Error in MemberPenaltyDepositWithdraw. Parameters: {@Parameters}", parameters);
                throw new Exception($"Database error: {ex.Message}", ex);
            }
        }

        private static string? GetString(IDictionary<string, object> dict, string key)
        {
            if (dict.TryGetValue(key, out var val) && val != DBNull.Value)
            {
                return val?.ToString();
            }
            return null;
        }

        private static decimal GetDecimal(IDictionary<string, object> dict, string key)
        {
            if (dict.TryGetValue(key, out var val) && val != DBNull.Value)
            {
                try
                {
                    return Convert.ToDecimal(val);
                }
                catch
                {
                    return 0m;
                }
            }
            return 0m;
        }
    }
}
