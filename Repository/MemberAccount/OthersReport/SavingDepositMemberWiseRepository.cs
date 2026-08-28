// Repository/MemberAccount/OthersReport/SavingDepositMemberWiseRepository.cs
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
    public class SavingDepositMemberWiseRepository : ISavingDepositMemberWiseRepository
    {
        private readonly AppDbContext _context;
        private readonly IDateConverterService _dateConverter;
        private readonly ILogger<SavingDepositMemberWiseRepository> _logger;

        public SavingDepositMemberWiseRepository(
            AppDbContext context,
            IDateConverterService dateConverter,
            ILogger<SavingDepositMemberWiseRepository> logger)
        {
            _context = context;
            _dateConverter = dateConverter;
            _logger = logger;
        }

        public async Task<SavingDepositMemberWiseData> GetReportDataAsync(SavingDepositMemberWiseRequestDto request)
        {
            try
            {
                // Build filter expression
                var sqlFilterExp = new StringBuilder();

                // Member filter
                if (request.MemberId.HasValue && request.MemberId.Value != -1)
                {
                    sqlFilterExp.Append($" AND t.MemMemberRegistrationId = {request.MemberId.Value}");
                }

                // Date filter - only for DateWise mode
                if (request.ReportMode == "DateWise" &&
                    !string.IsNullOrEmpty(request.FromDateBs) && request.FromDateBs != "-1" &&
                    !string.IsNullOrEmpty(request.ToDateBs) && request.ToDateBs != "-1")
                {
                    var fromDateAd = await _dateConverter.NepaliToEnglishAsync(request.FromDateBs);
                    var toDateAd = await _dateConverter.NepaliToEnglishAsync(request.ToDateBs);
                    var fromDateStr = fromDateAd.ToString("yyyy-MM-dd");
                    var toDateStr = toDateAd.ToString("yyyy-MM-dd");
                    sqlFilterExp.Append($" AND t.TransactionOn BETWEEN '{fromDateStr}' AND '{toDateStr}'");
                }

                // Branch filter
                if (!string.IsNullOrEmpty(request.BranchIds) && request.BranchIds != "-1")
                {
                    sqlFilterExp.Append($" AND t.UsmOfficeId IN ({request.BranchIds})");
                }

                // Saving Type filter
                if (request.SavingTypeId.HasValue && request.SavingTypeId.Value != -1)
                {
                    sqlFilterExp.Append($" AND a.SycDepositTypeId = {request.SavingTypeId.Value}");
                }

                // Transaction Type filter
                if (!string.IsNullOrEmpty(request.TransactionType))
                {
                    // The stored procedure handles Deposit vs Withdrawl internally
                    // We pass the type as a parameter
                }

                // Build Order By clause
                var orderByClause = BuildOrderByClause(request.OrderBy);

                // Determine stored procedure based on report mode and transaction type
                string spName = "sp_5_43_GetSavingDepositMemberWise";

                var parameters = new DynamicParameters();
                parameters.Add("@SqlFilterExp", sqlFilterExp.ToString() + orderByClause, DbType.String, size: -1);
                parameters.Add("@SqlChequeId", "-1", DbType.String);

                var connectionString = _context.Database.GetConnectionString();
                using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();

                var rows = await connection.QueryAsync<SavingDepositMemberWiseRowDto>(
                    spName,
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

                // Get saving type name if selected
                string? savingTypeName = null;
                if (request.SavingTypeId.HasValue && request.SavingTypeId.Value != -1)
                {
                    savingTypeName = await GetSavingTypeNameAsync(request.SavingTypeId.Value);
                }

                // Calculate totals
                var totalDeposit = resultList
                    .Where(r => r.TransactionType == "Deposit")
                    .Sum(r => r.Amount ?? 0);
                var totalWithdrawal = resultList
                    .Where(r => r.TransactionType == "Withdrawl")
                    .Sum(r => r.Amount ?? 0);

                return new SavingDepositMemberWiseData
                {
                    Rows = resultList,
                    TotalRecords = resultList.Count,
                    TotalAmount = resultList.Sum(r => r.Amount ?? 0),
                    FromDateBs = request.FromDateBs,
                    ToDateBs = request.ToDateBs,
                    BranchNames = request.BranchName,
                    OrderBy = request.OrderBy,
                    TransactionType = request.TransactionType,
                    ReportMode = request.ReportMode,
                    SelectedMemberId = selectedMemberId,
                    SelectedMemberName = selectedMemberName,
                    SavingTypeName = savingTypeName,
                    TotalDepositAmount = totalDeposit,
                    TotalWithdrawalAmount = totalWithdrawal
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetReportDataAsync for Saving Deposit Member Wise Report");
                throw;
            }
        }

        private static string BuildOrderByClause(string orderBy)
        {
            return orderBy switch
            {
                "Member Id" => " ORDER BY substring(MemberId, 1,(len(MemberId)-charindex('-', MemberId))-1), MemberId",
                "Member Name" => " ORDER BY MemberName",
                "Account No" => " ORDER BY substring(AccountNo, 1,(len(AccountNo)-charindex('-', AccountNo))-1), AccountNo",
                "Saving Type" => " ORDER BY savingType",
                "Date" => " ORDER BY Date",
                "Amount" => " ORDER BY Amount DESC",
                _ => " ORDER BY MemberId"
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

        private async Task<string?> GetSavingTypeNameAsync(long savingTypeId)
        {
            try
            {
                const string query = @"
                    SELECT DepositTypeName 
                    FROM SycDepositType 
                    WHERE SycDepositTypeId = @SavingTypeId AND IsActive = 1";

                var connectionString = _context.Database.GetConnectionString();
                using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();

                return await connection.QueryFirstOrDefaultAsync<string>(
                    query,
                    new { SavingTypeId = savingTypeId }
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting saving type name for ID: {SavingTypeId}", savingTypeId);
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