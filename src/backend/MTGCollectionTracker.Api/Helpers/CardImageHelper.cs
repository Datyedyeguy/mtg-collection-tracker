using System.Collections.Generic;
using System.Text.Json;
using MTGCollectionTracker.Shared.DTOs.Cards;

namespace MTGCollectionTracker.Api.Helpers;

/// <summary>
/// Shared helper for extracting image URIs from Scryfall JSON data stored in the database.
/// Used by both CardsController and CollectionsController to avoid duplicating JSON parsing logic.
/// </summary>
public static class CardImageHelper
{
    /// <summary>
    /// Extracts the "normal" image URL for a card.
    /// For single-faced cards: reads the "normal" key from the ImageUris JSON object.
    /// For multi-faced cards: falls back to the first face's ImageUri.
    /// </summary>
    /// <param name="imageUrisJson">Raw Scryfall ImageUris JSON (snake_case keys like "normal", "small").</param>
    /// <param name="facesJson">Serialized List&lt;CardFaceDto&gt; JSON (PascalCase keys).</param>
    /// <returns>The "normal" image URL, or the front face image, or null.</returns>
    public static string? ExtractImageUri(string? imageUrisJson, string? facesJson)
    {
        // Single-faced path: parse the ImageUris JSON object (Scryfall format, snake_case keys)
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
        // Faces JSON is serialized from List<CardFaceDto> with PascalCase keys
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
}
