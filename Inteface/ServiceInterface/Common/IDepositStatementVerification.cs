//using NexgenCosysReport.Dtos.RequestDtos.Common;

//namespace NexgenCosysReport.Inteface.ServiceInterface.Common
//{
//    public interface IDepositStatementVerification
//    {
//        Task<List<DepositStatementVerificationDto>> GetVerificationHistory(long mamAccountOpeningId);
//        Task<DateTime?> GetMaxTransactionDateForVerify(long mamAccountOpeningId);
//        Task<(bool Success, string Message)> CreateVerification(DepositStatementVerifyRequestDto request, long userId);
//        Task<VerificationStatusDto> GetVerificationStatus(long mamAccountOpeningId);
//        Task<bool> IsAlreadyVerified(long mamAccountOpeningId, string verifiedTillDateBs);
//        Task<long> GetAccountIdByAccountNo(string accountNo);
//        Task<decimal?> GetInterestPayable(long accountId);
//        Task<dynamic?> GetAccountInfo(string accountNo);
//    }
//}




// Inteface/ServiceInterface/Common/IDepositStatementVerification.cs
using NexgenCosysReport.Dtos.RequestDtos.Common;

namespace NexgenCosysReport.Inteface.ServiceInterface.Common
{
    public interface IDepositStatementVerification
    {
        /// <summary>Audit/detail view — history scoped strictly to one account opening.</summary>
        Task<List<DepositStatementVerificationDto>> GetVerificationHistory(long mamAccountOpeningId);

        Task<DateTime?> GetMaxTransactionDateForVerify(long mamAccountOpeningId);

        Task<(bool Success, string Message)> CreateVerification(DepositStatementVerifyRequestDto request, long userId);

        /// <summary>
        /// Public contract stays keyed by mamAccountOpeningId (matches the generated client),
        /// but internally resolves AccountNo and scopes the lookup the way the WebForm does
        /// (cMamDepositStatementVerification.GetByAccountNo). Returns null if the account
        /// opening doesn't exist.
        /// </summary>
        Task<VerificationStatusDto?> GetVerificationStatus(long mamAccountOpeningId);

        Task<dynamic?> GetAccountInfo(string accountNo);
        Task<dynamic?> GetAccountInfoById(long mamAccountOpeningId);
        Task<long> GetAccountIdByAccountNo(string accountNo);
        Task<decimal?> GetInterestPayable(long accountId);
    }
}