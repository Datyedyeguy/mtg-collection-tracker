using System.Threading.Tasks;
using MTGCollectionTracker.Shared.DTOs.Auth;

namespace MTGCollectionTracker.Client.Services;

/// <summary>
/// Service for handling authentication operations (login, register, logout).
/// </summary>
public interface IAuthService
{
    /// <summary>
    /// Attempts to log in a user with the provided credentials.
    /// </summary>
    /// <param name="request">Login credentials</param>
    /// <returns>Tuple with success flag and optional error message</returns>
    Task<(bool success, string? error)> LoginAsync(LoginRequest request);

    /// <summary>
    /// Attempts to register a new user account.
    /// </summary>
    /// <param name="request">Registration details</param>
    /// <returns>Tuple with success flag and optional error message</returns>
    Task<(bool success, string? error)> RegisterAsync(RegisterRequest request);

    /// <summary>
    /// Logs out the current user by clearing stored tokens and redirecting to home.
    /// </summary>
    Task LogoutAsync();
}
