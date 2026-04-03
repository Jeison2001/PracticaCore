using System.Security.Claims;
using Domain.Common.Users;

namespace Domain.Common.Extensions;

/// <summary>
/// Extension methods for ClaimsPrincipal to extract user information from JWT claims.
/// </summary>
public static class ClaimsPrincipalExtensions
{
    /// <summary>
    /// Extracts the full user information from the JWT claims.
    /// </summary>
    /// <param name="principal">The ClaimsPrincipal from the HTTP context.</param>
    /// <returns>A CurrentUserInfo object containing all user data from the token.</returns>
    public static CurrentUserInfo GetCurrentUserInfo(this ClaimsPrincipal? principal)
    {
        if (principal == null)
            return new CurrentUserInfo { UserId = null };

        var userIdClaim = principal.FindFirst("sub")?.Value
                       ?? principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        int? userId = null;
        if (!string.IsNullOrEmpty(userIdClaim))
        {
            int.TryParse(userIdClaim, out var parsed);
            userId = parsed != 0 ? parsed : null;
        }

        return new CurrentUserInfo
        {
            UserId = userId,
            Email = principal.FindFirst("email")?.Value
                 ?? principal.FindFirst(ClaimTypes.Email)?.Value,
            FirstName = principal.FindFirst("firstName")?.Value,
            LastName = principal.FindFirst("lastName")?.Value
        };
    }

    /// <summary>
    /// Extracts the user ID from the "sub" (JWT standard) or NameIdentifier claim.
    /// </summary>
    /// <param name="principal">The ClaimsPrincipal from the HTTP context.</param>
    /// <returns>The user ID as an integer, or null if not found or not parseable.</returns>
    public static int? GetUserId(this ClaimsPrincipal? principal)
    {
        return principal.GetCurrentUserInfo().UserId;
    }

    /// <summary>
    /// Extracts the user ID, throwing if the user is not authenticated.
    /// </summary>
    /// <param name="principal">The ClaimsPrincipal from the HTTP context.</param>
    /// <returns>The user ID as an integer.</returns>
    /// <exception cref="UnauthorizedAccessException">Thrown when user ID is not available.</exception>
    public static int RequireUserId(this ClaimsPrincipal principal)
    {
        var userId = principal.GetUserId();
        if (!userId.HasValue)
        {
            throw new UnauthorizedAccessException("No se pudo identificar el usuario");
        }
        return userId.Value;
    }
}
