// Inteface/ServiceInterface/Account/SecondLedgerDetailsReport/ISecondLedgerDetailsRepository.cs
using NexgenCosysReport.Dtos.RequestDtos.Account.SecondLedgerDetailsReport;

namespace NexgenCosysReport.Inteface.ServiceInterface.Account.SecondLedgerDetailsReport
{
    public interface ISecondLedgerDetailsRepository
    {
        Task<SecondLedgerDetailsData> GetSecondLedgerDetailsDataAsync(SecondLedgerDetailsRequestDto request);
    }
}