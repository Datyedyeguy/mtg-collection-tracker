namespace MTGCollectionTracker.Shared;

/// <summary>
/// Centralized API route definitions shared between backend controllers and frontend services.
/// Using constants ensures compile-time safety - if a route changes, both sides update automatically.
/// This prevents runtime 404 errors caused by mismatched routes after refactoring.
/// </summary>
/// <remarks>
/// API Versioning Decision Pending:
/// Currently routes use /api/[resource] without versioning (e.g., /api/auth/login).
/// To add versioning in future, change ApiBase to "/api/v1" and all routes update automatically.
/// Decision tracked in ROADMAP.md under "Decisions Needed".
/// </remarks>
public static class ApiRoutes
{
    // Note: Change to "/api/v1" when versioning decision is made
    private const string ApiBase = "/api";

    /// <summary>
    /// Base path for authentication endpoints.
    /// </summary>
    public const string Auth = $"{ApiBase}/auth";

    /// <summary>
    /// Login endpoint: POST /api/auth/login
    /// Accepts: LoginRequest
    /// Returns: AuthResponse (200 OK) or validation errors (400 Bad Request) or unauthorized (401)
    /// </summary>
    public const string AuthLogin = $"{Auth}/login";

    /// <summary>
    /// Register endpoint: POST /api/auth/register
    /// Accepts: RegisterRequest
    /// Returns: AuthResponse (200 OK) or validation errors (400 Bad Request) or conflict (409)
    /// </summary>
    public const string AuthRegister = $"{Auth}/register";

    /// <summary>
    /// Refresh token endpoint: POST /api/auth/refresh
    /// Accepts: RefreshTokenRequest
    /// Returns: AuthResponse (200 OK) or unauthorized (401)
    /// </summary>
    public const string AuthRefresh = $"{Auth}/refresh";

    /// <summary>
    /// Logout endpoint: POST /api/auth/logout
    /// Accepts: RefreshTokenRequest
    /// Returns: 200 OK (no content)
    /// </summary>
    public const string AuthLogout = $"{Auth}/logout";

    /// <summary>
    /// Health check endpoint: GET /api/health
    /// Returns: HealthCheckResponse with status, version, and timestamp
    /// </summary>
    public const string Health = "/api/health";

    // Future endpoints (uncomment as they're implemented):
    // 
    // /// <summary>
    // /// Get user's collection: GET /api/collections
    // /// Returns: List of collection entries for authenticated user
    // /// </summary>
    // public const string Collections = "/api/collections";
    //
    // /// <summary>
    // /// Search cards: GET /api/cards?q={query}
    // /// Returns: Paginated card search results
    // /// </summary>
    // public const string Cards = "/api/cards";
    //
    // /// <summary>
    // /// Get user's decklists: GET /api/decklists
    // /// Returns: List of decklists for authenticated user
    // /// </summary>
    // public const string Decklists = "/api/decklists";
}
