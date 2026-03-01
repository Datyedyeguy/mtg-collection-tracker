namespace MTGCollectionTracker.Shared.DTOs.Collections;

/// <summary>
/// Request body for updating a collection entry's quantities (PUT /api/collections/{id}).
/// Uses absolute values — the entry's quantities will be set to these values, not incremented.
/// At least one of <see cref="Quantity"/> or <see cref="FoilQuantity"/> must be greater than zero.
/// </summary>
/// <remarks>
/// To remove a card entirely, use DELETE /api/collections/{id} instead of setting both to zero.
/// </remarks>
public record UpdateCollectionEntryRequest
{
    /// <summary>
    /// New total number of nonfoil copies (0 or more).
    /// </summary>
    public int Quantity { get; init; }

    /// <summary>
    /// New total number of foil copies (0 or more).
    /// </summary>
    public int FoilQuantity { get; init; }
}
