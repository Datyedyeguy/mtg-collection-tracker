# ADR-018: HTTP Mocking for Frontend Tests

**Date**: January 29, 2026
**Status**: Proposed
**Deciders**: Development Team

## Context

The Blazor frontend's `AuthService` makes HTTP calls to the backend API. To test this service in isolation without requiring a running API, we need a way to mock HTTP responses.

**Requirements:**
1. Mock `HttpClient` responses for unit testing
2. Verify correct API endpoints are called (catch refactoring issues)
3. Test various HTTP status codes (200, 400, 401, 409, 500)
4. Parse and validate request/response bodies
5. Work with MSTest (our chosen testing framework)
6. Not require complex setup or DI container modifications

**Alternatives Considered:**

### Option 1: RichardSzalay.MockHttp ✅ **RECOMMENDED**

**Approach**: Intercepts HTTP calls using `MockHttpMessageHandler`

```csharp
var mockHttp = new MockHttpMessageHandler();
mockHttp.When(HttpMethod.Post, "https://localhost:5001/api/auth/login")
        .Respond("application/json", "{\"accessToken\": \"...\"}");

var httpClient = mockHttp.ToHttpClient();
httpClient.BaseAddress = new Uri("https://localhost:5001");

var authService = new AuthService(httpClient, ...);
```

**Pros:**
- ✅ Purpose-built for HTTP mocking
- ✅ Clean, fluent API with `.When()` and `.Respond()`
- ✅ Verifies full HTTP request (method, URL, headers, body)
- ✅ Works seamlessly with `HttpClient` - no wrapper interfaces needed
- ✅ Supports request matching with wildcards and predicates
- ✅ Can assert request was made: `mockHttp.VerifyNoOutstandingExpectation()`
- ✅ Well-maintained, stable library (7+ years, 4M+ downloads)
- ✅ MIT licensed
- ✅ Works with any DI framework or no DI

**Cons:**
- ⚠️ Additional NuGet dependency
- ⚠️ Requires installing separate package

**Example Test:**

```csharp
[TestMethod]
public async Task LoginAsync_WithValidCredentials_ReturnsSuccess()
{
    // Arrange
    var mockHttp = new MockHttpMessageHandler();
    mockHttp.When(HttpMethod.Post, $"{BaseUrl}/api/auth/login")
            .Respond("application/json", @"{
                ""accessToken"": ""token123"",
                ""refreshToken"": ""refresh456"",
                ""email"": ""test@example.com""
            }");

    var httpClient = mockHttp.ToHttpClient();
    httpClient.BaseAddress = new Uri(BaseUrl);

    var authService = new AuthService(httpClient, _tokenStorage, _authProvider, _nav);

    var request = new LoginRequest { Email = "test@example.com", Password = "password" };

    // Act
    var (success, error) = await authService.LoginAsync(request);

    // Assert
    success.ShouldBeTrue();
    error.ShouldBeNull();
    await _tokenStorage.Received().SaveTokensAsync("token123", "refresh456");
}
```

---

### Option 2: IHttpClientFactory Wrapper Interface

**Approach**: Create `IHttpClientWrapper` interface and mock it

```csharp
public interface IHttpClientWrapper
{
    Task<HttpResponseMessage> PostAsJsonAsync<T>(string url, T content);
}

// AuthService depends on IHttpClientWrapper instead of HttpClient
```

**Pros:**
- ✅ No additional dependencies
- ✅ Full control over mocking logic
- ✅ Works with NSubstitute (existing dependency)

**Cons:**
- ❌ Requires creating and maintaining wrapper interface
- ❌ All HTTP methods need explicit wrapping (Get, Post, Put, Delete, Patch)
- ❌ Wrapper doesn't match real `HttpClient` semantics (headers, timeout, etc.)
- ❌ More code to maintain
- ❌ Doesn't test actual HTTP serialization/deserialization
- ❌ Abstracts away the real HTTP stack (loses fidelity)

**Why NOT this approach:**
- Over-engineering: Creating unnecessary abstraction layers
- Doesn't test the real HTTP pipeline (JSON serialization, headers, content negotiation)
- More code surface area for bugs

---

### Option 3: TestServer (Microsoft.AspNetCore.Mvc.Testing)

**Approach**: Spin up in-memory ASP.NET Core server for integration tests

```csharp
var factory = new WebApplicationFactory<Program>();
var client = factory.CreateClient();

var response = await client.PostAsJsonAsync("/api/auth/login", request);
```

**Pros:**
- ✅ Tests full HTTP stack including routing, model binding, middleware
- ✅ Microsoft-official library
- ✅ Great for integration tests

**Cons:**
- ❌ Requires running entire backend API in-memory
- ❌ Slower than unit tests (needs DB, EF migrations, etc.)
- ❌ Not suitable for unit testing a frontend service in isolation
- ❌ Overkill for testing client-side HTTP calls
- ❌ Requires backend project reference (crosses boundary)

**Why NOT this approach:**
- Integration testing tool, not unit testing tool
- We want to test `AuthService` logic, not backend API
- Backend already has its own tests

---

### Option 4: Manual HttpMessageHandler Mock

**Approach**: Implement custom `HttpMessageHandler` subclass

```csharp
public class MockHttpMessageHandler : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (request.RequestUri.PathAndQuery == "/api/auth/login")
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
        
        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
    }
}
```

**Pros:**
- ✅ No dependencies
- ✅ Full control

**Cons:**
- ❌ Boilerplate code for every test
- ❌ No fluent API for readability
- ❌ Manual URL matching is error-prone
- ❌ No built-in request verification
- ❌ Reinventing the wheel (RichardSzalay already solved this)

**Why NOT this approach:**
- RichardSzalay.MockHttp does this better with less code

---

## Decision

**PROPOSED: Use RichardSzalay.MockHttp** for HTTP mocking in frontend service tests.

*This decision is pending review and approval.*

**Rationale:**
1. **Purpose-built**: Designed specifically for this exact use case
2. **Clean tests**: Fluent API makes tests readable and maintainable
3. **Full verification**: Tests actual HTTP requests (URL, method, body) - catches refactoring issues
4. **Battle-tested**: 7+ years in production use, stable API
5. **No abstraction overhead**: Works directly with `HttpClient` - no wrapper interfaces needed
6. **MIT licensed**: No licensing concerns

**When to use:**
- ✅ Unit testing frontend services that call HTTP APIs (AuthService, future collection services)
- ✅ Verifying correct API endpoints are called
- ✅ Testing error handling (401, 404, 500 responses)

**When NOT to use:**
- ❌ Backend API integration tests (use TestServer or TestContainers instead)
- ❌ E2E tests (use real API or tools like Playwright)

## Implementation Plan

1. Add NuGet package to `MTGCollectionTracker.Client.Tests`:
   ```bash
   dotnet add package RichardSzalay.MockHttp
   ```

2. Implement currently `[Ignore]`d tests in `AuthServiceTests.cs`

3. Create shared `ApiRoutes` constants (see ADR-019) to ensure paths match controllers

4. Add similar tests for future HTTP services (collections, cards, etc.)

## Package Reference

```xml
<PackageReference Include="RichardSzalay.MockHttp" Version="7.*" />
```

## Consequences

### Positive

- **Test fidelity**: Tests actual HTTP serialization/deserialization
- **Refactoring safety**: Tests will fail if API URLs change but client doesn't update
- **Readable tests**: Clear setup with `.When()` and `.Respond()` API
- **No abstractions**: No need for `IHttpClientWrapper` interface
- **Quick to write**: Minimal boilerplate per test
- **Isolated testing**: No need for running backend API

### Negative

- **Additional dependency**: One more NuGet package (mitigated: it's lightweight and stable)
- **Learning curve**: Team needs to learn MockHttp API (mitigated: simple, well-documented)

### Neutral

- **Test speed**: Similar to other mocking approaches (all in-memory)

## Related Decisions

- [ADR-017: Testing Libraries](ADR-017-testing-libraries.md) - MSTest, NSubstitute, Shouldly
- [ADR-019: Shared API Route Constants](ADR-019-shared-api-routes.md) - Compile-time route safety

## References

- [RichardSzalay.MockHttp Documentation](https://github.com/richardszalay/mockhttp)
- [Microsoft HttpClient Testing Guidelines](https://learn.microsoft.com/en-us/dotnet/core/testing/unit-testing-with-mstest)
- [TestServer Documentation](https://learn.microsoft.com/en-us/aspnet/core/test/integration-tests)
