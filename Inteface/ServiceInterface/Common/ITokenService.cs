using NexgenCosysReport.Models;
using System.Security.Claims;

namespace NexgenCosysReport.Inteface.ServiceInterface.Common;

/// <summary>
/// Interface for token generation service
/// </summary>
public interface ITokenService
{
    string GenerateToken(UsmUser user, string officeIds, string userTypeName);
    int GetExpiryMinutes();
    long? GetUserIdFromPrincipal(ClaimsPrincipal principal);
}
