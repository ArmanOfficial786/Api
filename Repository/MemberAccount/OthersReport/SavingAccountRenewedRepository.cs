// Repository/MemberAccount/OthersReport/SavingAccountRenewedRepository.cs
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
    public class SavingAccountRenewedRepository : ISavingAccountRenewedRepository
    {
        private readonly AppDbContext _context;
        private readonly IDateConverterService _dateConverter;
        private readonly ILogger<SavingAccountRenewedRepository> _logger;

        public SavingAccountRenewedRepository(
            AppDbContext context,
            IDateConverterService dateConverter,
            ILogger<SavingAccountRenewedRepository> logger)
        {
            _context = context;
            _dateConverter = dateConverter;
            _logger = logger;
        }

        public async Task<SavingAccountRenewedData> GetReportDataAsync(SavingAccountRenewedRequestDto request)
        {
            try
            {
                // Build filter expression
                var sqlFilterExp = new StringBuilder();

                // Member filter
                if (request.MemberId.HasValue && request.MemberId.Value != -1)
                {
                    sqlFilterExp.Append($" AND m.MemMemberRegistrationId = {request.MemberId.Value}");
                }

                // Date filter - for DateWise mode
                if (request.ReportMode == "DateWise" &&
                    !string.IsNullOrEmpty(request.FromDateBs) && request.FromDateBs != "-1" &&
                    !string.IsNullOrEmpty(request.ToDateBs) && request.ToDateBs != "-1")
                {
                    var fromDateAd = await _dateConverter.NepaliToEnglishAsync(request.FromDateBs);
                    var toDateAd = await _dateConverter.NepaliToEnglishAsync(request.ToDateBs);
                    var fromDateStr = fromDateAd.ToString("yyyy-MM-dd");
                    var toDateStr = toDateAd.ToString("yyyy-MM-dd");
                    sqlFilterExp.Append($" AND a.MaturityOn BETWEEN '{fromDateStr}' AND '{toDateStr}'");
                }

                // Branch filter
                if (!string.IsNullOrEmpty(request.BranchIds) && request.BranchIds != "-1")
                {
                    sqlFilterExp.Append($" AND a.UsmOfficeId IN ({request.BranchIds})");
                }

                // Build Order By clause
                var orderByClause = BuildOrderByClause(request.OrderBy);

                var parameters = new DynamicParameters();
                parameters.Add("@SqlFilterExp", sqlFilterExp.ToString() + orderByClause, DbType.String, size: -1);

                var connectionString = _context.Database.GetConnectionString();
                using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();

                var rows = await connection.QueryAsync<SavingAccountRenewedRowDto>(
                    "sp_5_43_GetSavingAccountRenewed",
                    parameters,
                    commandType: CommandType.StoredProcedure
                );

                var resultList = rows.AsList();

                // Get member details if specific member is selected
                string? selectedMemberId = null;
                string? selectedMemberName = null;
                if (request.MemberId.HasValue && request.MemberId.Value != -1)
                {
                    var member = await GetMemberDetailsAsync(request.MemberId.Value);
                    if (member != null)
                    {
                        selectedMemberId = member.MemberId;
                        selectedMemberName = member.MemberName;
                    }
                }

                return new SavingAccountRenewedData
                {
                    Rows = resultList,
                    TotalRecords = resultList.Count,
                    TotalBalance = resultList.Sum(r => r.AccountBalance ?? 0),
                    FromDateBs = request.FromDateBs,
                    ToDateBs = request.ToDateBs,
                    BranchNames = request.BranchName,
                    OrderBy = request.OrderBy,
                    ReportMode = request.ReportMode,
                    SelectedMemberId = selectedMemberId,
                    SelectedMemberName = selectedMemberName,
                    TotalRenewedAccounts = resultList.Count
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetReportDataAsync for Saving Account Renewed Report");
                throw;
            }
        }

        private static string BuildOrderByClause(string orderBy)
        {
            return orderBy switch
            {
                "Member Name" => " ORDER BY Name",
                "A‎/‎C Open Date" => " ORDER BY m.MemberId, a.AccountOpenOnBs",
                "A‎/‎C Renewed Date" => " ORDER BY m.MemberId, a.MaturityOnBs",
                "Account No" => " ORDER BY m.MemberId, substring(a.AccountNo, 1,(len(a.AccountNo)-charindex('-', a.AccountNo))-1), a.AccountNo",
                _ => " ORDER BY substring(m.MemberId, 1,(len(m.MemberId)-charindex('-', m.MemberId))-1), m.MemberId"
            };
        }

        private async Task<MemberDetailDto?> GetMemberDetailsAsync(long memberRegistrationId)
        {
            try
            {
                const string query = @"
                    SELECT 
                        MemberId,
                        CONCAT(FirstName, ' ', ISNULL(MiddleName, ''), ' ', LastName) AS MemberName
                    FROM MemMemberRegistration 
                    WHERE MemMemberRegistrationId = @MemberId";

                var connectionString = _context.Database.GetConnectionString();
                using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();

                return await connection.QueryFirstOrDefaultAsync<MemberDetailDto>(
                    query,
                    new { MemberId = memberRegistrationId }
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting member details for ID: {MemberId}", memberRegistrationId);
                return null;
            }
        }

        private class MemberDetailDto
        {
            public string? MemberId { get; set; }
            public string? MemberName { get; set; }
        }
    }
}