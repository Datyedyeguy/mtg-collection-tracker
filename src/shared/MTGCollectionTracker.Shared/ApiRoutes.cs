using System;

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

    /// <summary>
    /// Base path for collection endpoints.
    /// </summary>
    public const string Collections = $"{ApiBase}/collections";

    /// <summary>
    /// Get user's collection: GET /api/collections?platform={platform}
    /// Optional query param: platform (Paper, Arena, Mtgo)
    /// Returns: CollectionResponseDto with entries and metadata
    /// </summary>
    public const string CollectionsGet = Collections;

    /// <summary>
    /// Add a card to the user's collection: POST /api/collections
    /// Accepts: AddToCollectionRequest
    /// Returns: CollectionEntryDto (201 Created for new entry, 200 OK for upsert)
    /// </summary>
    public const string CollectionsAdd = Collections;

    /// <summary>
    /// Update a collection entry's quantities: PUT /api/collections/{id}
    /// Accepts: UpdateCollectionEntryRequest (absolute quantities, not deltas)
    /// Returns: CollectionEntryDto (200 OK) or 404 if not found
    /// </summary>
    public static string CollectionsUpdate(Guid id) => $"{Collections}/{id}";

    /// <summary>
    /// Remove a card from the user's collection: DELETE /api/collections/{id}
    /// Returns: 204 No Content on success, 404 if not found
    /// </summary>
    public static string CollectionsDelete(Guid id) => $"{Collections}/{id}";

    /// <summary>
    /// Get the user's ownership of a specific card across all platforms: GET /api/collections/card/{cardId}
    /// Returns: List&lt;CollectionEntryDto&gt; (empty list if not owned on any platform)
    /// </summary>
    public static string CollectionsGetByCard(Guid cardId) => $"{Collections}/card/{cardId}";

    /// <summary>
    /// Base path for card endpoints.
    /// </summary>
    public const string Cards = $"{ApiBase}/cards";

    /// <summary>
    /// Search cards: GET /api/cards?q={name}&amp;set={setCode}&amp;type={typeLine}&amp;page={page}&amp;pageSize={pageSize}
    /// At least one of q, set, or type is required.
    /// Returns: CardSearchResponseDto with matching cards and pagination info.
    /// </summary>
    public const string CardsSearch = Cards;

    /// <summary>
    /// Get full card details: GET /api/cards/{id}
    /// Returns: CardDetailDto with rules text, legalities, and all alternate printings.
    /// Returns 404 if the card ID is not found.
    /// </summary>
    public static string CardsGetById(Guid id) => $"{Cards}/{id}";

    // Future endpoints (uncomment as they're implemented):
    //
    // /// <summary>
    // /// Get user's decklists: GET /api/decklists
    // /// Returns: List of decklists for authenticated user
    // /// </summary>
    // public const string Decklists = "/api/decklists";
}
