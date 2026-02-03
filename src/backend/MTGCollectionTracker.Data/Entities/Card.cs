using System;
using System.Collections.Generic;

namespace MTGCollectionTracker.Data.Entities;

/// <summary>
/// Represents a specific printing of a Magic: The Gathering card from Scryfall.
/// Each card object represents one printing (set + collector number combination).
/// Different printings of the same card share an OracleId but have unique ScryfallIds.
/// </summary>
/// <remarks>
/// Example: Lightning Bolt has been printed in many sets. Each printing is a separate Card entity.
/// - M21 Lightning Bolt: unique ScryfallId, same OracleId as other Lightning Bolts
/// - ZNR Lightning Bolt: different ScryfallId, same OracleId
/// </remarks>
public class Card
{
    /// <summary>
    /// Primary key for this card in our database.
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
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// The set code this card is from (e.g., "m21", "znr").
    /// </summary>
    public string SetCode { get; set; } = string.Empty;

    /// <summary>
    /// The collector number within the set (e.g., "123", "45a").
    /// Can contain letters for variants.
    /// </summary>
    public string CollectorNumber { get; set; } = string.Empty;

    /// <summary>
    /// The card's rarity (e.g., "common", "uncommon", "rare", "mythic", "special", "bonus").
    /// </summary>
    public string Rarity { get; set; } = string.Empty;

    /// <summary>
    /// MTG Arena's card ID (GrpId). Null if card is not available on Arena.
    /// Only ~16,000 cards have Arena IDs.
    /// </summary>
    public int? ArenaId { get; set; }

    /// <summary>
    /// Magic Online's card ID. Null if card is not available on MTGO.
    /// </summary>
    public int? MtgoId { get; set; }

    /// <summary>
    /// The mana cost in Scryfall notation (e.g., "{2}{U}{U}").
    /// Null for lands and cards without mana costs.
    /// </summary>
    public string? ManaCost { get; set; }

    /// <summary>
    /// Converted mana cost (mana value). 0.0 for lands and free spells.
    /// Can be fractional for Un-set cards.
    /// </summary>
    public decimal Cmc { get; set; }

    /// <summary>
    /// The card's type line (e.g., "Creature — Human Wizard", "Instant").
    /// </summary>
    public string TypeLine { get; set; } = string.Empty;

    /// <summary>
    /// The Oracle rules text for this card.
    /// </summary>
    public string? OracleText { get; set; }

    /// <summary>
    /// Power value for creatures. Can be non-numeric (e.g., "*", "1+*").
    /// Null for non-creatures.
    /// </summary>
    public string? Power { get; set; }

    /// <summary>
    /// Toughness value for creatures. Can be non-numeric (e.g., "*", "2+*").
    /// Null for non-creatures.
    /// </summary>
    public string? Toughness { get; set; }

    /// <summary>
    /// JSON array of color codes (e.g., ["W","U","B","R","G"]).
    /// Stored as JSONB in PostgreSQL.
    /// </summary>
    public string? Colors { get; set; }

    /// <summary>
    /// JSON object containing image URLs from Scryfall CDN.
    /// Example: {"small": "url", "normal": "url", "large": "url", "png": "url"}
    /// Stored as JSONB in PostgreSQL.
    /// </summary>
    public string? ImageUris { get; set; }

    /// <summary>
    /// JSON object describing format legality.
    /// Example: {"standard": "legal", "modern": "legal", "commander": "legal"}
    /// Values: "legal", "not_legal", "restricted", "banned"
    /// Stored as JSONB in PostgreSQL.
    /// </summary>
    public string? Legalities { get; set; }

    // TODO: Add 'Finishes' field after researching foil/etched tracking
    // - Store Scryfall's finishes array: ["nonfoil", "foil", "etched"]
    // - Needed for validation (can't add foil if card is nonfoil-only)
    // - Research items:
    //   1. How to handle foil-only cards?
    //   2. Are etched foils significant enough to track separately?
    //   3. Best database design: separate columns vs JSONB?
    // - Phase 3 addition

    // TODO: Add 'Prices' field after determining pricing strategy
    // - Need to research pricing sources: TCGPlayer, CardKingdom, Scryfall
    // - Separate prices per finish: usd, usd_foil, usd_etched
    // - Update frequency strategy (daily bulk data from Scryfall?)
    // - Phase 4+ feature

    /// <summary>
    /// When this card was added to the database.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// When this card was last updated (e.g., from Scryfall sync).
    /// </summary>
    public DateTime UpdatedAt { get; set; }

    // Navigation Properties

    /// <summary>
    /// Collection entries where users own this card.
    /// </summary>
    public ICollection<CollectionEntry> CollectionEntries { get; set; } = new List<CollectionEntry>();
}
