using System;

namespace MTGCollectionTracker.Shared.DTOs.Collections;

/// <summary>
/// Returned by GET /api/imports/{jobId}/status.
/// The client polls this endpoint until <see cref="Status"/> is "Completed" or "Failed".
/// </summary>
public record ImportJobStatusDto
{
    /// <summary>The import job ID.</summary>
    public Guid JobId { get; init; }

    /// <summary>
    /// Current job status: "Pending", "Processing", "Completed", or "Failed".
    /// Use string comparison rather than an enum so the DTO remains decoupled from
    /// the server-side <c>ImportJobStatus</c> enum.
    /// </summary>
    public string Status { get; init; } = string.Empty;

    /// <summary>Processing progress 0–100. Updated after each 1,000-row batch.</summary>
    public int Progress { get; init; }

    /// <summary>
    /// Populated when <see cref="Status"/> is "Completed".
    /// Null while the job is still pending or processing.
    /// </summary>
    public ManaboxImportResultDto? Result { get; init; }

    /// <summary>
    /// Human-readable error message when <see cref="Status"/> is "Failed".
    /// </summary>
    public string? Error { get; init; }
}
