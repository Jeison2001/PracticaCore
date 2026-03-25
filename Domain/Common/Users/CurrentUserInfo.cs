namespace Domain.Common.Users;

/// <summary>
/// Represents the current authenticated user's information extracted from JWT claims.
/// </summary>
public record CurrentUserInfo
{
    /// <summary>
    /// User ID extracted from the "sub" or NameIdentifier claim.
    /// </summary>
    public int? UserId { get; init; }

    /// <summary>
    /// Email extracted from the email claim.
    /// </summary>
    public string? Email { get; init; }

    /// <summary>
    /// First name extracted from the firstName claim.
    /// </summary>
    public string? FirstName { get; init; }

    /// <summary>
    /// Last name extracted from the lastName claim.
    /// </summary>
    public string? LastName { get; init; }

    /// <summary>
    /// Full name combining first and last name.
    /// </summary>
    public string FullName => string.Join(" ", new[] { FirstName, LastName }
        .Where(x => !string.IsNullOrEmpty(x)));

    /// <summary>
    /// Whether the user is authenticated (has a valid userId).
    /// </summary>
    public bool IsAuthenticated => UserId.HasValue;

    /// <summary>
    /// Creates an empty (unauthenticated) instance.
    /// </summary>
    public static CurrentUserInfo Anonymous => new() { UserId = null };
}
