using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using MTGCollectionTracker.Shared;
using MTGCollectionTracker.Shared.DTOs.Cards;

namespace MTGCollectionTracker.Client.Services;

/// <summary>
/// Service for searching cards via the card search API.
/// Authentication is required — the bearer token is automatically attached by AuthorizationMessageHandler.
/// </summary>
public interface ICardService
{
    /// <summary>
    /// Search cards by name, set, and/or type line.
    /// At least one of name, set, or type must be non-empty.
    /// </summary>
    /// <param name="name">Partial card name search (case-insensitive)</param>
    /// <param name="set">Exact set code (case-insensitive, e.g. "m21")</param>
    /// <param name="type">Partial type line search (case-insensitive)</param>
    /// <param name="allPrintings">When false (default), returns one result per unique card.
    /// When true, returns every printing.</param>
    /// <param name="page">Page number (1-based)</param>
    /// <param name="pageSize">Results per page</param>
    /// <returns>Tuple of (results, errorMessage). One will always be null.</returns>
    Task<(CardSearchResponseDto? data, string? error)> SearchAsync(
        string? name = null,
        string? set = null,
        string? type = null,
        bool allPrintings = false,
        int page = 1,
        int pageSize = 20);
}

/// <summary>
/// Implementation of ICardService that calls the backend card search endpoint.
/// Auth token is injected automatically by the AuthorizationMessageHandler in the HttpClient pipeline.
/// </summary>
public class CardService : ICardService
{
    private readonly HttpClient _httpClient;

    public CardService(HttpClient httpClient)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
    }

    /// <inheritdoc />
    public async Task<(CardSearchResponseDto? data, string? error)> SearchAsync(
        string? name = null,
        string? set = null,
        string? type = null,
        bool allPrintings = false,
        int page = 1,
        int pageSize = 20)
    {
        try
        {
            // Build query string from non-empty parameters
            var queryParams = new List<string>
            {
                $"page={page}",
                $"pageSize={pageSize}",
                $"allPrintings={allPrintings.ToString().ToLower()}"
            };

            if (!string.IsNullOrWhiteSpace(name))
            {
                queryParams.Add($"q={Uri.EscapeDataString(name.Trim())}");
            }

            if (!string.IsNullOrWhiteSpace(set))
            {
                queryParams.Add($"set={Uri.EscapeDataString(set.Trim())}");
            }

            if (!string.IsNullOrWhiteSpace(type))
            {
                queryParams.Add($"type={Uri.EscapeDataString(type.Trim())}");
            }

            var url = ApiRoutes.CardsSearch + "?" + string.Join("&", queryParams);
            var response = await _httpClient.GetAsync(url);

            if (response.IsSuccessStatusCode)
            {
                var data = await response.Content.ReadFromJsonAsync<CardSearchResponseDto>();
                if (data == null)
                {
                    return (null, "Invalid response from server.");
                }

                return (data, null);
            }

            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                return (null, "You must be logged in to search cards.");
            }

            if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
            {
                var message = await response.Content.ReadAsStringAsync();
                return (null, string.IsNullOrWhiteSpace(message)
                    ? "Invalid search request."
                    : message);
            }

            return (null, $"Search failed: {response.StatusCode}");
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
