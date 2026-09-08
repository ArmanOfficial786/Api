using NexgenCosysReport.Dtos.RequestDtos.Common;

namespace NexgenCosysReport.Inteface.ServiceInterface.Common
{
    public interface IVoucher
    {
        Task<List<VoucherOptionResponse>> GetVoucherListAsync(VoucherListRequest request, CancellationToken cancellationToken = default);

        Task<VoucherOptionResponse?> GetByVoucherNoAsync(string voucherNo, CancellationToken cancellationToken = default);
    }
}
