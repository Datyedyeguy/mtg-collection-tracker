using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using MTGCollectionTracker.Api.Configuration;
using MTGCollectionTracker.Data.Entities;

namespace MTGCollectionTracker.Api.Services;

/// <summary>
/// Service for generating and validating JWT tokens.
/// </summary>
public interface IJwtService
{
    /// <summary>
    /// Generate a JWT access token for a user.
    /// </summary>
    string GenerateAccessToken(ApplicationUser user);

    /// <summary>
    /// Generate a secure random refresh token.
    /// </summary>
    string GenerateRefreshToken();

    /// <summary>
    /// Get the principal from an expired token (for refresh flow).
    /// </summary>
    ClaimsPrincipal? GetPrincipalFromExpiredToken(string token);

    /// <summary>
    /// Get token expiration time.
    /// </summary>
    DateTime GetAccessTokenExpiry();

    /// <summary>
    /// Get refresh token expiration time.
    /// </summary>
    DateTime GetRefreshTokenExpiry();

    /// <summary>
    /// Hash a refresh token for secure storage.
    /// </summary>
    string HashToken(string token);
}

/// <summary>
/// Implementation of JWT token generation and validation.
/// </summary>
public class JwtService : IJwtService
{
    private readonly JwtSettings _settings;
    private readonly SymmetricSecurityKey _signingKey;

    public JwtService(IOptions<JwtSettings> settings)
    {
        _settings = settings.Value;

        // Validate settings
        if (string.IsNullOrEmpty(_settings.Secret) || _settings.Secret.Length < 32)
        {
            throw new InvalidOperationException(
                "JWT Secret must be at least 32 characters. Check appsettings.json.");
        }

        _signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.Secret));
    }

    /// <inheritdoc />
    public string GenerateAccessToken(ApplicationUser user)
    {
        var claims = new List<Claim>
        {
            // Standard claims
            new(JwtRegisteredClaimNames.Sub, user.Id),
            new(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(),
                ClaimValueTypes.Integer64),

            // Custom claims
            new("displayName", user.DisplayName ?? user.Email ?? string.Empty),
        };

        var credentials = new SigningCredentials(_signingKey, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _settings.Issuer,
            audience: _settings.Audience,
            claims: claims,
            expires: GetAccessTokenExpiry(),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    /// <inheritdoc />
    public string GenerateRefreshToken()
    {
        // Generate a cryptographically secure random token
        var randomBytes = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);
        return Convert.ToBase64String(randomBytes);
    }

    /// <inheritdoc />
    public ClaimsPrincipal? GetPrincipalFromExpiredToken(string token)
    {
        var tokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            ValidateLifetime = false, // We want to validate expired tokens
            ValidIssuer = _settings.Issuer,
            ValidAudience = _settings.Audience,
            IssuerSigningKey = _signingKey
        };

        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out var securityToken);

            // Ensure it's a valid JWT with correct algorithm
            if (securityToken is not JwtSecurityToken jwtToken ||
                !jwtToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256,
                    StringComparison.InvariantCultureIgnoreCase))
            {
                return null;
            }

            return principal;
        }
        catch
        {
            return null;
        }
    }

    /// <inheritdoc />
    public DateTime GetAccessTokenExpiry()
    {
        return DateTime.UtcNow.AddMinutes(_settings.AccessTokenExpiryMinutes);
    }

    /// <inheritdoc />
    public DateTime GetRefreshTokenExpiry()
    {
        return DateTime.UtcNow.AddDays(_settings.RefreshTokenExpiryDays);
    }

    /// <inheritdoc />
    public string HashToken(string token)
    {
        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(token));
        return Convert.ToBase64String(hashBytes);
    }
}
