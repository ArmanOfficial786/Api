// Inteface/ServiceInterface/Account/SubLedgerDetailsReport/ISubLedgerDetailsRepository.cs
using NexgenCosysReport.Dtos.RequestDtos.Account.SubLedgerDetailsReport;

namespace NexgenCosysReport.Inteface.ServiceInterface.Account.SubLedgerDetailsReport
{
    public interface ISubLedgerDetailsRepository
    {
        Task<SubLedgerDetailsData> GetReportDataAsync(SubLedgerDetailsRequestDto request);
    }
}