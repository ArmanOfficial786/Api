// Inteface/ServiceInterface/AccountOperation/OthersReport/IBankReceivedPaymentRepository.cs
using NexgenCosysReport.Dtos.RequestDtos.Account.OthersReport;
//using NexgenCosysReport.Dtos.RequestDtos.AccountOperation.OthersReport;

namespace NexgenCosysReport.Inteface.ServiceInterface.AccountOperation.OthersReport
{
    public interface IBankReceivedPaymentRepository
    {
        Task<BankReceivedPaymentData> GetReportDataAsync(BankReceivedPaymentRequestDto request);
    }
}