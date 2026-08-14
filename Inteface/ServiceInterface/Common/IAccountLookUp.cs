using NexgenCosysReport.Dtos.RequestDtos.Common;

namespace NexgenCosysReport.Inteface.ServiceInterface.Common
{
    public interface IAccountLookUp
    {
        /// <summary>
        /// Grid behind the AccountDirectory (AC) modal popup.
        /// Accepts pagination, filtering, and sorting via Filter.
        /// </summary>
        Task<Pagination<AccountLookUpDtos>> GetAccountListAsync(Filter filter, long userId);

        /// <summary>
        /// Fired when the user clicks "Sel" on a grid row (AccountDirectoryAccountSearch_RowCommand).
        /// </summary>
        Task<AccountSelectedDto?> GetSelectedAccountAsync(long mamAccountOpeningId, long userId);

        /// <summary>
        /// Mirrors txtAccountNo_TextChanged: validates a typed account no. AND checks the
        /// logged-in user's office authorization for that account.
        /// </summary>
        Task<AccountValidationResult> ValidateAccountNoAsync(string accountNo, long userId);
    }

    public class AccountValidationResult
    {
        public bool IsValid { get; set; }
        public string? Message { get; set; }
        public AccountSelectedDto? Account { get; set; }
    }
}
