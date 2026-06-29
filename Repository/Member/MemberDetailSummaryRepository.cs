// Repository/Member/MemberDetailsSummaryRepository.cs
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using NexgenCosysReport.Dtos.RequestDtos.Member;
using NexgenCosysReport.Inteface.ServiceInterface.Common;
using NexgenCosysReport.Inteface.ServiceInterface.Member;
using System.Data;

namespace NexgenCosysReport.Repository.Member
{
    public class MemberDetailsSummaryRepository : IMemberDetailsSummary
    {
        private readonly AppDbContext _context;
        private readonly IDateConverterService _dateConverter;
        private readonly ILogger<MemberDetailsSummaryRepository> _logger;

        public MemberDetailsSummaryRepository(
            AppDbContext context,
            IDateConverterService dateConverter,
            ILogger<MemberDetailsSummaryRepository> logger)
        {
            _context = context;
            _dateConverter = dateConverter;
            _logger = logger;
        }

        public async Task<MemberDetailsSummarySpResponse> GetMemberDetailsSummary(MemberDetailsSummaryRequest request)
        {
            var connectionString = _context.Database.GetConnectionString();
            using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();

            // 1. Build filters
            var memberFilter = $" and MR.MemMemberRegistrationId = {request.MemberRegistrationId}";
            var shareFilter = $" and shm.MemMemberRegistrationId = {request.MemberRegistrationId}";
            var savingFilter = $" and mam.MemMemberRegistrationId = {request.MemberRegistrationId}";
            var loanFilter = $" And mR.MemMemberRegistrationId = {request.MemberRegistrationId}";
            var guaranteeFilter = $" and Mr.MemMemberRegistrationId = {request.MemberRegistrationId}";

            // 2. Convert Nepali dates to English (AD) and quote them
            string fromDateAd = await _dateConverter.BsToAdStringAsync(request.FromDate);
            string toDateAd = await _dateConverter.BsToAdStringAsync(request.ToDate);

            // Quote the AD dates (as original code did with Nepali dates)
            var fromDateQuoted = $"'{fromDateAd}'";
            var toDateQuoted = $"'{toDateAd}'";

            // 3. Execute each stored procedure

            // Member Info (no date params)
            var memberInfo = await connection.QueryFirstOrDefaultAsync<MemberDetailInfoDto>(
                "sp_4_11_GetMemberDetailReport",
                new { SqlFilterExp = memberFilter },
                commandType: CommandType.StoredProcedure);

            // Share Accounts
            var shareAccounts = await connection.QueryAsync<ShareAccountDto>(
                "sp_4_11_GetMemberDetailsSummaryForShare",
                new
                {
                    SqlFilterExp = shareFilter,
                    SqlFilterFromDate = fromDateQuoted,
                    SqlFilterTillDate = toDateQuoted
                },
                commandType: CommandType.StoredProcedure);

            // Saving Accounts
            var savingAccounts = await connection.QueryAsync<SavingAccountDto>(
                "sp_4_11_GetMemberDetailsSummaryForSaving",
                new
                {
                    SqlFilterExp = savingFilter,
                    SqlFilterFromDate = fromDateQuoted,
                    SqlFilterTillDate = toDateQuoted
                },
                commandType: CommandType.StoredProcedure);

            // Loan Issues
            var loanIssues = await connection.QueryAsync<LoanIssueDto>(
                "sp_4_11_GetMemberDetailsSummaryForLoan",
                new
                {
                    SqlFilterExp = loanFilter,
                    SqlFilterFromDate = fromDateQuoted,
                    SqlFilterTillDate = toDateQuoted
                },
                commandType: CommandType.StoredProcedure);

            // Group Guarantees (no date params)
            var groupGuarantees = await connection.QueryAsync<GroupGuaranteeDto>(
                "sp_4_11_ActiveLoanGuaranteerReport",
                new { SqlFilterExp = guaranteeFilter },
                commandType: CommandType.StoredProcedure);

            // Convert to lists
            var shareList = shareAccounts.AsList();
            var savingList = savingAccounts.AsList();
            var loanList = loanIssues.AsList();
            var guaranteeList = groupGuarantees.AsList();

            return new MemberDetailsSummarySpResponse
            {
                MemberInfo = memberInfo,
                ShareAccounts = shareList,
                SavingAccounts = savingList,
                LoanIssues = loanList,
                GroupGuarantees = guaranteeList,
                TotalShareRecords = shareList.Count,
                TotalSavingRecords = savingList.Count,
                TotalLoanRecords = loanList.Count,
                TotalGuaranteeRecords = guaranteeList.Count
            };
        }
    }
}