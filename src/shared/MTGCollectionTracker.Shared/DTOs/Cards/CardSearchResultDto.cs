using System;
using System.Collections.Generic;

namespace MTGCollectionTracker.Shared.DTOs.Cards;

/// <summary>
/// Represents a single card returned from a card search query.
/// Contains display data and enough information to add the card to a collection.
/// </summary>
public class CardSearchResultDto
{
    /// <summary>
    /// Our internal database identifier for this card printing.
    /// Use this when adding the card to a collection.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Scryfall's unique identifier for this specific printing.
    /// </summary>
    public Guid ScryfallId { get; set; }

    /// <summary>
    /// Oracle ID links all printings of the same card together.
    /// All Lightning Bolts share the same OracleId regardless of set.
    /// </summary>
    public Guid OracleId { get; set; }

    /// <summary>
    /// The card's name (e.g., "Lightning Bolt").
    /// For multi-faced cards this is the full name (e.g., "Delver of Secrets // Insectile Aberration").
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Alternate flavor name for showcase/crossover printings.
    /// Example: "Lightning, Lone Commando" for the Final Fantasy printing of "Isshin, Two Heavens as One".
    /// Null for standard printings.
    /// </summary>
    public string? FlavorName { get; set; }

    /// <summary>
    /// The set code this card is from (e.g., "m21", "znr").
    /// </summary>
    public string SetCode { get; set; } = string.Empty;

    /// <summary>
    /// The collector number within the set (e.g., "123", "45a").
    /// </summary>
    public string CollectorNumber { get; set; } = string.Empty;

    /// <summary>
    /// The card's rarity: "common", "uncommon", "rare", "mythic", "special", "bonus".
    /// </summary>
    public string Rarity { get; set; } = string.Empty;

    /// <summary>
    /// Mana cost in Scryfall notation (e.g., "{2}{U}{U}").
    /// Null for lands and cards without mana costs.
    /// </summary>
    public string? ManaCost { get; set; }

    /// <summary>
    /// Converted mana cost / mana value (e.g., 2.0 for a {2} spell).
    /// </summary>
    public decimal Cmc { get; set; }

    /// <summary>
    /// The card's type line (e.g., "Creature — Human Wizard", "Instant").
    /// </summary>
    public string TypeLine { get; set; } = string.Empty;

    /// <summary>
    /// Oracle rules text. May be null for cards with no text box.
    /// </summary>
    public string? OracleText { get; set; }

    /// <summary>
    /// Color identity (e.g., ["U", "B"] for a blue-black card).
    /// Empty array for colorless. Null if unknown.
    /// </summary>
    public string[]? Colors { get; set; }

    /// <summary>
    /// URL to the "normal" size card image from Scryfall CDN (488×680).
    /// For multi-faced cards this is the front face image.
    /// Null if no image is available.
    /// </summary>
    public string? ImageUri { get; set; }

    /// <summary>
    /// True if this card has multiple faces (transform, modal DFC, reversible).
    /// When true, the Faces list contains per-face details including individual images.
    /// </summary>
    public bool IsMultiFaced { get; set; }

    /// <summary>
    /// Face data for multi-faced cards. Null for single-faced cards.
    /// </summary>
    public List<CardFaceDto>? Faces { get; set; }

    /// <summary>
    /// Available finishes for this printing (e.g., ["nonfoil", "foil"]).
    /// Null if finish data is not available.
    /// </summary>
    public string[]? Finishes { get; set; }

    /// <summary>
    /// MTG Arena card ID (GrpId). Null if not available on Arena.
    /// </summary>
    public int? ArenaId { get; set; }

    /// <summary>
    /// When the search matched this card via a different printing's flavor name, this holds
    /// that flavor name so the UI can display context (e.g., "aka: Lightning, Lone Commando").
    /// Null when the representative printing itself matched the query.
    /// Only populated in deduplicated (non-allPrintings) responses.
    /// </summary>
    public string? MatchedFlavorName { get; set; }

    /// <summary>
    /// Image URI from the flavor-name printing that triggered the search match.
    /// When set, show this image instead of <see cref="ImageUri"/> so the user
    /// sees the art they actually searched for (e.g., the Lightning showcase art).
    /// Null when the representative's own name matched the query.
    /// </summary>
    public string? MatchedImageUri { get; set; }

    /// <summary>
    /// Magic Online card ID. Null if not available on MTGO.
    /// </summary>
    public int? MtgoId { get; set; }
}
