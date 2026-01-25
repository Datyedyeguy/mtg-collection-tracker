using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MTGCollectionTracker.Client.Services;
using NSubstitute;
using Shouldly;

namespace MTGCollectionTracker.Client.Tests.Services;

/// <summary>
/// Tests for CustomAuthStateProvider - JWT parsing and authentication state management.
/// </summary>
[TestClass]
public class CustomAuthStateProviderTests
{
    private ITokenStorageService _mockTokenStorage = null!;
    private CustomAuthStateProvider _provider = null!;

    // Valid JWT token for testing (expires far in future: year 2099)
    // Payload: {"sub":"test-user-123","email":"test@example.com","name":"Test User","exp":4070908800}
    private const string ValidToken = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiJ0ZXN0LXVzZXItMTIzIiwiZW1haWwiOiJ0ZXN0QGV4YW1wbGUuY29tIiwibmFtZSI6IlRlc3QgVXNlciIsImV4cCI6NDA3MDkwODgwMH0.6vn7VLXlXb8qKzDWZqJhJQPqxDQqJQ4KqGZ8qZ4qZ4M";

    // Expired JWT token (expired in year 2020)
    // Payload: {"sub":"expired-user","email":"expired@example.com","exp":1577836800}
    private const string ExpiredToken = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiJleHBpcmVkLXVzZXIiLCJlbWFpbCI6ImV4cGlyZWRAZXhhbXBsZS5jb20iLCJleHAiOjE1Nzc4MzY4MDB9.4LZqZ5qZ4qZ4qZ4qZ4qZ4qZ4qZ4qZ4qZ4qZ4qZ4qZ4";

    [TestInitialize]
    public void Setup()
    {
        _mockTokenStorage = Substitute.For<ITokenStorageService>();
        _provider = new CustomAuthStateProvider(_mockTokenStorage);
    }

    #region Constructor Tests

    [TestMethod]
    public void Constructor_WithNullTokenStorage_ThrowsArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() => new CustomAuthStateProvider(null!));
    }

    #endregion

    #region GetAuthenticationStateAsync Tests

    [TestMethod]
    public async Task GetAuthenticationStateAsync_WhenNoToken_ReturnsAnonymousUser()
    {
        // Arrange
        _mockTokenStorage.GetAccessTokenAsync().Returns((string?)null);

        // Act
        var authState = await _provider.GetAuthenticationStateAsync();

        // Assert
        authState.ShouldNotBeNull();
        authState.User.ShouldNotBeNull();
        authState.User.Identity.ShouldNotBeNull();
        authState.User.Identity.IsAuthenticated.ShouldBeFalse();
    }

    [TestMethod]
    public async Task GetAuthenticationStateAsync_WhenEmptyToken_ReturnsAnonymousUser()
    {
        // Arrange
        _mockTokenStorage.GetAccessTokenAsync().Returns(string.Empty);

        // Act
        var authState = await _provider.GetAuthenticationStateAsync();

        // Assert
        authState.User.Identity!.IsAuthenticated.ShouldBeFalse();
    }

    [TestMethod]
    public async Task GetAuthenticationStateAsync_WithValidToken_ReturnsAuthenticatedUser()
    {
        // Arrange
        _mockTokenStorage.GetAccessTokenAsync().Returns(ValidToken);

        // Act
        var authState = await _provider.GetAuthenticationStateAsync();

        // Assert
        authState.User.Identity!.IsAuthenticated.ShouldBeTrue();
        authState.User.Identity.AuthenticationType.ShouldBe("jwt");
    }

    [TestMethod]
    public async Task GetAuthenticationStateAsync_WithValidToken_ExtractsClaims()
    {
        // Arrange
        _mockTokenStorage.GetAccessTokenAsync().Returns(ValidToken);

        // Act
        var authState = await _provider.GetAuthenticationStateAsync();

        // Assert
        var claims = authState.User.Claims.ToList();
        claims.ShouldNotBeEmpty();
        
        var subClaim = claims.FirstOrDefault(c => c.Type == "sub");
        subClaim.ShouldNotBeNull();
        subClaim.Value.ShouldBe("test-user-123");

        var emailClaim = claims.FirstOrDefault(c => c.Type == "email");
        emailClaim.ShouldNotBeNull();
        emailClaim.Value.ShouldBe("test@example.com");

        var nameClaim = claims.FirstOrDefault(c => c.Type == "name");
        nameClaim.ShouldNotBeNull();
        nameClaim.Value.ShouldBe("Test User");
    }

    [TestMethod]
    public async Task GetAuthenticationStateAsync_WithExpiredToken_ReturnsAnonymousUser()
    {
        // Arrange
        _mockTokenStorage.GetAccessTokenAsync().Returns(ExpiredToken);

        // Act
        var authState = await _provider.GetAuthenticationStateAsync();

        // Assert
        authState.User.Identity!.IsAuthenticated.ShouldBeFalse();
    }

    [TestMethod]
    public async Task GetAuthenticationStateAsync_WithExpiredToken_ClearsTokens()
    {
        // Arrange
        _mockTokenStorage.GetAccessTokenAsync().Returns(ExpiredToken);

        // Act
        await _provider.GetAuthenticationStateAsync();

        // Assert
        await _mockTokenStorage.Received(1).ClearTokensAsync();
    }

    [TestMethod]
    public async Task GetAuthenticationStateAsync_WithInvalidToken_ReturnsAnonymousUser()
    {
        // Arrange - token with only 2 parts (should be 3)
        _mockTokenStorage.GetAccessTokenAsync().Returns("invalid.token");

        // Act
        var authState = await _provider.GetAuthenticationStateAsync();

        // Assert
        authState.User.Identity!.IsAuthenticated.ShouldBeFalse();
    }

    [TestMethod]
    public async Task GetAuthenticationStateAsync_WithInvalidToken_ClearsTokens()
    {
        // Arrange
        _mockTokenStorage.GetAccessTokenAsync().Returns("invalid.token.format");

        // Act
        await _provider.GetAuthenticationStateAsync();

        // Assert
        await _mockTokenStorage.Received(1).ClearTokensAsync();
    }

    [TestMethod]
    public async Task GetAuthenticationStateAsync_WithMalformedBase64_ReturnsAnonymousUser()
    {
        // Arrange - invalid base64 in payload
        _mockTokenStorage.GetAccessTokenAsync().Returns("header.!!!invalid-base64!!!.signature");

        // Act
        var authState = await _provider.GetAuthenticationStateAsync();

        // Assert
        authState.User.Identity!.IsAuthenticated.ShouldBeFalse();
    }

    #endregion

    #region NotifyUserAuthentication Tests

    [TestMethod]
    public void NotifyUserAuthentication_WithValidToken_TriggersAuthenticationStateChanged()
    {
        // Arrange
        AuthenticationState? capturedState = null;
        _provider.AuthenticationStateChanged += task =>
        {
            capturedState = task.Result;
        };

        // Act
        _provider.NotifyUserAuthentication(ValidToken);

        // Assert
        capturedState.ShouldNotBeNull();
        capturedState.User.Identity!.IsAuthenticated.ShouldBeTrue();
    }

    [TestMethod]
    public void NotifyUserAuthentication_WithValidToken_ExtractsClaims()
    {
        // Arrange
        AuthenticationState? capturedState = null;
        _provider.AuthenticationStateChanged += task =>
        {
            capturedState = task.Result;
        };

        // Act
        _provider.NotifyUserAuthentication(ValidToken);

        // Assert
        capturedState.ShouldNotBeNull();
        var subClaim = capturedState.User.FindFirst("sub");
        subClaim.ShouldNotBeNull();
        subClaim.Value.ShouldBe("test-user-123");
    }

    [TestMethod]
    public void NotifyUserAuthentication_WithNullToken_ThrowsArgumentException()
    {
        // Act & Assert
        Should.Throw<ArgumentException>(() => _provider.NotifyUserAuthentication(null!));
    }

    [TestMethod]
    public void NotifyUserAuthentication_WithEmptyToken_ThrowsArgumentException()
    {
        // Act & Assert
        Should.Throw<ArgumentException>(() => _provider.NotifyUserAuthentication(string.Empty));
    }

    [TestMethod]
    public void NotifyUserAuthentication_WithWhitespaceToken_ThrowsArgumentException()
    {
        // Act & Assert
        Should.Throw<ArgumentException>(() => _provider.NotifyUserAuthentication("   "));
    }

    #endregion

    #region NotifyUserLogout Tests

    [TestMethod]
    public void NotifyUserLogout_TriggersAuthenticationStateChanged()
    {
        // Arrange
        AuthenticationState? capturedState = null;
        _provider.AuthenticationStateChanged += task =>
        {
            capturedState = task.Result;
        };

        // Act
        _provider.NotifyUserLogout();

        // Assert
        capturedState.ShouldNotBeNull();
        capturedState.User.Identity!.IsAuthenticated.ShouldBeFalse();
    }

    [TestMethod]
    public void NotifyUserLogout_CreatesAnonymousUser()
    {
        // Arrange
        AuthenticationState? capturedState = null;
        _provider.AuthenticationStateChanged += task =>
        {
            capturedState = task.Result;
        };

        // Act
        _provider.NotifyUserLogout();

        // Assert
        capturedState.ShouldNotBeNull();
        capturedState.User.ShouldNotBeNull();
        capturedState.User.Claims.ShouldBeEmpty();
    }

    #endregion

    #region Integration Scenario Tests

    [TestMethod]
    public async Task Scenario_LoginFlow_UpdatesAuthenticationState()
    {
        // Arrange - start with no token
        _mockTokenStorage.GetAccessTokenAsync().Returns((string?)null);

        // Act 1: Check initial state (not logged in)
        var initialState = await _provider.GetAuthenticationStateAsync();

        // Assert 1: User is anonymous
        initialState.User.Identity!.IsAuthenticated.ShouldBeFalse();

        // Act 2: Simulate login - update mock to return token
        _mockTokenStorage.GetAccessTokenAsync().Returns(ValidToken);
        _provider.NotifyUserAuthentication(ValidToken);

        // Act 3: Check state after login
        var loggedInState = await _provider.GetAuthenticationStateAsync();

        // Assert 2: User is now authenticated
        loggedInState.User.Identity!.IsAuthenticated.ShouldBeTrue();
        loggedInState.User.FindFirst("sub")?.Value.ShouldBe("test-user-123");
    }

    [TestMethod]
    public async Task Scenario_LogoutFlow_ClearsAuthenticationState()
    {
        // Arrange - start logged in
        _mockTokenStorage.GetAccessTokenAsync().Returns(ValidToken);

        // Act 1: Verify logged in
        var initialState = await _provider.GetAuthenticationStateAsync();
        initialState.User.Identity!.IsAuthenticated.ShouldBeTrue();

        // Act 2: Simulate logout
        _mockTokenStorage.GetAccessTokenAsync().Returns((string?)null);
        _provider.NotifyUserLogout();

        // Act 3: Check state after logout
        var loggedOutState = await _provider.GetAuthenticationStateAsync();

        // Assert: User is anonymous
        loggedOutState.User.Identity!.IsAuthenticated.ShouldBeFalse();
    }

    #endregion
}
