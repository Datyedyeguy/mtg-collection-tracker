using System;
using System.Collections.Generic;

namespace MTGCollectionTracker.Shared.DTOs.Cards;

/// <summary>
/// Full detail view of a single card printing, including rules text, legalities,
/// power/toughness, and a list of all other printings that share the same Oracle ID.
/// Returned by GET /api/cards/{id}.
/// </summary>
public class CardDetailDto
{
    /// <summary>
    /// Our internal database ID for this specific printing.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Scryfall's unique identifier for this printing.
    /// </summary>
    public Guid ScryfallId { get; set; }

    /// <summary>
    /// Oracle ID shared by all printings of the same card across sets.
    /// </summary>
    public Guid OracleId { get; set; }

    /// <summary>
    /// The card's canonical name (e.g., "Lightning Bolt").
    /// For multi-faced cards this is the full combined name
    /// (e.g., "Delver of Secrets // Insectile Aberration").
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Alternate flavor name for showcase or crossover printings.
    /// Null for standard printings.
    /// </summary>
    public string? FlavorName { get; set; }

    /// <summary>
    /// The set code this printing is from (e.g., "m21", "znr").
    /// </summary>
    public string SetCode { get; set; } = string.Empty;

    /// <summary>
    /// Collector number within the set (e.g., "123", "45a").
    /// </summary>
    public string CollectorNumber { get; set; } = string.Empty;

    /// <summary>
    /// Rarity of this printing: "common", "uncommon", "rare", "mythic", "special", "bonus".
    /// </summary>
    public string Rarity { get; set; } = string.Empty;

    /// <summary>
    /// Mana cost in Scryfall notation (e.g., "{2}{U}{U}").
    /// Null for lands and cards without a mana cost.
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
    /// Oracle rules text. May be null for cards with no text box (e.g., vanilla creatures).
    /// Newlines are preserved and should be rendered as line breaks.
    /// </summary>
    public string? OracleText { get; set; }

    /// <summary>
    /// Power value for creatures and some other card types.
    /// Can be non-numeric (e.g., "*", "1+*").
    /// Null for non-creatures.
    /// </summary>
    public string? Power { get; set; }

    /// <summary>
    /// Toughness value for creatures.
    /// Can be non-numeric (e.g., "*", "2+*").
    /// Null for non-creatures.
    /// </summary>
    public string? Toughness { get; set; }

    /// <summary>
    /// Color codes for this card (e.g., ["W", "U"] for an Azorius card).
    /// Empty array for colorless. Null if not available.
    /// </summary>
    public string[]? Colors { get; set; }

    /// <summary>
    /// Available finishes for this printing (e.g., ["nonfoil", "foil", "etched"]).
    /// Null if finish data is unavailable.
    /// </summary>
    public string[]? Finishes { get; set; }

    /// <summary>
    /// URL to the "normal" size card image (488×680) from Scryfall CDN.
    /// For multi-faced cards, this is the front face.
    /// Null if no image is available.
    /// </summary>
    public string? ImageUri { get; set; }

    /// <summary>
    /// True when this card has multiple faces (transform, modal DFC, reversible).
    /// When true, <see cref="Faces"/> contains per-face data including individual images.
    /// </summary>
    public bool IsMultiFaced { get; set; }

    /// <summary>
    /// Face details for multi-faced cards. Null for single-faced cards.
    /// </summary>
    public List<CardFaceDto>? Faces { get; set; }

    /// <summary>
    /// MTG Arena card ID (GrpId). Null if not available on Arena.
    /// </summary>
    public int? ArenaId { get; set; }

    /// <summary>
    /// Magic Online card ID. Null if not available on MTGO.
    /// </summary>
    public int? MtgoId { get; set; }

    /// <summary>
    /// Format legality map.
    /// Keys are format names (e.g., "standard", "modern", "commander").
    /// Values are "legal", "not_legal", "restricted", or "banned".
    /// Null if legality data is not available.
    /// </summary>
    public Dictionary<string, string>? Legalities { get; set; }

    /// <summary>
    /// All printings of this card (same Oracle ID), ordered by set code then collector number.
    /// Includes the current printing so the UI can highlight it in the list.
    /// </summary>
    public List<CardPrintingDto> Printings { get; set; } = new();
}
