using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MTGCollectionTracker.Api.Configuration;
using MTGCollectionTracker.Api.Services;
using MTGCollectionTracker.Data.Entities;
using NSubstitute;
using Shouldly;

namespace MTGCollectionTracker.Api.Tests.Services;

[TestClass]
public class JwtServiceTests
{
    private const string ValidSecret = "ThisIsAValidSecretKeyThatIsAtLeast32Characters!";
    private const string ValidIssuer = "https://test.example.com";
    private const string ValidAudience = "https://app.example.com";

    private static JwtSettings CreateValidSettings(
        string? secret = null,
        int accessTokenExpiryMinutes = 15,
        int refreshTokenExpiryDays = 7)
    {
        return new JwtSettings
        {
            Secret = secret ?? ValidSecret,
            Issuer = ValidIssuer,
            Audience = ValidAudience,
            AccessTokenExpiryMinutes = accessTokenExpiryMinutes,
            RefreshTokenExpiryDays = refreshTokenExpiryDays
        };
    }

    private static IOptions<JwtSettings> CreateOptions(JwtSettings settings)
    {
        var options = Substitute.For<IOptions<JwtSettings>>();
        options.Value.Returns(settings);
        return options;
    }

    private static ApplicationUser CreateTestUser(
        string id = "test-user-id",
        string email = "test@example.com",
        string? displayName = "Test User")
    {
        return new ApplicationUser
        {
            Id = id,
            Email = email,
            DisplayName = displayName
        };
    }

    #region Constructor Tests

    [TestMethod]
    public void Constructor_WithValidSettings_CreatesInstance()
    {
        // Arrange
        var settings = CreateValidSettings();
        var options = CreateOptions(settings);

        // Act
        var service = new JwtService(options);

        // Assert
        service.ShouldNotBeNull();
    }

    [TestMethod]
    public void Constructor_WithEmptySecret_ThrowsInvalidOperationException()
    {
        // Arrange
        var settings = CreateValidSettings(secret: string.Empty);
        var options = CreateOptions(settings);

        // Act & Assert
        var ex = Should.Throw<InvalidOperationException>(() => new JwtService(options));
        ex.Message.ShouldContain("32 characters");
    }

    [TestMethod]
    public void Constructor_WithShortSecret_ThrowsInvalidOperationException()
    {
        // Arrange
        var settings = CreateValidSettings(secret: "TooShort");
        var options = CreateOptions(settings);

        // Act & Assert
        var ex = Should.Throw<InvalidOperationException>(() => new JwtService(options));
        ex.Message.ShouldContain("32 characters");
    }

    [TestMethod]
    public void Constructor_WithExactly32CharSecret_CreatesInstance()
    {
        // Arrange
        var settings = CreateValidSettings(secret: "12345678901234567890123456789012"); // exactly 32
        var options = CreateOptions(settings);

        // Act
        var service = new JwtService(options);

        // Assert
        service.ShouldNotBeNull();
    }

    #endregion

    #region GenerateAccessToken Tests

    [TestMethod]
    public void GenerateAccessToken_WithValidUser_ReturnsNonEmptyToken()
    {
        // Arrange
        var service = new JwtService(CreateOptions(CreateValidSettings()));
        var user = CreateTestUser();

        // Act
        var token = service.GenerateAccessToken(user);

        // Assert
        token.ShouldNotBeNullOrEmpty();
    }

    [TestMethod]
    public void GenerateAccessToken_WithValidUser_ReturnsValidJwt()
    {
        // Arrange
        var service = new JwtService(CreateOptions(CreateValidSettings()));
        var user = CreateTestUser();

        // Act
        var token = service.GenerateAccessToken(user);

        // Assert
        var handler = new JwtSecurityTokenHandler();
        handler.CanReadToken(token).ShouldBeTrue();

        var jwtToken = handler.ReadJwtToken(token);
        jwtToken.ShouldNotBeNull();
    }

    [TestMethod]
    public void GenerateAccessToken_WithValidUser_ContainsCorrectClaims()
    {
        // Arrange
        var service = new JwtService(CreateOptions(CreateValidSettings()));
        var user = CreateTestUser(id: "user-123", email: "john@example.com", displayName: "John Doe");

        // Act
        var token = service.GenerateAccessToken(user);

        // Assert
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);

        jwtToken.Subject.ShouldBe("user-123");
        jwtToken.Claims.First(c => c.Type == JwtRegisteredClaimNames.Email).Value.ShouldBe("john@example.com");
        jwtToken.Claims.First(c => c.Type == "displayName").Value.ShouldBe("John Doe");
        jwtToken.Claims.First(c => c.Type == JwtRegisteredClaimNames.Jti).Value.ShouldNotBeNullOrEmpty();
    }

    [TestMethod]
    public void GenerateAccessToken_WithValidUser_HasCorrectIssuerAndAudience()
    {
        // Arrange
        var service = new JwtService(CreateOptions(CreateValidSettings()));
        var user = CreateTestUser();

        // Act
        var token = service.GenerateAccessToken(user);

        // Assert
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);

        jwtToken.Issuer.ShouldBe(ValidIssuer);
        jwtToken.Audiences.ShouldContain(ValidAudience);
    }

    [TestMethod]
    public void GenerateAccessToken_WithNullDisplayName_UsesEmailAsDisplayName()
    {
        // Arrange
        var service = new JwtService(CreateOptions(CreateValidSettings()));
        var user = CreateTestUser(email: "fallback@example.com", displayName: null);

        // Act
        var token = service.GenerateAccessToken(user);

        // Assert
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);

        jwtToken.Claims.First(c => c.Type == "displayName").Value.ShouldBe("fallback@example.com");
    }

    [TestMethod]
    public void GenerateAccessToken_CalledTwice_GeneratesUniqueTokens()
    {
        // Arrange
        var service = new JwtService(CreateOptions(CreateValidSettings()));
        var user = CreateTestUser();

        // Act
        var token1 = service.GenerateAccessToken(user);
        var token2 = service.GenerateAccessToken(user);

        // Assert
        token1.ShouldNotBe(token2, "each token should have a unique JTI");
    }

    #endregion

    #region GenerateRefreshToken Tests

    [TestMethod]
    public void GenerateRefreshToken_ReturnsNonEmptyString()
    {
        // Arrange
        var service = new JwtService(CreateOptions(CreateValidSettings()));

        // Act
        var token = service.GenerateRefreshToken();

        // Assert
        token.ShouldNotBeNullOrEmpty();
    }

    [TestMethod]
    public void GenerateRefreshToken_ReturnsBase64String()
    {
        // Arrange
        var service = new JwtService(CreateOptions(CreateValidSettings()));

        // Act
        var token = service.GenerateRefreshToken();

        // Assert
        Should.NotThrow(() => Convert.FromBase64String(token));
    }

    [TestMethod]
    public void GenerateRefreshToken_CalledTwice_GeneratesUniqueTokens()
    {
        // Arrange
        var service = new JwtService(CreateOptions(CreateValidSettings()));

        // Act
        var token1 = service.GenerateRefreshToken();
        var token2 = service.GenerateRefreshToken();

        // Assert
        token1.ShouldNotBe(token2, "each refresh token should be unique");
    }

    [TestMethod]
    public void GenerateRefreshToken_HasSufficientLength()
    {
        // Arrange
        var service = new JwtService(CreateOptions(CreateValidSettings()));

        // Act
        var token = service.GenerateRefreshToken();
        var bytes = Convert.FromBase64String(token);

        // Assert
        bytes.Length.ShouldBeGreaterThanOrEqualTo(32, "refresh token should have sufficient entropy");
    }

    #endregion

    #region GetPrincipalFromExpiredToken Tests

    [TestMethod]
    public void GetPrincipalFromExpiredToken_WithValidToken_ReturnsPrincipal()
    {
        // Arrange
        var service = new JwtService(CreateOptions(CreateValidSettings()));
        var user = CreateTestUser(id: "user-456");
        var token = service.GenerateAccessToken(user);

        // Act
        var principal = service.GetPrincipalFromExpiredToken(token);

        // Assert
        principal.ShouldNotBeNull();
        principal.FindFirst(ClaimTypes.NameIdentifier)?.Value.ShouldBe("user-456");
    }

    [TestMethod]
    public void GetPrincipalFromExpiredToken_WithInvalidToken_ReturnsNull()
    {
        // Arrange
        var service = new JwtService(CreateOptions(CreateValidSettings()));

        // Act
        var principal = service.GetPrincipalFromExpiredToken("not.a.valid.token");

        // Assert
        principal.ShouldBeNull();
    }

    [TestMethod]
    public void GetPrincipalFromExpiredToken_WithRandomString_ReturnsNull()
    {
        // Arrange
        var service = new JwtService(CreateOptions(CreateValidSettings()));

        // Act
        var principal = service.GetPrincipalFromExpiredToken("randomgarbage");

        // Assert
        principal.ShouldBeNull();
    }

    [TestMethod]
    public void GetPrincipalFromExpiredToken_WithEmptyString_ReturnsNull()
    {
        // Arrange
        var service = new JwtService(CreateOptions(CreateValidSettings()));

        // Act
        var principal = service.GetPrincipalFromExpiredToken(string.Empty);

        // Assert
        principal.ShouldBeNull();
    }

    [TestMethod]
    public void GetPrincipalFromExpiredToken_WithWrongSigningKey_ReturnsNull()
    {
        // Arrange
        var service1 = new JwtService(CreateOptions(CreateValidSettings(secret: ValidSecret)));
        var service2 = new JwtService(CreateOptions(CreateValidSettings(secret: "ADifferentSecretKeyThatIsAlso32Chars!")));

        var user = CreateTestUser();
        var token = service1.GenerateAccessToken(user);

        // Act - try to validate with different key
        var principal = service2.GetPrincipalFromExpiredToken(token);

        // Assert
        principal.ShouldBeNull();
    }

    #endregion

    #region GetAccessTokenExpiry Tests

    [TestMethod]
    public void GetAccessTokenExpiry_ReturnsTimeInFuture()
    {
        // Arrange
        var service = new JwtService(CreateOptions(CreateValidSettings(accessTokenExpiryMinutes: 15)));
        var before = DateTime.UtcNow;

        // Act
        var expiry = service.GetAccessTokenExpiry();

        // Assert
        expiry.ShouldBeGreaterThan(before);
    }

    [TestMethod]
    public void GetAccessTokenExpiry_ReturnsCorrectDuration()
    {
        // Arrange
        var expiryMinutes = 30;
        var service = new JwtService(CreateOptions(CreateValidSettings(accessTokenExpiryMinutes: expiryMinutes)));
        var before = DateTime.UtcNow;

        // Act
        var expiry = service.GetAccessTokenExpiry();

        // Assert
        var expectedExpiry = before.AddMinutes(expiryMinutes);
        expiry.ShouldBe(expectedExpiry, TimeSpan.FromSeconds(1));
    }

    #endregion

    #region GetRefreshTokenExpiry Tests

    [TestMethod]
    public void GetRefreshTokenExpiry_ReturnsTimeInFuture()
    {
        // Arrange
        var service = new JwtService(CreateOptions(CreateValidSettings(refreshTokenExpiryDays: 7)));
        var before = DateTime.UtcNow;

        // Act
        var expiry = service.GetRefreshTokenExpiry();

        // Assert
        expiry.ShouldBeGreaterThan(before);
    }

    [TestMethod]
    public void GetRefreshTokenExpiry_ReturnsCorrectDuration()
    {
        // Arrange
        var expiryDays = 14;
        var service = new JwtService(CreateOptions(CreateValidSettings(refreshTokenExpiryDays: expiryDays)));
        var before = DateTime.UtcNow;

        // Act
        var expiry = service.GetRefreshTokenExpiry();

        // Assert
        var expectedExpiry = before.AddDays(expiryDays);
        expiry.ShouldBe(expectedExpiry, TimeSpan.FromSeconds(1));
    }

    #endregion

    #region HashToken Tests

    [TestMethod]
    public void HashToken_WithValidToken_ReturnsNonEmptyString()
    {
        // Arrange
        var service = new JwtService(CreateOptions(CreateValidSettings()));
        var token = "some-refresh-token";

        // Act
        var hash = service.HashToken(token);

        // Assert
        hash.ShouldNotBeNullOrEmpty();
    }

    [TestMethod]
    public void HashToken_WithSameInput_ReturnsSameHash()
    {
        // Arrange
        var service = new JwtService(CreateOptions(CreateValidSettings()));
        var token = "consistent-token";

        // Act
        var hash1 = service.HashToken(token);
        var hash2 = service.HashToken(token);

        // Assert
        hash1.ShouldBe(hash2, "hashing should be deterministic");
    }

    [TestMethod]
    public void HashToken_WithDifferentInputs_ReturnsDifferentHashes()
    {
        // Arrange
        var service = new JwtService(CreateOptions(CreateValidSettings()));

        // Act
        var hash1 = service.HashToken("token-one");
        var hash2 = service.HashToken("token-two");

        // Assert
        hash1.ShouldNotBe(hash2);
    }

    [TestMethod]
    public void HashToken_ReturnsValidBase64()
    {
        // Arrange
        var service = new JwtService(CreateOptions(CreateValidSettings()));
        var token = "any-token";

        // Act
        var hash = service.HashToken(token);

        // Assert
        Should.NotThrow(() => Convert.FromBase64String(hash));
    }

    [TestMethod]
    public void HashToken_ReturnsExpectedSha256Length()
    {
        // Arrange
        var service = new JwtService(CreateOptions(CreateValidSettings()));
        var token = "any-token";

        // Act
        var hash = service.HashToken(token);
        var bytes = Convert.FromBase64String(hash);

        // Assert
        bytes.Length.ShouldBe(32, "SHA256 produces 32 bytes (256 bits)");
    }

    #endregion
}
