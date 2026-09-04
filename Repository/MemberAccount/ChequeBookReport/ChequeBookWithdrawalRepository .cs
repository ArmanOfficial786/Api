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

                var rows = await connection.QueryAsync<ChequeBookWithdrawalRowDto>(
                    "sp_5_43_GetChequeWithdrawl",
                    parameters,
                    commandType: CommandType.StoredProcedure
                );

                var resultList = rows.AsList();

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

        private static string BuildOrderByClause(string orderBy)
        {
            string result = " ORDER BY c.ChequeNo";

            if (orderBy == "Member Name")
            {
                result = " ORDER BY Name";
            }
            else if (orderBy == "Member Id")
            {
                result = " ORDER BY substring(m.MemberId, 1,(len(m.MemberId)-charindex('-', m.MemberId))-1), m.MemberId";
            }
            else if (orderBy == "Account No")
            {
                result = " ORDER BY substring(a.AccountNo, 1,(len(a.AccountNo)-charindex('-', a.AccountNo))-1), a.AccountNo";
            }
            else if (orderBy == "Cheque No")
            {
                result = " ORDER BY c.ChequeNo";
            }
            else if (orderBy == "Date")
            {
                result = " ORDER BY c.LastModifiedOn";
            }
            else if (orderBy == "Status")
            {
                result = " ORDER BY s.ChequeWithdrawStatus";
            }

            return result;
        }
    }
}