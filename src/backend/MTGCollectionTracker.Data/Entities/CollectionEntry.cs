using System;

namespace MTGCollectionTracker.Data.Entities;

/// <summary>
/// Represents a user's ownership of a specific card on a specific platform.
/// Tracks how many copies of a card a user owns (Paper, Arena, or MTGO).
/// </summary>
/// <remarks>
/// Examples:
/// - User owns 3 copies of Lightning Bolt (M21) on Paper
/// - User owns 1 copy of Lightning Bolt (M21) on Arena
/// These would be two separate CollectionEntry records.
/// </remarks>
public class CollectionEntry
{
    /// <summary>
    /// Primary key for this collection entry.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// The user who owns this card. References ApplicationUser (ASP.NET Identity).
    /// </summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// The card being owned. References Card entity.
    /// </summary>
    public Guid CardId { get; set; }

    /// <summary>
    /// Which platform this card is on (Paper, Arena, or MTGO).
    /// Stored as string in database for readability.
    /// </summary>
    public Platform Platform { get; set; }

    /// <summary>
    /// How many copies of this card the user owns (1-999).
    /// Currently combines all finishes (foil/nonfoil/etched).
    /// </summary>
    /// <remarks>
    /// TODO: Research foil/nonfoil/etched finish tracking
    /// - Should we split into Quantity, FoilQuantity, EtchedQuantity?
    /// - Or use JSONB field for flexible finish tracking?
    /// - Edge cases: foil-only cards, etched variants
    /// - Phase 3 decision after Scryfall bulk data research
    /// </remarks>
    public int Quantity { get; set; }

    /// <summary>
    /// Optional: When this card was acquired.
    /// Null if not tracked or unknown.
    /// </summary>
    public DateTime? AcquiredDate { get; set; }

    // TODO: Add Location/Container tracking (Phase 6 - Decklist Management)
    // Planned features:
    // - Track which deck/binder/box the card is in
    // - Remember source location when moved to deck
    // - Return to source when removed from deck
    // Design: Separate Container and ContainerEntry entities
    // - Container: Deck, Binder, or Box
    // - ContainerEntry: Allocation of cards to containers
    // - Unallocated vs allocated quantity tracking

    /// <summary>
    /// When this collection entry was created.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// When this collection entry was last updated.
    /// </summary>
    public DateTime UpdatedAt { get; set; }

    // Navigation Properties

    /// <summary>
    /// The user who owns this card (EF Core navigation property).
    /// </summary>
    public ApplicationUser User { get; set; } = null!;

    /// <summary>
    /// The card being owned (EF Core navigation property).
    /// </summary>
    public Card Card { get; set; } = null!;
}
