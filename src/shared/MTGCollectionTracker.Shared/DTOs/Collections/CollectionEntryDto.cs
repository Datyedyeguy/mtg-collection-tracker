using System;
using MTGCollectionTracker.Shared.Enums;

namespace MTGCollectionTracker.Shared.DTOs.Collections;

/// <summary>
/// Represents a single card in a user's collection on a specific platform.
/// </summary>
public class CollectionEntryDto
{
    /// <summary>
    /// Unique identifier for this collection entry.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// The card's unique identifier.
    /// </summary>
    public Guid CardId { get; set; }

    /// <summary>
    /// The name of the card (e.g., "Lightning Bolt").
    /// </summary>
    public string CardName { get; set; } = string.Empty;

    /// <summary>
    /// The set code (e.g., "m21", "znr").
    /// </summary>
    public string SetCode { get; set; } = string.Empty;

    /// <summary>
    /// The collector number within the set (e.g., "123", "45a").
    /// </summary>
    public string CollectorNumber { get; set; } = string.Empty;

    /// <summary>
    /// The platform where this card exists (Paper, Arena, Mtgo).
    /// </summary>
    public Platform Platform { get; set; }

    /// <summary>
    /// Number of copies owned (1-999).
    /// </summary>
    public int Quantity { get; set; }

    /// <summary>
    /// Optional: When this card was acquired.
    /// </summary>
    public DateTime? AcquiredDate { get; set; }

    /// <summary>
    /// When this entry was created.
    /// </summary>
    public DateTime CreatedAt { get; set; }
}
