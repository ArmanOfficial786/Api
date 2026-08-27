// src/Modules/UserManagement/UserManagement.Infrastructure/Services/TokenService.cs
using Microsoft.IdentityModel.Tokens;
using NexgenCosysReport.Inteface.ServiceInterface.Common;
using NexgenCosysReport.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace NexgenCosysReport.Services.CommonService;

public class TokenService : ITokenService
{
    private readonly IConfiguration _config;

    public TokenService(IConfiguration config)
    {
        _config = config;
    }

    public string GenerateToken(UsmUser user, string officeIds, string userTypeName)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.ASCII.GetBytes(_config["Jwt:Key"]!);

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Email, user.LoginEmailAddress ?? string.Empty),
            new Claim("UserId", user.UsmUserId.ToString()),
            new Claim("FullName", user.FullName ?? string.Empty),
            new Claim("UserTypeId", user.UsmUserTypeId.ToString()),
            new Claim(ClaimTypes.Role, userTypeName ?? string.Empty),
            new Claim("GenderId", user.UsmGenderId.ToString()),
            new Claim("OfficeId", user.UsmOfficeId.ToString()),
            new Claim("OfficeIds", officeIds ?? string.Empty)
        };
        var expiryMinutes = _config.GetValue<int?>("Jwt:ExpiryMinutes") ?? 30;
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Issuer = _config["Jwt:Issuer"],
            Audience = _config["Jwt:Audience"],
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddMinutes(expiryMinutes),
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }
    public int GetExpiryMinutes()
    {
        return _config.GetValue<int?>("Jwt:ExpiryMinutes") ?? 30;
    }

    public long? GetUserIdFromPrincipal(ClaimsPrincipal user)
    {
        var userIdClaim = user.FindFirst("UserId")?.Value;
        if (string.IsNullOrEmpty(userIdClaim))
            userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (long.TryParse(userIdClaim, out var id))
            return id;

        return null;
    }
}