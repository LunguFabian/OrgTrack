using System.Security.Claims;

namespace OrgTrack.Api.Extensions;

public static class ClaimsPrincipalExtensions
{
    /// <summary>
    /// Extracts the logged-in user's ID (Guid) from the JWT token.
    /// Our JWT stores the ID in the "sub" (Subject) claim.
    /// </summary>
    public static Guid GetUserId(this ClaimsPrincipal user)
    {
        var userIdStr = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.FindFirstValue("sub");
        
        if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
        {
            throw new UnauthorizedAccessException("User ID nu a fost găsit în token-ul de autentificare.");
        }

        return userId;
    }
}
