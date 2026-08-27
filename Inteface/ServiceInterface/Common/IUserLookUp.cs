using NexgenCosysReport.Dtos.RequestDtos.Common;

namespace NexgenCosysReport.Inteface.ServiceInterface.Common
{
    public interface IUserLookUp
    {
        Task<List<UserLookupResponse>> GetActiveUsersAsync(long loggedInUserId);
    }
}
