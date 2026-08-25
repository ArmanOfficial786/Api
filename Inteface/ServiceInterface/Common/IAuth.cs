using NexgenCosysReport.Dtos.RequestDtos.Common;

namespace NexgenCosysReport.Inteface.ServiceInterface.Common
{
    public interface IAuth
    {
        Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);
        Task LogoutAsync(long userId, CancellationToken cancellationToken = default);
    }
}
