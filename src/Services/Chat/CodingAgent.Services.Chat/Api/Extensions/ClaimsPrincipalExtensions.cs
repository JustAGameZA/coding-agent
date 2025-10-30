using System.Security.Claims;

namespace CodingAgent.Services.Chat.Api.Extensions;

public static class ClaimsPrincipalExtensions
{
    /// <summary>
    /// Gets the user ID from the JWT claims.
    /// Looks for NameIdentifier or sub claims.
    /// </summary>
    /// <param name="principal">The claims principal</param>
    /// <returns>The user ID as a Guid</returns>
    /// <exception cref="InvalidOperationException">If no valid user ID claim is found</exception>
    public static Guid GetUserId(this ClaimsPrincipal principal)
    {
        if (principal?.Identity?.IsAuthenticated != true)
        {
            throw new InvalidOperationException("User is not authenticated");
        }

        // Try NameIdentifier first (standard JWT claim)
        var userIdClaim = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value
                         ?? principal.FindFirst("sub")?.Value
                         ?? principal.FindFirst("user_id")?.Value;

        if (string.IsNullOrEmpty(userIdClaim))
        {
            throw new InvalidOperationException("User ID claim not found in JWT token");
        }

        if (!Guid.TryParse(userIdClaim, out var userId))
        {
            throw new InvalidOperationException($"Invalid user ID format: {userIdClaim}");
        }

        return userId;
    }

    /// <summary>
    /// Gets the user ID from the JWT claims, returning null if not found.
    /// </summary>
    /// <param name="principal">The claims principal</param>
    /// <returns>The user ID as a Guid, or null if not found/invalid</returns>
    public static Guid? TryGetUserId(this ClaimsPrincipal principal)
    {
        try
        {
            return GetUserId(principal);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Gets the user's email from the JWT claims.
    /// </summary>
    /// <param name="principal">The claims principal</param>
    /// <returns>The user's email address</returns>
    public static string? GetUserEmail(this ClaimsPrincipal principal)
    {
        return principal?.FindFirst(ClaimTypes.Email)?.Value
               ?? principal?.FindFirst("email")?.Value;
    }

    /// <summary>
    /// Gets the user's name from the JWT claims.
    /// </summary>
    /// <param name="principal">The claims principal</param>
    /// <returns>The user's name</returns>
    public static string? GetUserName(this ClaimsPrincipal principal)
    {
        return principal?.FindFirst(ClaimTypes.Name)?.Value
               ?? principal?.FindFirst("name")?.Value
               ?? principal?.FindFirst("preferred_username")?.Value;
    }

    /// <summary>
    /// Checks if the user has a specific role.
    /// </summary>
    /// <param name="principal">The claims principal</param>
    /// <param name="role">The role to check</param>
    /// <returns>True if the user has the specified role</returns>
    public static bool HasRole(this ClaimsPrincipal principal, string role)
    {
        return principal?.IsInRole(role) == true;
    }
}