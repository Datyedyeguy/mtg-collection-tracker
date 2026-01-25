namespace MTGCollectionTracker.Api.Configuration;

/// <summary>
/// Configuration settings for JWT token generation and validation.
/// Bound from appsettings.json "JwtSettings" section.
/// </summary>
public class JwtSettings
{
    /// <summary>
    /// Configuration section name in appsettings.json.
    /// </summary>
    public const string SectionName = "JwtSettings";

    /// <summary>
    /// Secret key used to sign tokens. Must be at least 32 characters.
    /// IMPORTANT: Use a strong, unique secret in production.
    /// Do NOT commit real secrets to appsettings.json.
    /// </summary>
    /// <remarks>
    /// Configuration binding follows the standard ASP.NET Core configuration hierarchy:
    /// appsettings.json &lt; appsettings.{Environment}.json &lt; User Secrets (dev only) &lt; environment variables &lt; Azure Key Vault.
    ///
    /// Development:
    /// - Keep a placeholder or dummy value in appsettings.json if needed.
    /// - Override with .NET User Secrets so nothing sensitive is committed to Git:
    ///   dotnet user-secrets set "JwtSettings:Secret" "your-strong-dev-secret"
    ///
    /// Production / staging:
    /// - Prefer environment variables (e.g. JwtSettings__Secret) or an Azure Key Vault-backed configuration provider.
    /// - These values will override anything in appsettings.json at runtime.
    /// </remarks>
    public string Secret { get; set; } = string.Empty;

    /// <summary>
    /// Token issuer (who created the token). Typically your API URL.
    /// </summary>
    public string Issuer { get; set; } = string.Empty;

    /// <summary>
    /// Token audience (who the token is for). Typically your frontend URL.
    /// </summary>
    public string Audience { get; set; } = string.Empty;

    /// <summary>
    /// Access token lifetime in minutes. Default: 15 minutes.
    /// Short-lived for security.
    /// </summary>
    public int AccessTokenExpiryMinutes { get; set; } = 15;

    /// <summary>
    /// Refresh token lifetime in days. Default: 7 days.
    /// Longer-lived, stored in database, can be revoked.
    /// </summary>
    public int RefreshTokenExpiryDays { get; set; } = 7;
}
