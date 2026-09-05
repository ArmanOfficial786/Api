// Repositories/MemberAccount/ChequeBookReport/ChequeBookWithdrawalRepository.cs
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using NexgenCosysReport.DbContext;
using NexgenCosysReport.Dtos.RequestDtos.MemberAccount.ChequeBookReport;
using NexgenCosysReport.Inteface.ServiceInterface.Common;
using NexgenCosysReport.Inteface.ServiceInterface.MemberAccount.ChequeBookReport;
using System.Data;
using System.Text;

namespace NexgenCosysReport.Repository.MemberAccount.ChequeBookReport
{
    public class ChequeBookWithdrawalRepository : IChequeBookWithdrawal
    {
        private readonly AppDbContext _context;
        private readonly IDateConverterService _dateConverter;
        private readonly ILogger<ChequeBookWithdrawalRepository> _logger;

        public ChequeBookWithdrawalRepository(
            AppDbContext context,
            IDateConverterService dateConverter,
            ILogger<ChequeBookWithdrawalRepository> logger)
        {
            _context = context;
            _dateConverter = dateConverter;
            _logger = logger;
        }

        public async Task<ChequeBookWithdrawalData> GetReportDataAsync(ChequeBookWithdrawalRequestDto request)
        {
            try
            {
                var sqlFilterExp = new StringBuilder();

                if (request.AccountId != -1)
                {
                    sqlFilterExp.Append($" AND c.MamAccountOpeningId = {request.AccountId}");
                }

                var orderByClause = BuildOrderByClause(request.OrderBy);
                if (!string.IsNullOrEmpty(orderByClause))
                {
                    sqlFilterExp.Append(orderByClause);
                }

                var parameters = new DynamicParameters();
                parameters.Add("@SqlFilterExp", sqlFilterExp.ToString(), DbType.String, size: -1);

                var connectionString = _context.Database.GetConnectionString();
                using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();

                // Raw shape matching sp_5_43_GetChequeWithdrawl's actual SELECT list:
                // ChequeNo, Date (raw AD datetime = c.LastModifiedOn, NOT a Bs string),
                // Status, AccountNo, MemberId, Name.
                var rawRows = (await connection.QueryAsync<SpRawRow>(
                    "sp_5_43_GetChequeWithdrawl",
                    parameters,
                    commandType: CommandType.StoredProcedure
                )).ToList();

                var resultList = rawRows.Select(MapToRowDto).ToList();

                return new ChequeBookWithdrawalData
                {
                    Rows = resultList,
                    TotalRecords = resultList.Count,
                    TotalWithdrawals = resultList.Count,
                    AccountNo = request.AccountNo,
                    MemberId = request.MemberId,
                    MemberName = request.MemberName,
                    OrderBy = request.OrderBy
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetReportDataAsync for Cheque Book Withdrawal Report");
                throw;
            }
        }

        /// <summary>
        /// Root-cause fix: sp_5_43_GetChequeWithdrawl returns ChequeNo/Date/Status/AccountNo/
        /// MemberId/Name — not the DTO's ChequeDateBs/WithdrawalDateBs/MemberName property
        /// names. Also "Date" is a raw AD datetime (c.LastModifiedOn), so it must be
        /// captured as DateTime? and formatted here, not mapped straight into a string.
        /// </summary>
        private static ChequeBookWithdrawalRowDto MapToRowDto(SpRawRow raw)
        {
            // Image shows MM/dd/yyyy formatting (e.g. 09/02/2014) for withdrawal/lost dates.
            string? dateFormatted = raw.Date.HasValue ? raw.Date.Value.ToString("MM/dd/yyyy") : null;

            return new ChequeBookWithdrawalRowDto
            {
                MemberId = raw.MemberId,
                MemberName = raw.Name,
                AccountNo = raw.AccountNo,
                ChequeNo = raw.ChequeNo,
                ChequeDate = null,       // SP has no separate cheque-issue date
                ChequeDateBs = null,
                WithdrawalDate = raw.Date.HasValue ? raw.Date.Value.ToString("yyyy-MM-dd") : null,
                WithdrawalDateBs = dateFormatted,
                Status = raw.Status,
                Remarks = null,
                BranchName = null,       // not returned by this SP
                Operator = null,
                LastModifiedOn = dateFormatted,
                LastModifiedBy = null,
                WithdrawalAmount = null, // not returned by this SP
                ChequeWithdrawStatus = raw.Status
            };
        }

        /// <summary>
        /// Order-by filter as a switch statement, per request.
        /// </summary>
        private static string BuildOrderByClause(string orderBy)
        {
            switch (orderBy)
            {
                case "Member Name":
                    return " ORDER BY Name";
                case "Member Id":
                    return " ORDER BY substring(m.MemberId, 1,(len(m.MemberId)-charindex('-', m.MemberId))-1), m.MemberId";
                case "Account No":
                    return " ORDER BY substring(a.AccountNo, 1,(len(a.AccountNo)-charindex('-', a.AccountNo))-1), a.AccountNo";
                case "Cheque No":
                    return " ORDER BY c.ChequeNo";
                case "Date":
                    return " ORDER BY c.LastModifiedOn";
                case "Status":
                    return " ORDER BY s.ChequeWithdrawStatus";
                default:
                    // Default groups by member/account first so the view's grouping
                    // (Member -> Account No -> cheque rows) comes out contiguous.
                    return " ORDER BY m.MemberId, a.AccountNo, c.ChequeNo";
            }
        }

        /// <summary>
        /// Exact shape of what sp_5_43_GetChequeWithdrawl's SELECT list returns.
        /// Internal to this repository only.
        /// </summary>
        private class SpRawRow
        {
            public long? ChequeNo { get; set; }
            public DateTime? Date { get; set; }
            public string? Status { get; set; }
            public string? AccountNo { get; set; }
            public string? MemberId { get; set; }
            public string? Name { get; set; }
        }
    }
}