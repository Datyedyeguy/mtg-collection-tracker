using System;
using Microsoft.AspNetCore.Identity;

namespace MTGCollectionTracker.Data.Entities;

/// <summary>
/// Custom user entity that extends IdentityUser with application-specific properties.
/// </summary>
/// <remarks>
/// IdentityUser already provides:
/// - Id (string, GUID by default)
/// - UserName (our login identifier)
/// - Email
/// - PasswordHash
/// - SecurityStamp (changes when credentials change, invalidates old tokens)
/// - And many more (PhoneNumber, TwoFactorEnabled, LockoutEnd, etc.)
///
/// We extend it with properties specific to our MTG collection app.
/// </remarks>
public class ApplicationUser : IdentityUser
{
    /// <summary>
    /// Display name shown in the UI (can be different from UserName).
    /// </summary>
    public string? DisplayName { get; set; }

    /// <summary>
    /// When the user account was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When the user profile was last updated.
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
