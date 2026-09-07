// Inteface/ServiceInterface/Account/OtherReports/IVoucherDetailsRepository.cs
using NexgenCosysReport.Dtos.RequestDtos.Account.OtherReports;
using static NexgenCosysReport.Dtos.RequestDtos.Account.OtherReports.VoucherDetailsRequestDto;

namespace NexgenCosysReport.Inteface.ServiceInterface.Account.OtherReports
{
    public interface IVoucherDetailsRepository
    {
        Task<VoucherDetailsData> GetVoucherDetailsDataAsync(VoucherDetailsRequestDto request);
    }
}