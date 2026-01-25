using System;

namespace MTGCollectionTracker.Data.Entities;

/// <summary>
/// Represents a refresh token used to obtain new access tokens.
/// Stored in database to allow revocation and tracking.
/// </summary>
public class RefreshToken
{
    /// <summary>
    /// Unique identifier for this refresh token.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// The SHA256 hash of the refresh token. The plaintext token is only sent to the client.
    /// </summary>
    public string TokenHash { get; set; } = string.Empty;

    /// <summary>
    /// Foreign key to the user who owns this token.
    /// </summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// Navigation property to the user.
    /// </summary>
    public ApplicationUser User { get; set; } = null!;

    /// <summary>
    /// When the token expires.
    /// </summary>
    public DateTime ExpiresAt { get; set; }

    /// <summary>
    /// When the token was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When the token was revoked (null if still valid).
    /// </summary>
    public DateTime? RevokedAt { get; set; }

    /// <summary>
    /// IP address of the client that created this token.
    /// </summary>
    public string? CreatedByIp { get; set; }

    /// <summary>
    /// IP address of the client that revoked this token.
    /// </summary>
    public string? RevokedByIp { get; set; }

    /// <summary>
    /// The hash of the token that replaced this one (if rotated).
    /// </summary>
    public string? ReplacedByTokenHash { get; set; }

    /// <summary>
    /// Reason for revocation.
    /// </summary>
    public string? RevokedReason { get; set; }

    /// <summary>
    /// Whether this token is currently valid (not expired and not revoked).
    /// </summary>
    public bool IsActive => RevokedAt == null && DateTime.UtcNow < ExpiresAt;
}
