using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.Authorization;

namespace MTGCollectionTracker.Client.Services;

/// <summary>
/// Custom authentication state provider for Blazor WebAssembly.
/// Reads JWT tokens from storage and provides authentication state to Blazor components.
/// </summary>
public class CustomAuthStateProvider : AuthenticationStateProvider
{
    private readonly ITokenStorageService _tokenStorage;

    public CustomAuthStateProvider(ITokenStorageService tokenStorage)
    {
        _tokenStorage = tokenStorage ?? throw new ArgumentNullException(nameof(tokenStorage));
    }

    /// <summary>
    /// Gets the current authentication state by reading and parsing the access token.
    /// Called by Blazor whenever it needs to check if user is authenticated.
    /// </summary>
    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        var token = await _tokenStorage.GetAccessTokenAsync();

        if (string.IsNullOrWhiteSpace(token))
        {
            // No token = anonymous user
            return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
        }

        try
        {
            // Parse JWT and extract claims
            var claims = ParseClaimsFromJwt(token);
            
            // Check if token is expired
            var expiryClaim = claims.FirstOrDefault(c => c.Type == "exp");
            if (expiryClaim != null)
            {
                var expiryUnixTime = long.Parse(expiryClaim.Value);
                var expiryDateTime = DateTimeOffset.FromUnixTimeSeconds(expiryUnixTime);
                
                if (expiryDateTime < DateTimeOffset.UtcNow)
                {
                    // Token expired - treat as anonymous
                    await _tokenStorage.ClearTokensAsync();
                    return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
                }
            }

            // Create authenticated user identity
            var identity = new ClaimsIdentity(claims, "jwt");
            var user = new ClaimsPrincipal(identity);
            
            return new AuthenticationState(user);
        }
        catch
        {
            // Invalid token format - treat as anonymous
            await _tokenStorage.ClearTokensAsync();
            return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
        }
    }

    /// <summary>
    /// Notifies Blazor that a user has logged in.
    /// Call this after successful login/registration.
    /// </summary>
    /// <param name="token">The JWT access token</param>
    public void NotifyUserAuthentication(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
            throw new ArgumentException("Token cannot be null or empty.", nameof(token));

        var claims = ParseClaimsFromJwt(token);
        var identity = new ClaimsIdentity(claims, "jwt");
        var user = new ClaimsPrincipal(identity);

        var authState = Task.FromResult(new AuthenticationState(user));
        NotifyAuthenticationStateChanged(authState);
    }

    /// <summary>
    /// Notifies Blazor that a user has logged out.
    /// Call this after logout.
    /// </summary>
    public void NotifyUserLogout()
    {
        var anonymousUser = new ClaimsPrincipal(new ClaimsIdentity());
        var authState = Task.FromResult(new AuthenticationState(anonymousUser));
        NotifyAuthenticationStateChanged(authState);
    }

    /// <summary>
    /// Parses a JWT token and extracts claims from the payload.
    /// JWT format: header.payload.signature (Base64Url encoded)
    /// </summary>
    private static IEnumerable<Claim> ParseClaimsFromJwt(string jwt)
    {
        var claims = new List<Claim>();
        
        // JWT is in format: header.payload.signature
        var parts = jwt.Split('.');
        if (parts.Length != 3)
            throw new ArgumentException("Invalid JWT format - expected 3 parts separated by dots");

        // Decode the payload (middle part)
        var payload = parts[1];
        
        // Base64Url to Base64: replace characters and add padding
        var base64 = payload.Replace('-', '+').Replace('_', '/');
        switch (base64.Length % 4)
        {
            case 2: base64 += "=="; break;
            case 3: base64 += "="; break;
        }

        // Decode Base64 to JSON
        var jsonBytes = Convert.FromBase64String(base64);
        var keyValuePairs = JsonSerializer.Deserialize<Dictionary<string, object>>(jsonBytes);

        if (keyValuePairs == null)
            return claims;

        // Convert JSON claims to .NET Claim objects
        foreach (var kvp in keyValuePairs)
        {
            // Handle array values (like roles)
            if (kvp.Value is JsonElement element && element.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in element.EnumerateArray())
                {
                    claims.Add(new Claim(kvp.Key, item.ToString()));
                }
            }
            else
            {
                claims.Add(new Claim(kvp.Key, kvp.Value?.ToString() ?? string.Empty));
            }
        }

        return claims;
    }
}
