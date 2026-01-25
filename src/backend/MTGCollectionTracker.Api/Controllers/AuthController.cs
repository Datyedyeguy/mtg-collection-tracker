using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MTGCollectionTracker.Api.Services;
using MTGCollectionTracker.Data;
using MTGCollectionTracker.Data.Entities;
using MTGCollectionTracker.Shared.DTOs.Auth;

namespace MTGCollectionTracker.Api.Controllers;

/// <summary>
/// Authentication endpoints for user registration, login, and token management.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IJwtService _jwtService;
    private readonly AppDbContext _dbContext;

    public AuthController(
        UserManager<ApplicationUser> userManager,
        IJwtService jwtService,
        AppDbContext dbContext)
    {
        _userManager = userManager;
        _jwtService = jwtService;
        _dbContext = dbContext;
    }

    /// <summary>
    /// Register a new user account.
    /// </summary>
    /// <param name="request">Registration details</param>
    /// <returns>Auth tokens on success</returns>
    [HttpPost("register")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        // Check if user already exists
        var existingUser = await _userManager.FindByEmailAsync(request.Email);
        if (existingUser != null)
        {
            return BadRequest(new { message = "Registration failed." });
        }

        // Create new user
        var user = new ApplicationUser
        {
            UserName = request.Email,  // Use email as username
            Email = request.Email,
            DisplayName = request.DisplayName
        };

        var result = await _userManager.CreateAsync(user, request.Password);

        if (!result.Succeeded)
        {
            var errors = result.Errors.Select(e => e.Description);
            return BadRequest(new { message = "Registration failed.", errors });
        }

        // Generate tokens
        var authResponse = await GenerateAuthResponse(user);
        return Ok(authResponse);
    }

    /// <summary>
    /// Login with email and password.
    /// </summary>
    /// <param name="request">Login credentials</param>
    /// <returns>Auth tokens on success</returns>
    [HttpPost("login")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        // Find user by email
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user == null)
        {
            return Unauthorized(new { message = "Invalid email or password." });
        }

        // Check if account is locked out
        if (await _userManager.IsLockedOutAsync(user))
        {
            return Unauthorized(new { message = "Account is locked. Please try again later." });
        }

        // Validate password
        var isValidPassword = await _userManager.CheckPasswordAsync(user, request.Password);
        if (!isValidPassword)
        {
            // Record failed login attempt
            await _userManager.AccessFailedAsync(user);
            return Unauthorized(new { message = "Invalid email or password." });
        }

        // Reset failed login count on successful login
        await _userManager.ResetAccessFailedCountAsync(user);

        // Generate tokens
        var authResponse = await GenerateAuthResponse(user);
        return Ok(authResponse);
    }

    /// <summary>
    /// Refresh an expired access token using a valid refresh token.
    /// </summary>
    /// <param name="request">Refresh token</param>
    /// <returns>New auth tokens on success</returns>
    [HttpPost("refresh")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequest request)
    {
        // Hash the incoming token to compare with stored hash
        var tokenHash = _jwtService.HashToken(request.RefreshToken);

        // Find the refresh token by hash
        var refreshToken = await _dbContext.RefreshTokens
            .Include(rt => rt.User)
            .FirstOrDefaultAsync(rt => rt.TokenHash == tokenHash);

        if (refreshToken == null)
        {
            return Unauthorized(new { message = "Invalid refresh token." });
        }

        if (!refreshToken.IsActive)
        {
            return Unauthorized(new { message = "Refresh token has expired or been revoked." });
        }

        // Revoke the old refresh token (token rotation for security)
        refreshToken.RevokedAt = DateTime.UtcNow;
        refreshToken.RevokedByIp = GetClientIpAddress();
        refreshToken.RevokedReason = "Replaced by new token";

        // Generate new tokens
        var authResponse = await GenerateAuthResponse(refreshToken.User);

        // Link old token to new one (store hash of replacement)
        refreshToken.ReplacedByTokenHash = _jwtService.HashToken(authResponse.RefreshToken);

        await _dbContext.SaveChangesAsync();

        return Ok(authResponse);
    }

    /// <summary>
    /// Logout and revoke the refresh token.
    /// </summary>
    /// <param name="request">Refresh token to revoke</param>
    [HttpPost("logout")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> Logout([FromBody] RefreshTokenRequest request)
    {
        // Hash the incoming token to compare with stored hash
        var tokenHash = _jwtService.HashToken(request.RefreshToken);

        var refreshToken = await _dbContext.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.TokenHash == tokenHash);

        if (refreshToken != null && refreshToken.IsActive)
        {
            refreshToken.RevokedAt = DateTime.UtcNow;
            refreshToken.RevokedByIp = GetClientIpAddress();
            refreshToken.RevokedReason = "Logged out by user";
            await _dbContext.SaveChangesAsync();
        }

        return Ok(new { message = "Logged out successfully." });
    }

    /// <summary>
    /// Get current user's profile. Requires authentication.
    /// </summary>
    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetCurrentUser()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub");

        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return Unauthorized();
        }

        return Ok(new
        {
            user.Id,
            user.Email,
            user.DisplayName,
            user.CreatedAt
        });
    }

    /// <summary>
    /// Generate access token, refresh token, and create auth response.
    /// </summary>
    private async Task<AuthResponse> GenerateAuthResponse(ApplicationUser user)
    {
        var accessToken = _jwtService.GenerateAccessToken(user);
        var refreshTokenValue = _jwtService.GenerateRefreshToken();
        var refreshTokenHash = _jwtService.HashToken(refreshTokenValue);

        // Store hashed refresh token in database (only the hash is stored)
        var refreshToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            TokenHash = refreshTokenHash,
            UserId = user.Id,
            ExpiresAt = _jwtService.GetRefreshTokenExpiry(),
            CreatedByIp = GetClientIpAddress()
        };

        _dbContext.RefreshTokens.Add(refreshToken);
        await _dbContext.SaveChangesAsync();

        return new AuthResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshTokenValue,
            ExpiresAt = _jwtService.GetAccessTokenExpiry(),
            UserId = user.Id,
            Email = user.Email ?? string.Empty,
            DisplayName = user.DisplayName
        };
    }

    /// <summary>
    /// Get the client's IP address from the request.
    /// </summary>
    private string? GetClientIpAddress()
    {
        // Check for forwarded IP (behind proxy/load balancer)
        var forwardedFor = HttpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwardedFor))
        {
            return forwardedFor.Split(',').First().Trim();
        }

        return HttpContext.Connection.RemoteIpAddress?.ToString();
    }
}
