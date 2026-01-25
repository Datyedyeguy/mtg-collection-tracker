using System.ComponentModel.DataAnnotations;

namespace MTGCollectionTracker.Shared.DTOs.Auth;

/// <summary>
/// Request model for refreshing an access token.
/// </summary>
public class RefreshTokenRequest
{
    /// <summary>
    /// The refresh token obtained during login.
    /// </summary>
    [Required(ErrorMessage = "Refresh token is required")]
    public string RefreshToken { get; set; } = string.Empty;
}
