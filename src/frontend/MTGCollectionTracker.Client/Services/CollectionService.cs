using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using MTGCollectionTracker.Shared;
using MTGCollectionTracker.Shared.DTOs.Collections;
using MTGCollectionTracker.Shared.Enums;

// Alias to keep code concise when referring to the update request DTO
using UpdateRequest = MTGCollectionTracker.Shared.DTOs.Collections.UpdateCollectionEntryRequest;

namespace MTGCollectionTracker.Client.Services;

/// <summary>
/// Service for managing user collections via API calls.
/// </summary>
public interface ICollectionService
{
    /// <summary>
    /// Get the user's collection, optionally filtered by platform with pagination.
    /// </summary>
    /// <param name="platform">Optional platform filter</param>
    /// <param name="page">Page number (1-based)</param>
    /// <param name="pageSize">Items per page</param>
    /// <returns>Collection response with entries and metadata</returns>
    Task<(CollectionResponseDto? data, string? error)> GetCollectionAsync(
        Platform? platform = null,
        int page = 1,
        int pageSize = 50);

    /// <summary>
    /// Add a card to the user's collection.
    /// Uses upsert semantics: if the card already exists on that platform, quantities are accumulated.
    /// </summary>
    /// <param name="request">Card ID, platform, and quantities to add</param>
    /// <returns>Updated entry (200 OK) or new entry (201 Created), or an error message</returns>
    Task<(CollectionEntryDto? data, string? error)> AddToCollectionAsync(AddToCollectionRequest request);

    /// <summary>
    /// Get the user's ownership of a specific card across all platforms.
    /// </summary>
    /// <param name="cardId">The card's database ID</param>
    /// <returns>List of per-platform entries (empty if not owned), or an error message</returns>
    Task<(List<CollectionEntryDto>? data, string? error)> GetCardOwnershipAsync(Guid cardId);

    /// <summary>
    /// Update a collection entry's quantities (absolute values, not deltas).
    /// </summary>
    /// <param name="entryId">The collection entry ID</param>
    /// <param name="quantity">New nonfoil quantity</param>
    /// <param name="foilQuantity">New foil quantity</param>
    /// <returns>Updated entry or an error message</returns>
    Task<(CollectionEntryDto? data, string? error)> UpdateCollectionEntryAsync(Guid entryId, int quantity, int foilQuantity);

    /// <summary>
    /// Remove a card from the user's collection.
    /// </summary>
    /// <param name="entryId">The collection entry ID</param>
    /// <returns>Null on success, or an error message</returns>
    Task<string?> DeleteCollectionEntryAsync(Guid entryId);
}

/// <summary>
/// Implementation of ICollectionService for calling backend collection endpoints.
/// Auth token is injected automatically by the AuthorizationMessageHandler in the HttpClient pipeline.
/// </summary>
public class CollectionService : ICollectionService
{
    private readonly HttpClient _httpClient;

    public CollectionService(HttpClient httpClient)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
    }

    /// <inheritdoc />
    public async Task<(CollectionResponseDto? data, string? error)> GetCollectionAsync(
        Platform? platform = null,
        int page = 1,
        int pageSize = 50)
    {
        try
        {
            var url = ApiRoutes.CollectionsGet;

            // Build query parameters
            var queryParams = new List<string>
            {
                $"page={page}",
                $"pageSize={pageSize}"
            };

            if (platform.HasValue)
            {
                queryParams.Add($"platform={platform.Value}");
            }

            url += "?" + string.Join("&", queryParams);

            var response = await _httpClient.GetAsync(url);

            if (response.IsSuccessStatusCode)
            {
                var data = await response.Content.ReadFromJsonAsync<CollectionResponseDto>();
                if (data == null)
                {
                    return (null, "Invalid response from server");
                }

                return (data, null);
            }

            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                return (null, "You must be logged in to view your collection");
            }

            // Generic error for other status codes
            return (null, $"Failed to load collection: {response.StatusCode}");
        }
        catch (HttpRequestException)
        {
            return (null, "Unable to connect to server. Please check your connection.");
        }
        catch (Exception ex)
        {
            return (null, $"An unexpected error occurred: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public async Task<(CollectionEntryDto? data, string? error)> AddToCollectionAsync(AddToCollectionRequest request)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync(ApiRoutes.CollectionsAdd, request);

            if (response.IsSuccessStatusCode)
            {
                var data = await response.Content.ReadFromJsonAsync<CollectionEntryDto>();
                if (data == null)
                {
                    return (null, "Invalid response from server");
                }

                return (data, null);
            }

            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                return (null, "You must be logged in to manage your collection");
            }

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return (null, "Card not found");
            }

            if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
            {
                var message = await response.Content.ReadAsStringAsync();
                return (null, !string.IsNullOrWhiteSpace(message) ? message : "Invalid request");
            }

            return (null, $"Failed to add to collection: {response.StatusCode}");
        }
        catch (HttpRequestException)
        {
            return (null, "Unable to connect to server. Please check your connection.");
        }
        catch (Exception ex)
        {
            return (null, $"An unexpected error occurred: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public async Task<(List<CollectionEntryDto>? data, string? error)> GetCardOwnershipAsync(Guid cardId)
    {
        try
        {
            var response = await _httpClient.GetAsync(ApiRoutes.CollectionsGetByCard(cardId));

            if (response.IsSuccessStatusCode)
            {
                var data = await response.Content.ReadFromJsonAsync<List<CollectionEntryDto>>();
                return (data ?? [], null);
            }

            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                return (null, "You must be logged in to view your collection");
            }

            return (null, $"Failed to load ownership: {response.StatusCode}");
        }
        catch (HttpRequestException)
        {
            return (null, "Unable to connect to server. Please check your connection.");
        }
        catch (Exception ex)
        {
            return (null, $"An unexpected error occurred: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public async Task<(CollectionEntryDto? data, string? error)> UpdateCollectionEntryAsync(Guid entryId, int quantity, int foilQuantity)
    {
        try
        {
            var request = new UpdateRequest
            {
                Quantity = quantity,
                FoilQuantity = foilQuantity
            };

            var response = await _httpClient.PutAsJsonAsync(ApiRoutes.CollectionsUpdate(entryId), request);

            if (response.IsSuccessStatusCode)
            {
                var data = await response.Content.ReadFromJsonAsync<CollectionEntryDto>();
                if (data == null)
                {
                    return (null, "Invalid response from server");
                }

                return (data, null);
            }

            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                return (null, "You must be logged in to manage your collection");
            }

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return (null, "Collection entry not found");
            }

            if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
            {
                var message = await response.Content.ReadAsStringAsync();
                return (null, !string.IsNullOrWhiteSpace(message) ? message : "Invalid request");
            }

            return (null, $"Failed to update collection entry: {response.StatusCode}");
        }
        catch (HttpRequestException)
        {
            return (null, "Unable to connect to server. Please check your connection.");
        }
        catch (Exception ex)
        {
            return (null, $"An unexpected error occurred: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public async Task<string?> DeleteCollectionEntryAsync(Guid entryId)
    {
        try
        {
            var response = await _httpClient.DeleteAsync(ApiRoutes.CollectionsDelete(entryId));

            if (response.IsSuccessStatusCode)
            {
                return null; // success — no error
            }

            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                return "You must be logged in to manage your collection";
            }

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return "Collection entry not found";
            }

            return $"Failed to delete collection entry: {response.StatusCode}";
        }
        catch (HttpRequestException)
        {
            return "Unable to connect to server. Please check your connection.";
        }
        catch (Exception ex)
        {
            return $"An unexpected error occurred: {ex.Message}";
        }
    }
}
