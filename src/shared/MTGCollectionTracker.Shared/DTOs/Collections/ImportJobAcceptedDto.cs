using System;

namespace MTGCollectionTracker.Shared.DTOs.Collections;

/// <summary>
/// Returned immediately (202 Accepted) when a Manabox import is submitted.
/// The client uses <see cref="StatusUrl"/> to poll for progress.
/// </summary>
public record ImportJobAcceptedDto
{
    /// <summary>Unique ID of the background import job.</summary>
    public Guid JobId { get; init; }

    /// <summary>
    /// Relative URL to poll for job status.
    /// Example: <c>/api/imports/3fa85f64-.../status</c>
    /// </summary>
    public string StatusUrl { get; init; } = string.Empty;
}
