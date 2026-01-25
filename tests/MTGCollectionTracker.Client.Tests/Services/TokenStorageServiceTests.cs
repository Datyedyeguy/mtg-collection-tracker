using System;
using System.Threading.Tasks;
using Microsoft.JSInterop;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MTGCollectionTracker.Client.Services;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Shouldly;

namespace MTGCollectionTracker.Client.Tests.Services;

/// <summary>
/// Tests for TokenStorageService - localStorage wrapper for JWT tokens.
/// </summary>
[TestClass]
public class TokenStorageServiceTests
{
    private IJSRuntime _mockJSRuntime = null!;
    private TokenStorageService _service = null!;

    [TestInitialize]
    public void Setup()
    {
        _mockJSRuntime = Substitute.For<IJSRuntime>();
        _service = new TokenStorageService(_mockJSRuntime);
    }

    #region Constructor Tests

    [TestMethod]
    public void Constructor_WithNullJSRuntime_ThrowsArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() => new TokenStorageService(null!));
    }

    #endregion

    #region GetAccessTokenAsync Tests

    [TestMethod]
    public async Task GetAccessTokenAsync_WhenTokenExists_ReturnsToken()
    {
        // Arrange
        var expectedToken = "test-access-token-123";
        _mockJSRuntime.InvokeAsync<string?>("localStorage.getItem", Arg.Is<object[]>(args => args.Length == 1))
            .Returns(expectedToken);

        // Act
        var result = await _service.GetAccessTokenAsync();

        // Assert
        result.ShouldBe(expectedToken);
        await _mockJSRuntime.Received(1).InvokeAsync<string?>("localStorage.getItem", Arg.Is<object[]>(args => 
            args.Length == 1 && args[0].ToString() == "accessToken"));
    }

    [TestMethod]
    public async Task GetAccessTokenAsync_WhenNoToken_ReturnsNull()
    {
        // Arrange
        _mockJSRuntime.InvokeAsync<string?>("localStorage.getItem", Arg.Any<object[]>())
            .Returns((string?)null);

        // Act
        var result = await _service.GetAccessTokenAsync();

        // Assert
        result.ShouldBeNull();
    }

    [TestMethod]
    public async Task GetAccessTokenAsync_WhenJSExceptionThrown_ReturnsNull()
    {
        // Arrange - simulate localStorage not available (private browsing)
        _mockJSRuntime.InvokeAsync<string?>("localStorage.getItem", Arg.Any<object[]>())
            .Throws(new JSException("localStorage is not available"));

        // Act
        var result = await _service.GetAccessTokenAsync();

        // Assert
        result.ShouldBeNull();
    }

    #endregion

    #region GetRefreshTokenAsync Tests

    [TestMethod]
    public async Task GetRefreshTokenAsync_WhenTokenExists_ReturnsToken()
    {
        // Arrange
        var expectedToken = "test-refresh-token-456";
        _mockJSRuntime.InvokeAsync<string?>("localStorage.getItem", Arg.Is<object[]>(args => args.Length == 1))
            .Returns(expectedToken);

        // Act
        var result = await _service.GetRefreshTokenAsync();

        // Assert
        result.ShouldBe(expectedToken);
        await _mockJSRuntime.Received(1).InvokeAsync<string?>("localStorage.getItem", Arg.Is<object[]>(args => 
            args.Length == 1 && args[0].ToString() == "refreshToken"));
    }

    [TestMethod]
    public async Task GetRefreshTokenAsync_WhenNoToken_ReturnsNull()
    {
        // Arrange
        _mockJSRuntime.InvokeAsync<string?>("localStorage.getItem", Arg.Any<object[]>())
            .Returns((string?)null);

        // Act
        var result = await _service.GetRefreshTokenAsync();

        // Assert
        result.ShouldBeNull();
    }

    [TestMethod]
    public async Task GetRefreshTokenAsync_WhenJSExceptionThrown_ReturnsNull()
    {
        // Arrange
        _mockJSRuntime.InvokeAsync<string?>("localStorage.getItem", Arg.Any<object[]>())
            .Throws(new JSException("localStorage is not available"));

        // Act
        var result = await _service.GetRefreshTokenAsync();

        // Assert
        result.ShouldBeNull();
    }

    #endregion

    #region SaveTokensAsync Tests

    [TestMethod]
    public async Task SaveTokensAsync_WithValidTokens_SavesBothToLocalStorage()
    {
        // Arrange
        var accessToken = "access-token-123";
        var refreshToken = "refresh-token-456";

        // Act
        await _service.SaveTokensAsync(accessToken, refreshToken);

        // Assert
        await _mockJSRuntime.Received(1).InvokeVoidAsync("localStorage.setItem", Arg.Is<object[]>(args =>
            args.Length == 2 && args[0].ToString() == "accessToken" && args[1].ToString() == accessToken));
        await _mockJSRuntime.Received(1).InvokeVoidAsync("localStorage.setItem", Arg.Is<object[]>(args =>
            args.Length == 2 && args[0].ToString() == "refreshToken" && args[1].ToString() == refreshToken));
    }

    [TestMethod]
    public async Task SaveTokensAsync_WithNullAccessToken_ThrowsArgumentException()
    {
        // Act & Assert
        var ex = await Should.ThrowAsync<ArgumentException>(
            async () => await _service.SaveTokensAsync(null!, "refresh-token"));
        
        ex.ParamName.ShouldBe("accessToken");
    }

    [TestMethod]
    public async Task SaveTokensAsync_WithEmptyAccessToken_ThrowsArgumentException()
    {
        // Act & Assert
        var ex = await Should.ThrowAsync<ArgumentException>(
            async () => await _service.SaveTokensAsync("", "refresh-token"));
        
        ex.ParamName.ShouldBe("accessToken");
    }

    [TestMethod]
    public async Task SaveTokensAsync_WithWhitespaceAccessToken_ThrowsArgumentException()
    {
        // Act & Assert
        var ex = await Should.ThrowAsync<ArgumentException>(
            async () => await _service.SaveTokensAsync("   ", "refresh-token"));
        
        ex.ParamName.ShouldBe("accessToken");
    }

    [TestMethod]
    public async Task SaveTokensAsync_WithNullRefreshToken_ThrowsArgumentException()
    {
        // Act & Assert
        var ex = await Should.ThrowAsync<ArgumentException>(
            async () => await _service.SaveTokensAsync("access-token", null!));
        
        ex.ParamName.ShouldBe("refreshToken");
    }

    [TestMethod]
    public async Task SaveTokensAsync_WithEmptyRefreshToken_ThrowsArgumentException()
    {
        // Act & Assert
        var ex = await Should.ThrowAsync<ArgumentException>(
            async () => await _service.SaveTokensAsync("access-token", ""));
        
        ex.ParamName.ShouldBe("refreshToken");
    }

    [TestMethod]
    public async Task SaveTokensAsync_WhenJSExceptionThrown_ThrowsInvalidOperationException()
    {
        // Arrange
        _mockJSRuntime.InvokeVoidAsync("localStorage.setItem", Arg.Any<object[]>())
            .Throws(new JSException("localStorage quota exceeded"));

        // Act & Assert
        var ex = await Should.ThrowAsync<InvalidOperationException>(
            async () => await _service.SaveTokensAsync("access", "refresh"));
        
        ex.Message.ShouldContain("Failed to save tokens");
        ex.InnerException.ShouldBeOfType<JSException>();
    }

    #endregion

    #region ClearTokensAsync Tests

    [TestMethod]
    public async Task ClearTokensAsync_RemovesBothTokensFromLocalStorage()
    {
        // Act
        await _service.ClearTokensAsync();

        // Assert
        await _mockJSRuntime.Received(1).InvokeVoidAsync("localStorage.removeItem", Arg.Is<object[]>(args =>
            args.Length == 1 && args[0].ToString() == "accessToken"));
        await _mockJSRuntime.Received(1).InvokeVoidAsync("localStorage.removeItem", Arg.Is<object[]>(args =>
            args.Length == 1 && args[0].ToString() == "refreshToken"));
    }

    [TestMethod]
    public async Task ClearTokensAsync_WhenJSExceptionThrown_DoesNotThrow()
    {
        // Arrange
        _mockJSRuntime.InvokeVoidAsync("localStorage.removeItem", Arg.Any<object[]>())
            .Throws(new JSException("localStorage not available"));

        // Act - should not throw
        await _service.ClearTokensAsync();

        // Assert - exception was swallowed
        await _mockJSRuntime.Received().InvokeVoidAsync("localStorage.removeItem", Arg.Is<object[]>(args =>
            args.Length == 1 && args[0].ToString() == "accessToken"));
    }

    #endregion
}
