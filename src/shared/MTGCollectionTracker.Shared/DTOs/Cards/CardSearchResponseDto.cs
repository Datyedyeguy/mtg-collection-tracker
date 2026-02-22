using System.Collections.Generic;

namespace MTGCollectionTracker.Shared.DTOs.Cards;

/// <summary>
/// Paginated response returned from the card search endpoint (GET /api/cards).
/// </summary>
public class CardSearchResponseDto
{
    /// <summary>
    /// The cards matching the search criteria for the requested page.
    /// </summary>
    public List<CardSearchResultDto> Cards { get; set; } = [];

    /// <summary>
    /// Total number of cards matching the search criteria across all pages.
    /// Use this with PageSize to calculate TotalPages on the client if needed.
    /// </summary>
    public int TotalCards { get; set; }

    /// <summary>
    /// The current page number (1-based).
    /// </summary>
    public int Page { get; set; }

    /// <summary>
    /// Number of cards per page requested.
    /// </summary>
    public int PageSize { get; set; }

    /// <summary>
    /// Total number of pages available.
    /// Calculated as ceil(TotalCards / PageSize).
    /// </summary>
    public int TotalPages { get; set; }

    /// <summary>
    /// The name query that was searched, if any.
    /// Empty string if no name query was provided.
    /// </summary>
    public string Query { get; set; } = string.Empty;

    /// <summary>
    /// When true, results include every printing of each card.
    /// When false (default), only one printing per unique card (Oracle ID) is returned.
    /// </summary>
    public bool AllPrintings { get; set; }
}
