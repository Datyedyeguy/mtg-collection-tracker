using System;
using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace MTGCollectionTracker.Client.Services;

/// <summary>
/// Implementation of ITokenStorageService using browser's localStorage.
/// </summary>
public class TokenStorageService : ITokenStorageService
{
    private readonly IJSRuntime _jsRuntime;

    // localStorage keys
    private const string AccessTokenKey = "accessToken";
    private const string RefreshTokenKey = "refreshToken";

    public TokenStorageService(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime ?? throw new ArgumentNullException(nameof(jsRuntime));
    }

    /// <inheritdoc />
    public async Task<string?> GetAccessTokenAsync()
    {
        try
        {
            return await _jsRuntime.InvokeAsync<string?>("localStorage.getItem", AccessTokenKey);
        }
        catch (JSException)
        {
            // localStorage not available (rare, but possible in private browsing)
            return null;
        }
    }

    /// <inheritdoc />
    public async Task<string?> GetRefreshTokenAsync()
    {
        try
        {
            return await _jsRuntime.InvokeAsync<string?>("localStorage.getItem", RefreshTokenKey);
        }
        catch (JSException)
        {
            return null;
        }
    }

    /// <inheritdoc />
    public async Task SaveTokensAsync(string accessToken, string refreshToken)
    {
        if (string.IsNullOrWhiteSpace(accessToken))
            throw new ArgumentException("Access token cannot be null or empty.", nameof(accessToken));

        if (string.IsNullOrWhiteSpace(refreshToken))
            throw new ArgumentException("Refresh token cannot be null or empty.", nameof(refreshToken));

        try
        {
            await _jsRuntime.InvokeVoidAsync("localStorage.setItem", AccessTokenKey, accessToken);
            await _jsRuntime.InvokeVoidAsync("localStorage.setItem", RefreshTokenKey, refreshToken);
        }
        catch (JSException ex)
        {
            throw new InvalidOperationException("Failed to save tokens to localStorage.", ex);
        }
    }

    /// <inheritdoc />
    public async Task ClearTokensAsync()
    {
        try
        {
            await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", AccessTokenKey);
            await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", RefreshTokenKey);
        }
        catch (JSException)
        {
            // Ignore errors during clear (tokens might not exist)
        }
    }
}
