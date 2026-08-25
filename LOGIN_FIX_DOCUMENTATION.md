# Login Issue Fix - "User already logged in!" False Positive

## Problem
The API was returning a **401 Unauthorized** error with the message **"User already logged in!"** even when the user had not logged in previously.

## Root Cause
The session validation logic in `AuthRepository.LoginAsync()` was too strict. It was checking:
```csharp
if (lastLogin is not null && lastLogin.LogOutOn == null)
	throw new Exception("User already logged in!");
```

This logic **rejected ALL login attempts** if there was ANY previous login record without a logout time, without considering:
1. Whether the previous session had **expired** due to timeout
2. Whether the session was actually **still active**

## Solution
The fix implements a **session timeout validation** that:

1. **Allows login** if:
   - No previous session exists, OR
   - Previous session has a valid `LogOutOn` timestamp, OR
   - Previous session has **exceeded the timeout period** (≥30 minutes old)

2. **Rejects login** only if:
   - A previous session exists AND is not logged out AND is still within the timeout window

3. **Auto-cleanup**: When a previous session has timed out, it automatically sets `LogOutOn` to mark it as expired

## Changes Made

### File: `Repository/Common/AuthRepository.cs` (Lines 210-238)

**Before:**
```csharp
// 7. Single active session enforcement
var lastLogin = await _context.UsmLogins
	.Where(l => l.UsmUserId == user.UsmUserId)
	.OrderByDescending(l => l.UsmLoginId)
	.FirstOrDefaultAsync(cancellationToken);

if (lastLogin is not null && lastLogin.LogOutOn == null)
	throw new Exception("User already logged in!");
```

**After:**
```csharp
// 7. Single active session enforcement with session timeout validation
var lastLogin = await _context.UsmLogins
	.Where(l => l.UsmUserId == user.UsmUserId)
	.OrderByDescending(l => l.UsmLoginId)
	.FirstOrDefaultAsync(cancellationToken);

// Allow login if:
// 1. No previous session exists, OR
// 2. Previous session was logged out, OR
// 3. Previous session has timed out (> 30 minutes old without logout)
if (lastLogin is not null && lastLogin.LogOutOn == null)
{
	// Check if the previous session has exceeded timeout (30 minutes in WebForms, adjust as needed)
	var sessionTimeoutMinutes = 30;
	var sessionExpiryTime = lastLogin.LoginInOn.AddMinutes(sessionTimeoutMinutes);

	// If the previous session is still within timeout window, reject the new login
	if (DateTime.Now < sessionExpiryTime)
	{
		throw new Exception("User already logged in! Please logout from your previous session or wait for session to expire.");
	}
	else
	{
		// Session has timed out, update the previous login record to reflect logout
		lastLogin.LogOutOn = lastLogin.LoginInOn.AddMinutes(sessionTimeoutMinutes);
		_context.UsmLogins.Update(lastLogin);
		await _context.SaveChangesAsync(cancellationToken);
	}
}
```

## Configuration
The session timeout is currently set to **30 minutes** (standard for web applications). If your application uses a different timeout value:

**Locate and update:**
```csharp
var sessionTimeoutMinutes = 30; // Change this to match your SessionState timeout
```

This should match the value in your `web.config` (for WebForms) or session configuration:
```xml
<sessionState timeout="30" />
```

## Testing
To test the fix:

1. **Fresh Login** - Should work without "already logged in" error
2. **Double Login (same browser/IP within timeout)** - Should be rejected
3. **Stale Session Login (>30 min later)** - Should allow new login and auto-cleanup old session

## Related Code
- **Logout implementation**: Already correctly implemented in `LogoutAsync()` at line 284
- **Session validation**: Now properly handles timeout scenarios in `LoginAsync()`

## Notes
- The timeout validation uses `LoginInOn` timestamp to determine session age
- Expired sessions are auto-marked with `LogOutOn` for audit trail
- The error message was improved to provide guidance: "Please logout from your previous session or wait for session to expire"
