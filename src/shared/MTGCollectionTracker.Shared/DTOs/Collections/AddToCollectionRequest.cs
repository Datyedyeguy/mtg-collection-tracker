using System;
using MTGCollectionTracker.Shared.Enums;

namespace MTGCollectionTracker.Shared.DTOs.Collections;

/// <summary>
/// Request body for adding cards to a user's collection (POST /api/collections).
/// At least one of <see cref="Quantity"/> or <see cref="FoilQuantity"/> must be greater than zero.
/// </summary>
/// <remarks>
/// Upsert semantics: if the user already owns this card on this platform,
/// the quantities are added to the existing entry rather than returning a conflict error.
/// </remarks>
public record AddToCollectionRequest
{
    /// <summary>
    /// The Scryfall ID of the specific printing to add (e.g., Lightning Bolt from M21).
    /// Must refer to a card that exists in the local Scryfall-synced database.
    /// </summary>
    public Guid CardId { get; init; }

    /// <summary>
    /// The platform where the user owns this card.
    /// </summary>
    public Platform Platform { get; init; }

    /// <summary>
    /// Number of nonfoil copies to add (0 or more).
    /// </summary>
    public int Quantity { get; init; }

    /// <summary>
    /// Number of foil copies to add (0 or more).
    /// Meaningful for Paper and MTGO; cosmetic-only on Arena but still accepted.
    /// </summary>
    public int FoilQuantity { get; init; }
}
