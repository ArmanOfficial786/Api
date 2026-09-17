using NexgenCosysReport.Dtos.RequestDtos.Common;

namespace NexgenCosysReport.Inteface.ServiceInterface.Common
{
    public interface IPaymentDurationType
    {
        Task<List<PaymentDurationTypeResponse>> GetAllAsync();
    }
}
