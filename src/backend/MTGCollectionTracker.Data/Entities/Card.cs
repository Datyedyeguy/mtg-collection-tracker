using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;
using MTGCollectionTracker.Shared.DTOs.Cards;

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
    /// Alternate flavor name for showcase/crossover printings (e.g., "Lightning, Lone Commando"
    /// for a Final Fantasy printing of "Isshin, Two Heavens as One").
    /// Null for standard printings.
    /// </summary>
    public string? FlavorName { get; set; }

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

    /// <summary>
    /// JSON array of available finishes for this card printing.
    /// Example: ["nonfoil", "foil"] or ["nonfoil", "foil", "etched"]
    /// Possible values: "nonfoil", "foil", "etched"
    /// Used for validation - users can only add copies with finishes that exist.
    /// Stored as JSONB in PostgreSQL.
    /// </summary>
    public string? Finishes { get; set; }

    /// <summary>
    /// Backing field for Faces property.
    /// </summary>
    private string? _faces;

    /// <summary>
    /// JSON array containing all card faces for multi-faced cards.
    /// Null for single-faced cards.
    ///
    /// Internal storage - use CardFaces property for type-safe access.
    /// Stored as JSONB in PostgreSQL.
    /// </summary>
    public string? Faces
    {
        get => _faces;
        set
        {
            _faces = value;
            _cardFaces = null; // Clear cache when underlying data changes
        }
    }

    /// <summary>
    /// Cached deserialized card faces to avoid repeated JSON parsing.
    /// </summary>
    private List<CardFaceDto>? _cardFaces;

    /// <summary>
    /// Typed access to card faces for multi-faced cards.
    /// Null for single-faced cards.
    ///
    /// For transform cards (e.g., "Delver of Secrets // Insectile Aberration"):
    /// - Contains both front and back face with complete data for each
    ///
    /// For modal DFCs (e.g., "Alrund, God of the Cosmos // Hakka, Whispering Raven"):
    /// - Contains both faces that can be cast
    ///
    /// For reversible cards (Secret Lair promos):
    /// - Contains same card with different artwork on each face
    ///
    /// This property provides type-safe access to the Faces JSON column.
    /// Setting this property automatically serializes to the Faces column.
    /// The deserialized value is cached to avoid repeated JSON parsing.
    /// </summary>
    [NotMapped]
    public List<CardFaceDto>? CardFaces
    {
        get
        {
            if (_cardFaces == null && !string.IsNullOrEmpty(Faces))
            {
                _cardFaces = JsonSerializer.Deserialize<List<CardFaceDto>>(Faces);
            }
            return _cardFaces;
        }
        set
        {
            _cardFaces = value;
            Faces = value == null || value.Count == 0
                ? null
                : JsonSerializer.Serialize(value);
        }
    }

    // TODO: Add 'Prices' field after determining pricing strategy (Phase 4+)
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
