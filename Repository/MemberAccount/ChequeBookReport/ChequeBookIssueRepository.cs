// Repositories/MemberAccount/ChequeBookReport/ChequeBookIssueRepository.cs
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using NexgenCosysReport.DbContext;
using NexgenCosysReport.Dtos.RequestDtos.MemberAccount.ChequeBookReport;
using NexgenCosysReport.Inteface.ServiceInterface.Common;
using NexgenCosysReport.Inteface.ServiceInterface.MemberAccount.ChequeBookReport;
using System.Data;
using System.Text;

namespace NexgenCosysReport.Repositories.MemberAccount.ChequeBookReport
{
    public class ChequeBookIssueRepository : IChequeBookIssueRepository
    {
        private readonly AppDbContext _context;
        private readonly IDateConverterService _dateConverter;
        private readonly ILogger<ChequeBookIssueRepository> _logger;

        public ChequeBookIssueRepository(
            AppDbContext context,
            IDateConverterService dateConverter,
            ILogger<ChequeBookIssueRepository> logger)
        {
            _context = context;
            _dateConverter = dateConverter;
            _logger = logger;
        }

        public async Task<MemberInfoDto?> GetMemberByMemberIdAsync(string memberId)
        {
            try
            {
                var sql = @"
                    SELECT 
                        MemMemberRegistrationId as MemberRegistrationId,
                        MemberId,
                        CONCAT(FirstName, ' ', ISNULL(MiddleName, ''), ' ', LastName) as FullName
                    FROM MemMemberRegistration
                    WHERE MemberId = @MemberId AND IsActive = 1";

                var parameters = new DynamicParameters();
                parameters.Add("@MemberId", memberId, DbType.String);

                var connectionString = _context.Database.GetConnectionString();
                using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();

                return await connection.QueryFirstOrDefaultAsync<MemberInfoDto>(
                    sql,
                    parameters,
                    commandType: CommandType.Text
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetMemberByMemberIdAsync for MemberId: {MemberId}", memberId);
                throw;
            }
        }

        public async Task<ChequeBookIssueData> GetReportDataAsync(ChequeBookIssueRequestDto request)
        {
            try
            {
                var sqlFilterExp = new StringBuilder();

                if (request.MemberId != -1)
                {
                    sqlFilterExp.Append($" AND m.MemMemberRegistrationId = {request.MemberId}");
                }

                if (request.ReportView == "Date" &&
                    !string.IsNullOrEmpty(request.FromDateBs) && request.FromDateBs != "-1" &&
                    !string.IsNullOrEmpty(request.ToDateBs) && request.ToDateBs != "-1")
                {
                    var fromDateAd = await _dateConverter.NepaliToEnglishAsync(request.FromDateBs);
                    var toDateAd = await _dateConverter.NepaliToEnglishAsync(request.ToDateBs);
                    var fromDateStr = fromDateAd.ToString("yyyy-MM-dd");
                    var toDateStr = toDateAd.ToString("yyyy-MM-dd");

                    sqlFilterExp.Append($" AND c.ChequeIssueOn BETWEEN '{fromDateStr}' AND '{toDateStr}'");
                }

                if (!string.IsNullOrEmpty(request.BranchIds) && request.BranchIds != "-1")
                {
                    sqlFilterExp.Append($" AND c.UsmOfficeId IN ({request.BranchIds})");
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

                var rows = await connection.QueryAsync<ChequeBookIssueRowDto>(
                    "sp_5_43_GetChequeBookIssue",
                    parameters,
                    commandType: CommandType.StoredProcedure
                );

                var resultList = rows.AsList();

                return new ChequeBookIssueData
                {
                    Rows = resultList,
                    TotalRecords = resultList.Count,
                    TotalChequesIssued = resultList.Sum(r => r.TotalCheques ?? 0),
                    FromDateBs = request.FromDateBs,
                    ToDateBs = request.ToDateBs,
                    BranchNames = request.BranchName,
                    OrderBy = request.OrderBy,
                    MemberId = request.MemberIdText,
                    MemberName = request.MemberName,
                    ReportView = request.ReportView
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetReportDataAsync for Cheque Book Issue Report");
                throw;
            }
        }

        private static string BuildOrderByClause(string orderBy)
        {
            string result = " ORDER BY c.ChequeIssueOnBs";

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
            else if (orderBy == "Cheque Issue Date")
            {
                result = " ORDER BY c.ChequeIssueOnBs";
            }
            else if (orderBy == "Cheque No From")
            {
                result = " ORDER BY c.ChequeNoFrom";
            }

            return result;
        }
    }
}