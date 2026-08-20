using NexgenCosysReport.Dtos.RequestDtos.MemberAccount;

namespace NexgenCosysReport.Inteface.ServiceInterface.MemberAccount
{
    public interface IDepositeStatement
    {
        Task<DepositStatementData> GetDepositStatement(DepositStatementRequestDto request);
        Task<(decimal OpeningBalance, decimal ClosingBalance, List<DepositStatementRowDto> Rows)> GetDepositStatementData(
            string fromDate, string toDate, string accountNo, bool enableInterest, bool entryBy, string language, bool customNarration);
        Task<DepositStatementMemberDetailDto?> GetMemberDetails(long accountId, string language);
        Task<(decimal Interest, decimal Tax, decimal ClosingBalance)> GetInterestAndTax(long mamAccountOpeningId, string toDate);
        Task<long> GetAccountIdByAccountNo(string accountNo);
        Task<dynamic?> GetOfficeInfo(long accountId);
        Task<string?> GetLatestVerification(string accountNo);
        Task<decimal?> GetInterestPayable(long accountId);
    }
}
