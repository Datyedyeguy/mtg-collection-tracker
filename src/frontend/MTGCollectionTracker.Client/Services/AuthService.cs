using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using MTGCollectionTracker.Shared.DTOs.Auth;

namespace MTGCollectionTracker.Client.Services;

/// <summary>
/// Implementation of IAuthService for calling backend authentication endpoints.
/// </summary>
public class AuthService : IAuthService
{
    private readonly HttpClient _httpClient;
    private readonly ITokenStorageService _tokenStorage;
    private readonly CustomAuthStateProvider _authStateProvider;
    private readonly NavigationManager _navigationManager;

    public AuthService(
        HttpClient httpClient,
        ITokenStorageService tokenStorage,
        CustomAuthStateProvider authStateProvider,
        NavigationManager navigationManager)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _tokenStorage = tokenStorage ?? throw new ArgumentNullException(nameof(tokenStorage));
        _authStateProvider = authStateProvider ?? throw new ArgumentNullException(nameof(authStateProvider));
        _navigationManager = navigationManager ?? throw new ArgumentNullException(nameof(navigationManager));
    }

    /// <inheritdoc />
    public async Task<(bool success, string? error)> LoginAsync(LoginRequest request)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("/api/auth/login", request);

            if (response.IsSuccessStatusCode)
            {
                var authResponse = await response.Content.ReadFromJsonAsync<AuthResponse>();
                if (authResponse == null)
                {
                    return (false, "Invalid response from server");
                }

                // Store tokens in localStorage
                await _tokenStorage.SaveTokensAsync(authResponse.AccessToken, authResponse.RefreshToken);

                // Notify authentication state provider
                _authStateProvider.NotifyUserAuthentication(authResponse.AccessToken);

                return (true, null);
            }

            // Handle specific error status codes
            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                return (false, "Invalid email or password");
            }

            if (response.StatusCode == HttpStatusCode.BadRequest)
            {
                // Try to parse validation errors from response
                var errorMessage = await TryParseErrorMessageAsync(response);
                return (false, errorMessage ?? "Invalid login request");
            }

            // Generic error for other status codes
            return (false, $"Login failed: {response.StatusCode}");
        }
        catch (HttpRequestException)
        {
            return (false, "Unable to connect to server. Please check your connection.");
        }
        catch (Exception ex)
        {
            return (false, $"An unexpected error occurred: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public async Task<(bool success, string? error)> RegisterAsync(RegisterRequest request)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("/api/auth/register", request);

            if (response.IsSuccessStatusCode)
            {
                var authResponse = await response.Content.ReadFromJsonAsync<AuthResponse>();
                if (authResponse == null)
                {
                    return (false, "Invalid response from server");
                }

                // Store tokens in localStorage
                await _tokenStorage.SaveTokensAsync(authResponse.AccessToken, authResponse.RefreshToken);

                // Notify authentication state provider
                _authStateProvider.NotifyUserAuthentication(authResponse.AccessToken);

                return (true, null);
            }

            // Handle specific error status codes
            if (response.StatusCode == HttpStatusCode.BadRequest)
            {
                // Try to parse validation errors from response
                var errorMessage = await TryParseErrorMessageAsync(response);
                return (false, errorMessage ?? "Invalid registration request");
            }

            if (response.StatusCode == HttpStatusCode.Conflict)
            {
                return (false, "An account with this email already exists");
            }

            // Generic error for other status codes
            return (false, $"Registration failed: {response.StatusCode}");
        }
        catch (HttpRequestException)
        {
            return (false, "Unable to connect to server. Please check your connection.");
        }
        catch (Exception ex)
        {
            return (false, $"An unexpected error occurred: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public async Task LogoutAsync()
    {
        // Clear stored tokens
        await _tokenStorage.ClearTokensAsync();

        // Notify authentication state provider
        _authStateProvider.NotifyUserLogout();

        // Redirect to home page
        _navigationManager.NavigateTo("/", forceLoad: true);
    }

    /// <summary>
    /// Attempts to parse a user-friendly error message from the HTTP response.
    /// </summary>
    private async Task<string?> TryParseErrorMessageAsync(HttpResponseMessage response)
    {
        try
        {
            var content = await response.Content.ReadAsStringAsync();
            if (string.IsNullOrWhiteSpace(content))
            {
                return null;
            }

            // Try to parse as JSON error response
            using var doc = JsonDocument.Parse(content);

            // Check for ASP.NET Core validation error format
            if (doc.RootElement.TryGetProperty("errors", out var errors))
            {
                // Collect all error messages
                var errorMessages = new System.Collections.Generic.List<string>();
                foreach (var errProp in errors.EnumerateObject())
                {
                    if (errProp.Value.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var errMsg in errProp.Value.EnumerateArray())
                        {
                            errorMessages.Add(errMsg.GetString() ?? "Validation error");
                        }
                    }
                }
                return errorMessages.Count > 0 ? string.Join(", ", errorMessages) : null;
            }

            // Check for simple error message
            if (doc.RootElement.TryGetProperty("message", out var messageProperty))
            {
                return messageProperty.GetString();
            }

            if (doc.RootElement.TryGetProperty("error", out var errProperty))
            {
                return errProperty.GetString();
            }

            return null;
        }
        catch
        {
            // If parsing fails, return null to use default error message
            return null;
        }
    }
}
