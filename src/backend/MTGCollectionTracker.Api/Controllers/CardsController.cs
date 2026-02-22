using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MTGCollectionTracker.Data;
using MTGCollectionTracker.Shared.DTOs.Cards;

namespace MTGCollectionTracker.Api.Controllers;

/// <summary>
/// Endpoints for searching and retrieving card data synced from Scryfall.
/// Card data is public — no authentication is required to search cards.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class CardsController : ControllerBase
{
    private readonly AppDbContext _dbContext;

    public CardsController(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <summary>
    /// Search for cards by name, set, or type line.
    /// At least one search parameter must be provided.
    /// </summary>
    /// <param name="q">Card name (partial, case-insensitive). Example: "lightning bolt"</param>
    /// <param name="set">Set code (exact match, case-insensitive). Example: "m21"</param>
    /// <param name="type">Type line (partial, case-insensitive). Example: "creature"</param>
    /// <param name="allPrintings">When false (default), returns one result per unique card (Oracle ID).
    /// When true, returns every printing (one row per set).</param>
    /// <param name="page">Page number (1-based, default: 1)</param>
    /// <param name="pageSize">Results per page (default: 20, max: 100)</param>
    /// <returns>Paginated card search results</returns>
    /// <response code="200">Cards matching the search criteria</response>
    /// <response code="400">No search parameters provided, or invalid pagination values</response>
    [HttpGet]
    [ProducesResponseType(typeof(CardSearchResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CardSearchResponseDto>> SearchCards(
        [FromQuery] string? q = null,
        [FromQuery] string? set = null,
        [FromQuery] string? type = null,
        [FromQuery] bool allPrintings = false,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        // Validate that at least one search parameter was provided
        if (string.IsNullOrWhiteSpace(q) && string.IsNullOrWhiteSpace(set) && string.IsNullOrWhiteSpace(type))
        {
            return BadRequest("At least one search parameter is required: q (name), set (set code), or type (type line).");
        }

        if (page < 1)
        {
            return BadRequest("Page number must be at least 1.");
        }

        if (pageSize < 1 || pageSize > 100)
        {
            return BadRequest("Page size must be between 1 and 100.");
        }

        var query = _dbContext.Cards.AsQueryable();

        // Name search: partial, case-insensitive — matches both the canonical name and
        // any flavor name (e.g., searching "lightnin" matches "Lightning, Lone Commando",
        // which is a showcase printing of "Isshin, Two Heavens as One").
        if (!string.IsNullOrWhiteSpace(q))
        {
            var nameLower = q.Trim().ToLower();
            query = query.Where(c =>
                c.Name.ToLower().Contains(nameLower) ||
                (c.FlavorName != null && c.FlavorName.ToLower().Contains(nameLower)));
        }

        // Set filter: exact match on set code (normalize to lowercase)
        if (!string.IsNullOrWhiteSpace(set))
        {
            var setLower = set.Trim().ToLower();
            query = query.Where(c => c.SetCode == setLower);
        }

        // Type line filter: partial, case-insensitive
        if (!string.IsNullOrWhiteSpace(type))
        {
            var typeLower = type.Trim().ToLower();
            query = query.Where(c => c.TypeLine.ToLower().Contains(typeLower));
        }

        // When not showing all printings, deduplicate to one card per Oracle ID.
        // Strategy: fetch (Id, OracleId) pairs for all filter-matching cards, group
        // in memory, then re-query by the representative IDs.
        //
        // Why not MIN(Id) as a subquery? PostgreSQL has no MIN aggregate for uuid types.
        // The in-memory EF provider works fine (Guid implements IComparable in .NET),
        // but Postgres rejects it. The two-step approach is safe here because at least
        // one filter is always required (validated above), so the Id list is bounded.
        // Tracks OracleId → flavor name that caused the match, when the representative card
        // matched via a *different* printing's flavor name rather than its own name.
        // Used to show "aka: Lightning, Lone Commando" in search results.
        var matchedFlavorNameByOracleId = new Dictionary<Guid, string>();
        var matchedImageUriByOracleId = new Dictionary<Guid, string>();

        if (!allPrintings)
        {
            // Step 1: collect the OracleIds that have any printing matching the filter.
            var matchingOracleIds = await query
                .Select(c => c.OracleId)
                .Distinct()
                .ToListAsync();

            // Step 2: from ALL printings of those cards, pick one representative per OracleId.
            // Include ImageUris and Faces so we can surface the matched printing's art.
            var allPrintingsForOracles = await _dbContext.Cards
                .Where(c => matchingOracleIds.Contains(c.OracleId))
                .Select(c => new { c.Id, c.OracleId, c.Name, c.FlavorName, c.ImageUris, c.Faces })
                .ToListAsync();

            var qLower = q?.Trim().ToLower();

            foreach (var group in allPrintingsForOracles.GroupBy(c => c.OracleId))
            {
                var representative = group.FirstOrDefault(c => c.FlavorName == null) ?? group.First();

                // If the representative's own name doesn't match the query, the hit came from
                // a flavor-name printing — record the flavor name and its image.
                if (!string.IsNullOrEmpty(qLower) && !representative.Name.ToLower().Contains(qLower))
                {
                    var flavorMatch = group.FirstOrDefault(c =>
                        c.FlavorName != null && c.FlavorName.ToLower().Contains(qLower));
                    if (flavorMatch?.FlavorName != null)
                    {
                        matchedFlavorNameByOracleId[group.Key] = flavorMatch.FlavorName;
                        var matchedImage = ExtractImageUri(flavorMatch.ImageUris, flavorMatch.Faces);
                        if (matchedImage != null)
                            matchedImageUriByOracleId[group.Key] = matchedImage;
                    }
                }
            }

            var representativeIds = allPrintingsForOracles
                .GroupBy(c => c.OracleId)
                .Select(g => g.FirstOrDefault(c => c.FlavorName == null)?.Id ?? g.First().Id)
                .ToList();

            query = _dbContext.Cards.Where(c => representativeIds.Contains(c.Id));
        }

        var totalCards = await query.CountAsync();
        var totalPages = (int)Math.Ceiling(totalCards / (double)pageSize);

        // Fetch raw card data — JSON columns are returned as strings and
        // deserialized in memory after the query (JSON parsing can't run in SQL).
        var rawCards = await query
            .OrderBy(c => c.Name)
            .ThenBy(c => c.SetCode)
            .ThenBy(c => c.CollectorNumber)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(c => new
            {
                c.Id,
                c.ScryfallId,
                c.OracleId,
                c.Name,
                c.FlavorName,
                c.SetCode,
                c.CollectorNumber,
                c.Rarity,
                c.ManaCost,
                c.Cmc,
                c.TypeLine,
                c.OracleText,
                c.Colors,
                c.ImageUris,
                c.Faces,
                c.Finishes,
                c.ArenaId,
                c.MtgoId,
            })
            .ToListAsync();

        // Map raw database rows to DTOs, deserializing JSON columns in memory
        var results = rawCards.Select(card => new CardSearchResultDto
        {
            Id = card.Id,
            ScryfallId = card.ScryfallId,
            OracleId = card.OracleId,
            Name = card.Name,
            FlavorName = card.FlavorName,
            SetCode = card.SetCode,
            CollectorNumber = card.CollectorNumber,
            Rarity = card.Rarity,
            ManaCost = card.ManaCost,
            Cmc = card.Cmc,
            TypeLine = card.TypeLine,
            OracleText = card.OracleText,
            Colors = DeserializeJsonArray(card.Colors),
            Finishes = DeserializeJsonArray(card.Finishes),
            ImageUri = ExtractImageUri(card.ImageUris, card.Faces),
            IsMultiFaced = card.Faces != null,
            Faces = card.Faces != null
                ? JsonSerializer.Deserialize<List<CardFaceDto>>(card.Faces)
                : null,
            ArenaId = card.ArenaId,
            MtgoId = card.MtgoId,
            MatchedFlavorName = matchedFlavorNameByOracleId.GetValueOrDefault(card.OracleId),
            MatchedImageUri = matchedImageUriByOracleId.GetValueOrDefault(card.OracleId),
        }).ToList();

        return Ok(new CardSearchResponseDto
        {
            Cards = results,
            TotalCards = totalCards,
            Page = page,
            PageSize = pageSize,
            TotalPages = totalPages,
            Query = q?.Trim() ?? string.Empty,
            AllPrintings = allPrintings,
        });
    }

    /// <summary>
    /// Extracts the "normal" image URL for a card.
    /// For single-faced cards: reads the "normal" key from the ImageUris JSON object.
    /// For multi-faced cards: falls back to the first face's ImageUri.
    /// </summary>
    private static string? ExtractImageUri(string? imageUrisJson, string? facesJson)
    {
        // Single-faced path: parse the ImageUris JSON object
        if (!string.IsNullOrEmpty(imageUrisJson))
        {
            try
            {
                using var doc = JsonDocument.Parse(imageUrisJson);
                if (doc.RootElement.TryGetProperty("normal", out var normalUrl))
                {
                    return normalUrl.GetString();
                }
            }
            catch (JsonException)
            {
                // Malformed JSON — fall through to face fallback
            }
        }

        // Multi-faced path: use the front face's image
        if (!string.IsNullOrEmpty(facesJson))
        {
            try
            {
                var faces = JsonSerializer.Deserialize<List<CardFaceDto>>(facesJson);
                return faces?.Count > 0 ? faces[0].ImageUri : null;
            }
            catch (JsonException)
            {
                // Malformed JSON — return null
            }
        }

        return null;
    }

    /// <summary>
    /// Deserializes a JSON string array column (e.g., Colors, Finishes) into a string array.
    /// Returns null if the input is null or parsing fails.
    /// </summary>
    private static string[]? DeserializeJsonArray(string? json)
    {
        if (string.IsNullOrEmpty(json))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<string[]>(json);
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
