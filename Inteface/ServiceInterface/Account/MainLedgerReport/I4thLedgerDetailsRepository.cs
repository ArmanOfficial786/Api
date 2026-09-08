
using global::NexgenCosysReport.Dtos.RequestDtos.Account.MainLedgerReport;
// Inteface/ServiceInterface/Account/OthersReport/ILedgerDetailsRepository.cs

namespace NexgenCosysReport.Inteface.ServiceInterface.Account.MainLedgerReport
{
    public interface I4thLedgerDetailsRepository
    {
        Task<LedgerDetailsData> GetReportDataAsync(LedgerDetailsReqResponse request);
    }
}