using NexgenCosysReport.Dtos.RequestDtos.Common;

namespace NexgenCosysReport.Inteface.ServiceInterface.Common
{
    public interface IDepositStatementVerification
    {
        Task<List<DepositStatementVerificationDto>> GetVerificationHistory(long mamAccountOpeningId);
        Task<DateTime?> GetMaxTransactionDateForVerify(long mamAccountOpeningId);
        Task<(bool Success, string Message)> CreateVerification(DepositStatementVerifyRequestDto request, long userId);
        Task<VerificationStatusDto> GetVerificationStatus(long mamAccountOpeningId);
        Task<bool> IsAlreadyVerified(long mamAccountOpeningId, string verifiedTillDateBs);
        Task<long> GetAccountIdByAccountNo(string accountNo);
        Task<decimal?> GetInterestPayable(long accountId);
        Task<dynamic?> GetAccountInfo(string accountNo);
    }
}
