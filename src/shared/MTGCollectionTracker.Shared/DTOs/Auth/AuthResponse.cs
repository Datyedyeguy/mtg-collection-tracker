using System;

namespace MTGCollectionTracker.Shared.DTOs.Auth;

/// <summary>
/// Response model returned after successful authentication.
/// </summary>
public class AuthResponse
{
    /// <summary>
    /// JWT access token for API authorization.
    /// Short-lived (15 minutes by default).
    /// </summary>
    public string AccessToken { get; set; } = string.Empty;

    /// <summary>
    /// Refresh token for obtaining new access tokens.
    /// Long-lived (7 days by default).
    /// </summary>
    public string RefreshToken { get; set; } = string.Empty;

    /// <summary>
    /// When the access token expires (UTC).
    /// </summary>
    public DateTime ExpiresAt { get; set; }

    /// <summary>
    /// User's unique identifier.
    /// </summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// User's email address.
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// User's display name (if set).
    /// </summary>
    public string? DisplayName { get; set; }
}
