using System.Collections.Generic;
using MTGCollectionTracker.Shared.Enums;

namespace MTGCollectionTracker.Shared.DTOs.Collections;

/// <summary>
/// Response containing a user's collection entries with metadata.
/// </summary>
public class CollectionResponseDto
{
    /// <summary>
    /// List of collection entries.
    /// </summary>
    public List<CollectionEntryDto> Entries { get; set; } = new();

    /// <summary>
    /// Total number of unique cards across all platforms.
    /// </summary>
    public int TotalUniqueCards { get; set; }

    /// <summary>
    /// Total number of cards counting all copies.
    /// </summary>
    public int TotalCards { get; set; }

    /// <summary>
    /// Number of cards grouped by platform.
    /// </summary>
    public Dictionary<Platform, int> CardsByPlatform { get; set; } = new();

    /// <summary>
    /// Current page number (1-based).
    /// </summary>
    public int CurrentPage { get; set; } = 1;

    /// <summary>
    /// Number of items per page.
    /// </summary>
    public int PageSize { get; set; } = 50;

    /// <summary>
    /// Total number of pages available.
    /// </summary>
    public int TotalPages { get; set; }

    /// <summary>
    /// Whether there are more pages after the current one.
    /// </summary>
    public bool HasNextPage => CurrentPage < TotalPages;

    /// <summary>
    /// Whether there are pages before the current one.
    /// </summary>
    public bool HasPreviousPage => CurrentPage > 1;
}
