// Inteface/ServiceInterface/Account/OthersReport/IPaymentThroughSavingRepository.cs
using NexgenCosysReport.Dtos.RequestDtos.Account.OthersReport;

namespace NexgenCosysReport.Inteface.ServiceInterface.Account.OthersReport
{
    public interface IPaymentThroughSavingRepository
    {
        Task<PaymentThroughSavingData> GetReportDataAsync(PaymentThroughSavingRequestDto request);
    }
}
