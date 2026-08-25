// Repository/Common/AccountLookUpRepository.cs
using Microsoft.EntityFrameworkCore;
using NexgenCosysReport.DbContext;
using NexgenCosysReport.Dtos.RequestDtos.Common;
using NexgenCosysReport.Inteface.ServiceInterface.Common;

namespace NexgenCosysReport.Repository.Common
{
    public class AccountLookUpRepository : IAccountLookUp
    {
        private readonly AppDbContext _context;
        private const int FIXED_PAGE_SIZE = 10;

        // MamAccountStatusId values kept consistent with CMamAccountOpening:
        // 1 = Active, 2 = Closed, 3 = Renewed, 4 = Active(other), 5 = Disabled/Locked
        private static readonly long[] ValidStatusIds = { 1, 2, 4, 5 };

        public AccountLookUpRepository(AppDbContext context)
        {
            _context = context;
        }

        // -- 1. Paginated + filtered grid, restricted to the user's offices -----
        public async Task<Pagination<AccountLookUpDtos>> GetAccountListAsync(
            Filter filter, long userId)
        {
            var officeIds = _context.UsmRelationUserToOffices
                .Where(r => r.UsmUserId == userId)
                .Select(r => r.UsmOfficeId);

            var query =
                from ao in _context.MamAccountOpenings
                where ao.IsDeleted == false
                      && ao.MamAccountStatusId != null
                      && ValidStatusIds.Contains(ao.MamAccountStatusId.Value)
                      && officeIds.Contains(ao.UsmOfficeId)
                join dt in _context.SycDepositTypes on ao.SycDepositTypeId equals dt.SycDepositTypeId
                join mr in _context.MemMemberRegistrations on ao.MemMemberRegistrationId equals mr.MemMemberRegistrationId
                join off in _context.UsmOffices on ao.UsmOfficeId equals off.UsmOfficeId
                join aht in _context.MamAccountHolderTypes on ao.MamAccountHolderTypeId equals aht.MamAccountHolderTypeId into ahtJoin
                from aht in ahtJoin.DefaultIfEmpty()
                join st in _context.MamAccountStatuses on ao.MamAccountStatusId equals st.MamAccountStatusId into stJoin
                from st in stJoin.DefaultIfEmpty()
                select new AccountLookUpDtos
                {
                    MamAccountOpeningId = ao.MamAccountOpeningId,
                    MemberId = mr.MemberId,
                    MemberName = (mr.FirstName + " " + mr.MiddleName + " " + mr.LastName).Trim(),
                    AccountNo = ao.AccountNo,
                    DepositType = dt.DepositTypeName,
                    AccountType = aht != null ? aht.AccountHolder : null,
                    InterestRate = ao.InterestRate ?? dt.InterestRate,
                    OpenedDate = ao.AccountOpenOnBs,
                    MaturityDate = ao.MaturityOnBs,
                    Status = st != null ? st.AccountStatus : null,
                    UsmOfficeId = ao.UsmOfficeId,
                    OfficeName = off.OfficeName
                };

            // Apply generic filters from client
            query = query.ApplyFilterParams(filter.Params);

            // Apply sort or default sort
            var sorts = filter.Sort is { Count: > 0 }
                ? filter.Sort
                : [new SortParam(nameof(AccountLookUpDtos.MamAccountOpeningId), SortOrder.Desc)];
            query = query.ApplySortParams(sorts);

            // Paginate and return
            return await query.ToPaginationAsync(filter);
        }

        // -- 2. Row select (AccountDirectoryAccountSearch_RowCommand) -----------
        public async Task<AccountSelectedDto?> GetSelectedAccountAsync(long mamAccountOpeningId, long userId)
        {
            var officeIds = _context.UsmRelationUserToOffices
                .Where(r => r.UsmUserId == userId)
                .Select(r => r.UsmOfficeId);

            var result = await (
                from ao in _context.MamAccountOpenings
                where ao.MamAccountOpeningId == mamAccountOpeningId
                      && officeIds.Contains(ao.UsmOfficeId)
                join mr in _context.MemMemberRegistrations on ao.MemMemberRegistrationId equals mr.MemMemberRegistrationId
                select new AccountSelectedDto
                {
                    MamAccountOpeningId = ao.MamAccountOpeningId,
                    AccountNo = ao.AccountNo,
                    MemMemberRegistrationId = ao.MemMemberRegistrationId,
                    MemberId = mr.MemberId,
                    // AccountNamingOption mirrors the code-behind override for named accounts
                    MemberName = ao.AccountNamingOption
                        ? (ao.AccountName ?? string.Empty)
                        : (mr.FirstName + " " + mr.MiddleName + " " + mr.LastName).Trim(),
                    UsmOfficeId = ao.UsmOfficeId,
                    AccountNamingOption = ao.AccountNamingOption,
                    AccountName = ao.AccountName
                }
            ).SingleOrDefaultAsync();

            return result;
        }

        // -- 3. Direct account-no entry + authorization check -------------------
        //     (mirrors txtAccountNo_TextChanged in DepositStatementReport.aspx.cs)
        public async Task<AccountValidationResult> ValidateAccountNoAsync(string accountNo, long userId)
        {
            if (string.IsNullOrWhiteSpace(accountNo))
            {
                return new AccountValidationResult
                {
                    IsValid = false,
                    Message = "Please Enter Correct Account No"
                };
            }

            // GetMamAccountOpeningByAccountNoList -> order by id, take last if duplicates
            var matches = await _context.MamAccountOpenings
                .Where(a => a.AccountNo == accountNo && a.IsDeleted == false)
                .OrderBy(a => a.MamAccountOpeningId)
                .ToListAsync();

            if (!matches.Any())
            {
                return new AccountValidationResult
                {
                    IsValid = false,
                    Message = "Please Enter Correct Account No"
                };
            }

            var account = matches.Count > 1 ? matches.Last() : matches.First();

            var hasOfficeAccess = await _context.UsmRelationUserToOffices
                .AnyAsync(r => r.UsmUserId == userId && r.UsmOfficeId == account.UsmOfficeId);

            if (!hasOfficeAccess)
            {
                return new AccountValidationResult
                {
                    IsValid = false,
                    Message = "You Don't have Authority to view the Statement of current Account No"
                };
            }

            var member = await _context.MemMemberRegistrations
                .SingleOrDefaultAsync(m => m.MemMemberRegistrationId == account.MemMemberRegistrationId);

            if (member == null)
            {
                return new AccountValidationResult
                {
                    IsValid = false,
                    Message = "Member details not found for this account."
                };
            }

            var memberName = account.AccountNamingOption
                ? account.AccountName
                : (member.FirstName + " " + member.MiddleName + " " + member.LastName).Trim();

            return new AccountValidationResult
            {
                IsValid = true,
                Account = new AccountSelectedDto
                {
                    MamAccountOpeningId = account.MamAccountOpeningId,
                    AccountNo = account.AccountNo,
                    MemMemberRegistrationId = account.MemMemberRegistrationId,
                    MemberId = member.MemberId,
                    MemberName = memberName ?? string.Empty,
                    UsmOfficeId = account.UsmOfficeId,
                    AccountNamingOption = account.AccountNamingOption,
                    AccountName = account.AccountName
                }
            };
        }
    }
}
