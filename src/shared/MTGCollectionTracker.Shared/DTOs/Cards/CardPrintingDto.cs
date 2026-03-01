using System;

namespace MTGCollectionTracker.Shared.DTOs.Cards;

/// <summary>
/// Compact representation of a single card printing, used in the "Other Printings" section
/// of the card detail page. Contains just enough data to display a thumbnail and navigate
/// to the full detail page for that printing.
/// </summary>
public record CardPrintingDto
{
    /// <summary>
    /// Our internal database ID for this printing.
    /// Use this to navigate to /cards/{Id} for the full detail view.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// The set code this printing belongs to (e.g., "m21", "znr").
    /// Displayed as a label under the card thumbnail.
    /// </summary>
    public string SetCode { get; set; } = string.Empty;

    /// <summary>
    /// Collector number within the set (e.g., "123", "45a").
    /// Shown alongside the set code to disambiguate multiple printings in one set.
    /// </summary>
    public string CollectorNumber { get; set; } = string.Empty;

    /// <summary>
    /// Rarity of this printing: "common", "uncommon", "rare", "mythic", "special", "bonus".
    /// </summary>
    public string Rarity { get; set; } = string.Empty;

    /// <summary>
    /// Available finishes for this printing (e.g., ["nonfoil", "foil", "etched"]).
    /// Null if finish data is unavailable.
    /// </summary>
    public string[]? Finishes { get; set; }

    /// <summary>
    /// URL to the "normal" size card image (488×680) from Scryfall CDN.
    /// Null if no image is available for this printing.
    /// </summary>
    public string? ImageUri { get; set; }

    /// <summary>
    /// Alternate flavor name for showcase or crossover printings.
    /// Null for standard printings.
    /// </summary>
    public string? FlavorName { get; set; }
}
