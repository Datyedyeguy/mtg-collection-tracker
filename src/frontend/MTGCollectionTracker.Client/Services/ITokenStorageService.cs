using System.Threading.Tasks;

namespace MTGCollectionTracker.Client.Services;

/// <summary>
/// Service for storing and retrieving JWT authentication tokens from browser storage.
/// </summary>
public interface ITokenStorageService
{
    /// <summary>
    /// Retrieves the access token from storage.
    /// </summary>
    /// <returns>The access token, or null if not found.</returns>
    Task<string?> GetAccessTokenAsync();

    /// <summary>
    /// Retrieves the refresh token from storage.
    /// </summary>
    /// <returns>The refresh token, or null if not found.</returns>
    Task<string?> GetRefreshTokenAsync();

    /// <summary>
    /// Stores both access and refresh tokens in browser storage.
    /// </summary>
    /// <param name="accessToken">The JWT access token.</param>
    /// <param name="refreshToken">The refresh token.</param>
    Task SaveTokensAsync(string accessToken, string refreshToken);

    /// <summary>
    /// Removes all stored tokens from browser storage.
    /// Used during logout.
    /// </summary>
    Task ClearTokensAsync();
}
