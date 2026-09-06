// Inteface/ServiceInterface/Account/OtherReports/IVoucherDetailsRepository.cs
using NexgenCosysReport.Dtos.RequestDtos.Account.OtherReports;

namespace NexgenCosysReport.Inteface.ServiceInterface.Account.OtherReports
{
    public interface IVoucherDetailsRepository
    {
        Task<VoucherDetailsData> GetVoucherDetailsDataAsync(VoucherDetailsRequestDto request);
    }
}