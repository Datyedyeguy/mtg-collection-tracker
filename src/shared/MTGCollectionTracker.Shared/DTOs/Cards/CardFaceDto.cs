using System.ComponentModel.DataAnnotations;

namespace MTGCollectionTracker.Shared.DTOs.Cards;

/// <summary>
/// Represents one face of a multi-faced Magic card.
/// Used for transform cards, modal DFCs, reversible cards, etc.
/// </summary>
/// <remarks>
/// This is OUR schema for card faces, simplified from Scryfall's structure.
/// We extract only the fields we need for display and functionality.
///
/// Examples:
/// - Transform: "Delver of Secrets" (front) / "Insectile Aberration" (back)
/// - Modal DFC: "Alrund, God of the Cosmos" (front) / "Hakka, Whispering Raven" (back)
/// - Reversible: Same card, different artwork on each side
/// </remarks>
public class CardFaceDto
{
    /// <summary>
    /// The name of this face (e.g., "Insectile Aberration").
    /// Required for all faces.
    /// </summary>
    [Required]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Mana cost for this face (e.g., "{2}{U}{U}").
    /// Null if this face has no mana cost (e.g., back of transform card).
    /// </summary>
    public string? ManaCost { get; set; }

    /// <summary>
    /// Type line for this face (e.g., "Creature — Human Insect").
    /// Required.
    /// </summary>
    [Required]
    public string TypeLine { get; set; } = string.Empty;

    /// <summary>
    /// Oracle text (rules text) for this face.
    /// Null for faces without text (e.g., some tokens).
    /// </summary>
    public string? OracleText { get; set; }

    /// <summary>
    /// Power value for creatures. Can be non-numeric (e.g., "*", "1+*").
    /// Null for non-creatures.
    /// </summary>
    public string? Power { get; set; }

    /// <summary>
    /// Toughness value for creatures. Can be non-numeric (e.g., "*", "1+*").
    /// Null for non-creatures.
    /// </summary>
    public string? Toughness { get; set; }

    /// <summary>
    /// URL to the card image for this face (from Scryfall CDN).
    /// We store the "normal" size image URL (488x680).
    /// Required for display purposes.
    /// </summary>
    [Required]
    public string ImageUri { get; set; } = string.Empty;

    /// <summary>
    /// Array of color codes for this face (e.g., ["U", "B"]).
    /// Empty array for colorless.
    /// </summary>
    public string[]? Colors { get; set; }
}
