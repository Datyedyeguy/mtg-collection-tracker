# ADR-019: Shared API Route Constants

**Date**: January 29, 2026
**Status**: Accepted
**Deciders**: Development Team

## Context

API routes are currently defined in two places:
1. **Backend controllers** - `[HttpPost("/api/auth/login")]`
2. **Frontend services** - `PostAsJsonAsync("/api/auth/login", ...)`

**Problem**: If a controller route changes during refactoring, the frontend still calls the old URL. This creates a runtime bug that won't be caught until the application runs (or tests with HTTP mocking are executed).

**Example scenario:**
```csharp
// Backend controller - route changes to v1
[HttpPost("/api/v1/auth/login")]  // Changed!
public async Task<IActionResult> Login(...)

// Frontend service - still using old route
await _httpClient.PostAsJsonAsync("/api/auth/login", request);  // ❌ 404 Not Found
```

This is a **runtime error** that could slip into production if:
- Tests aren't comprehensive
- Manual testing misses the flow
- Refactoring happens in backend without updating frontend

**Goal**: Make route mismatches a **compile-time error** instead of a runtime error.

## Decision

**Create a shared `ApiRoutes` constants class** in the `MTGCollectionTracker.Shared` project that both backend and frontend reference.

### Implementation

**File**: `src/shared/MTGCollectionTracker.Shared/ApiRoutes.cs`

```csharp
namespace MTGCollectionTracker.Shared;

/// <summary>
/// Centralized API route definitions shared between backend controllers and frontend services.
/// Using constants ensures compile-time safety - if a route changes, both sides update automatically.
/// </summary>
public static class ApiRoutes
{
    /// <summary>
    /// Base path for authentication endpoints.
    /// </summary>
    public const string Auth = "/api/auth";

    /// <summary>
    /// Login endpoint: POST /api/auth/login
    /// </summary>
    public const string AuthLogin = $"{Auth}/login";

    /// <summary>
    /// Register endpoint: POST /api/auth/register
    /// </summary>
    public const string AuthRegister = $"{Auth}/register";

    /// <summary>
    /// Refresh token endpoint: POST /api/auth/refresh
    /// </summary>
    public const string AuthRefresh = $"{Auth}/refresh";

    /// <summary>
    /// Logout endpoint: POST /api/auth/logout
    /// </summary>
    public const string AuthLogout = $"{Auth}/logout";

    /// <summary>
    /// Health check endpoint: GET /api/health
    /// </summary>
    public const string Health = "/api/health";

    // Future routes
    // public const string Collections = "/api/collections";
    // public const string Cards = "/api/cards";
    // public const string Decklists = "/api/decklists";
}
```

### Backend Usage

```csharp
using MTGCollectionTracker.Shared;

[ApiController]
public class AuthController : ControllerBase
{
    [HttpPost(ApiRoutes.AuthLogin)]  // ✅ Uses constant
    public async Task<IActionResult> Login(LoginRequest request)
    {
        // ...
    }

    [HttpPost(ApiRoutes.AuthRegister)]  // ✅ Uses constant
    public async Task<IActionResult> Register(RegisterRequest request)
    {
        // ...
    }
}
```

### Frontend Usage

```csharp
using MTGCollectionTracker.Shared;

public class AuthService : IAuthService
{
    public async Task<(bool, string?)> LoginAsync(LoginRequest request)
    {
        var response = await _httpClient.PostAsJsonAsync(ApiRoutes.AuthLogin, request);  // ✅ Uses constant
        // ...
    }

    public async Task<(bool, string?)> RegisterAsync(RegisterRequest request)
    {
        var response = await _httpClient.PostAsJsonAsync(ApiRoutes.AuthRegister, request);  // ✅ Uses constant
        // ...
    }
}
```

### Test Usage

```csharp
using MTGCollectionTracker.Shared;

[TestMethod]
public async Task LoginAsync_WithValidCredentials_ReturnsSuccess()
{
    var mockHttp = new MockHttpMessageHandler();
    mockHttp.When(HttpMethod.Post, $"{BaseUrl}{ApiRoutes.AuthLogin}")  // ✅ Uses constant
            .Respond("application/json", "...");
    // ...
}
```

## Alternatives Considered

### Option 1: Keep routes as string literals (Current State)

```csharp
// Backend
[HttpPost("/api/auth/login")]

// Frontend
PostAsJsonAsync("/api/auth/login", ...)
```

**Pros:**
- ✅ Simple, no extra files
- ✅ Self-documenting (route visible in code)

**Cons:**
- ❌ **Runtime errors**: Typo or refactoring breaks app at runtime
- ❌ **No compile-time safety**: Can't catch mismatches until tests run
- ❌ **Duplication**: Routes defined in multiple places
- ❌ **Refactoring risk**: Easy to forget to update all locations

---

### Option 2: Generate frontend client from OpenAPI/Swagger

**Approach**: Use NSwag or Kiota to auto-generate TypeScript/C# client from backend

**Pros:**
- ✅ 100% type-safe
- ✅ Auto-generates DTOs and methods
- ✅ Routes always in sync

**Cons:**
- ❌ Build complexity (code generation step)
- ❌ Overkill for small API
- ❌ Generated code is verbose
- ❌ Harder to customize HTTP behavior
- ❌ Requires maintaining OpenAPI schema

**Why NOT this approach:**
- Over-engineering for a small API with ~10 endpoints
- We want to learn Blazor/HTTP fundamentals, not rely on code generation
- Generated code abstracts away HTTP details (good for prod, bad for learning)

---

### Option 3: ASP.NET Core Minimal APIs with IEndpointRouteBuilder extension

**Approach**: Define routes as extension methods

```csharp
public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/auth/login", async (LoginRequest req) => ...);
    }
}
```

**Pros:**
- ✅ Centralized in one file

**Cons:**
- ❌ Backend-only solution (doesn't help frontend)
- ❌ Still requires duplicating strings for frontend
- ❌ Would require significant refactoring from controllers

---

### Option 4: Constants in backend, copy to frontend

**Approach**: Define constants in backend, manually sync to frontend

**Cons:**
- ❌ Manual synchronization is error-prone
- ❌ No compile-time guarantee they match
- ❌ Defeats the purpose of centralization

---

## Decision Rationale

**Why Option 1 (Shared Constants) is best:**

1. **Compile-time safety**: If you change `ApiRoutes.AuthLogin` from `/api/auth/login` to `/api/v1/auth/login`, **both controller and service update automatically**
2. **Single source of truth**: Route defined once, used everywhere
3. **Refactoring confidence**: IDE "Find All References" shows all usages
4. **Test alignment**: Tests use same constants, ensuring they match reality
5. **Simple**: Just a class with string constants - no code generation or build complexity
6. **Learning-friendly**: Easy to understand and maintain

**Trade-offs accepted:**
- Slightly less readable at first glance (constant name vs literal string)
  - Mitigated: Constants are well-named and have XML docs
- Requires updating Shared project and rebuilding
  - Mitigated: This is already part of normal dev workflow

## Implementation Checklist

- [ ] Create `ApiRoutes.cs` in `MTGCollectionTracker.Shared`
- [ ] Update `AuthController` to use constants
- [ ] Update `HealthController` to use constants  
- [ ] Update `AuthService` to use constants
- [ ] Update `AuthServiceTests` to use constants
- [ ] Update any `.http` test files to reference constants in comments
- [ ] Add constants to future controllers/services as they're created

## Consequences

### Positive

- **Compile-time safety**: Mismatched routes become build errors, not runtime errors
- **Refactoring confidence**: Change route in one place, updates everywhere
- **Test reliability**: Tests use same constants as production code
- **IDE support**: "Go to Definition" on route constant shows usage across solution
- **Self-documenting**: Constants have XML docs explaining what each endpoint does
- **Prevention > Detection**: Catches route mismatches before code runs

### Negative

- **Indirection**: Route not visible inline (must navigate to constant)
  - Mitigated: IDE tooltips show constant value on hover
- **Shared project dependency**: Frontend and backend both depend on Shared
  - Mitigated: This dependency already exists for DTOs

### Neutral

- **Performance**: Constants are inlined by compiler (zero runtime cost)
- **Maintenance**: One more file to maintain, but reduces overall maintenance burden

## Related Decisions

- [ADR-018: HTTP Mocking for Frontend Tests](ADR-018-http-mocking-frontend-tests.md) - Tests verify routes match
- [ADR-002: ASP.NET Core Backend](ADR-002-aspnet-core-backend.md) - Controller-based API design

## Future Enhancements

If the API grows significantly (50+ endpoints), consider:
- Grouping constants by feature area (Auth, Collections, Cards)
- Using a route builder pattern for complex parametrized routes
- OpenAPI/Swagger code generation for external consumers

For now, simple constants are sufficient and align with learning goals.

## References

- [C# String Interpolation in Constants](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/tokens/interpolated)
- [ASP.NET Core Routing](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/routing)
