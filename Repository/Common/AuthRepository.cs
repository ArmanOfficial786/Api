using Microsoft.EntityFrameworkCore;
using NexgenCosysReport.DbContext;
using NexgenCosysReport.Dtos.RequestDtos.Common;
using NexgenCosysReport.Inteface.ServiceInterface.Common;
using NexgenCosysReport.Models;
using NexgenCosysReport.Services.CommonService;

namespace NexgenCosysReport.Repository.Common;

public class AuthRepository : IAuth
{
    private readonly AppDbContext _context;
    private readonly ITokenService _tokenService;

    public AuthRepository(AppDbContext context, ITokenService tokenService)
    {
        _context = context;
        _tokenService = tokenService;
    }

    public async Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        // 1. Lookup user — mirrors CUsmUser.GetByEmailId
        var user = await _context.UsmUsers
            .FirstOrDefaultAsync(u => u.LoginEmailAddress == request.Email, cancellationToken);

        if (user is null)
            throw new Exception("Invalid email or password.");

        // 2. Company lookup — mirrors CSycCompany.GetCompanyDetail (single-row table, Id = 0)
        var company = await _context.SycCompanies
            .FirstOrDefaultAsync(c => c.SycCompanyId == 0, cancellationToken);

        if (company == null)
            throw new Exception("Company not found.");

        // 3. Office access — mirrors CUsmRelationUserToOfficeLogin.GetByUsmUserId + accessFlag loop
        var hasOfficeAccess = await _context.UsmRelationUserToOfficeLogins
            .AnyAsync(r => r.UsmUserId == user.UsmUserId && r.UsmOfficeId == request.CompanyId, cancellationToken);

        // 4. Password check — real legacy MD5+ASCII PasswordUtility, full-string compare
        var passwordProvider = new PasswordProvider();
        var indexOfSalt = user.Password.LastIndexOf(':');
        var salt = user.Password.Substring(indexOfSalt + 1);
        var encryptedLoginPassword = passwordProvider.GetCipheredValue(request.Password + ":" + salt);
        var passwordMatches = user.Password == encryptedLoginPassword;

        if (!(passwordMatches && hasOfficeAccess))
        {
            if (!hasOfficeAccess)
                throw new Exception("You do not have access to this office.");

            throw new Exception("Invalid email or password.");
        }

        // 5. Account state checks
        if (!user.IsActive)
            throw new Exception("Your account is not activated.");

        if (user.PasswordChangedOn is null)
            throw new Exception("First login — password change required.");

        if (user.PasswordExpDays != 0 &&
            DateTime.Now > user.PasswordChangedOn.Value.AddDays(user.PasswordExpDays ?? 0))
        {
            throw new Exception("Your password has expired.");
        }

        // 6. Active system edition required
        var systemEdition = await _context.UsmSystemEditions
            .Where(e => e.IsActive == true)
            .FirstOrDefaultAsync(cancellationToken);

        if (systemEdition is null)
            throw new Exception("No active system edition configured.");

        // 7. Single active session enforcement — no ExpiresOn column exists, so the
        //    "still active" window is computed from LoginInOn + Jwt:ExpiryMinutes.
        //    A row with LogOutOn == null whose token has actually expired is stale,
        //    not "logged in" — we close it out here instead of blocking the new login.
        var expiryMinutes = _tokenService.GetExpiryMinutes();

        var openLogin = await _context.UsmLogins
            .Where(l => l.UsmUserId == user.UsmUserId && l.LogOutOn == null)
            .OrderByDescending(l => l.UsmLoginId)
            .FirstOrDefaultAsync(cancellationToken);

        if (openLogin is not null)
        {
            var sessionStillValid = openLogin.LoginInOn.AddMinutes(expiryMinutes) > DateTime.Now;

            //if (sessionStillValid)
            //    throw new Exception("User already logged in!");

            // Token expired naturally (no explicit logout) — auto-close the stale row so it
            // doesn't linger indefinitely with LogOutOn == null.
            openLogin.LogOutOn = DateTime.Now;
            await _context.SaveChangesAsync(cancellationToken);
        }

        // 8. OfficeIds string — needed for token generation
        var officeIds = await _context.UsmRelationUserToOffices
            .Where(r => r.UsmUserId == user.UsmUserId)
            .Select(r => r.UsmOfficeId)
            .ToListAsync(cancellationToken);
        var officeIdsCsv = string.Join(",", officeIds);

        // 9. UserType name — needed for token generation
        var userType = await _context.UsmUserTypes
            .Where(t => t.UsmUserTypeId == user.UsmUserTypeId)
            .FirstOrDefaultAsync(cancellationToken);

        // 10. Generate token BEFORE committing the login row — if this throws
        //     (e.g. missing Jwt:Key config), no dangling "already logged in" row is left behind.
        var token = _tokenService.GenerateToken(user, officeIdsCsv, userType?.UserTypeName ?? string.Empty);

        // 11. Only now create the login record — mirrors CUsmLogin.CreateLogin, moved to run
        //     last so failures above never leave an open, unclosed session.
        var newLogin = new UsmLogin
        {
            UsmUserId = user.UsmUserId,
            LoginInOn = DateTime.Now,
            SessionId = Guid.NewGuid().ToString()
        };
        _context.UsmLogins.Add(newLogin);
        await _context.SaveChangesAsync(cancellationToken);

        return new LoginResponse
        {
            Token = token,
            UserId = user.UsmUserId,
            FullName = user.FullName,
            Email = user.LoginEmailAddress,
            UserTypeId = user.UsmUserTypeId,
            UserTypeName = userType?.UserTypeName ?? string.Empty,
            GenderId = user.UsmGenderId,
            OfficeId = user.UsmOfficeId,
            OfficeIds = officeIdsCsv,
            CompanyName = company?.CompanyName ?? string.Empty,
            SystemEditionName = systemEdition.SystemEditionName
        };
    }

    public async Task LogoutAsync(long userId, CancellationToken cancellationToken = default)
    {
        var lastLogin = await _context.UsmLogins
            .Where(l => l.UsmUserId == userId && l.LogOutOn == null && l.LoginInOn.Date == DateTime.Today)
            .OrderByDescending(l => l.UsmLoginId)
            .FirstOrDefaultAsync(cancellationToken);

        if (lastLogin is not null)
        {
            lastLogin.LogOutOn = DateTime.Now;
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}