// Inteface/ServiceInterface/Account/IBTReports/IIBTTransactionRepository.cs
using NexgenCosysReport.Dtos.RequestDtos.Account.IBTReports;

namespace NexgenCosysReport.Inteface.ServiceInterface.Account.IBTReports
{
    public interface IIBTTransactionRepository
    {
        Task<IBTTransactionData> GetReportDataAsync(IBTTransactionRequestDto request);
    }
}