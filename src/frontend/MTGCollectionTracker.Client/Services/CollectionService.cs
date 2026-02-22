using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using MTGCollectionTracker.Shared;
using MTGCollectionTracker.Shared.DTOs.Collections;
using MTGCollectionTracker.Shared.Enums;

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
}
