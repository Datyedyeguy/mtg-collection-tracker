using System.Collections.Generic;

namespace MTGCollectionTracker.Shared.DTOs.Collections;

/// <summary>
/// Result returned by POST /api/collections/import/manabox.
/// </summary>
public record ManaboxImportResultDto
{
    /// <summary>
    /// Number of new collection entries created (card was not previously owned on this platform).
    /// </summary>
    public int Imported { get; init; }

    /// <summary>
    /// Number of existing collection entries whose quantities were updated.
    /// Always 0 when <see cref="ImportMode.Replace"/> is used (all entries are deleted first).
    /// </summary>
    public int Updated { get; init; }

    /// <summary>
    /// Number of rows skipped because the Scryfall ID in the CSV was not found in the local
    /// card database. Run <c>ScryfallSync</c> to pull in any missing cards.
    /// </summary>
    public int Skipped { get; init; }

    /// <summary>
    /// Human-readable names of the skipped cards, for display in the UI.
    /// Populated from the <c>Name</c> column of the CSV (not from the database).
    /// </summary>
    public List<string> SkippedCards { get; init; } = [];
}
