using System;
using System.Collections.Generic;

namespace MTGCollectionTracker.Data.Entities;

/// <summary>
/// Lifecycle states for an <see cref="ImportJob"/>.
/// </summary>
public enum ImportJobStatus
{
    /// <summary>Accepted and waiting in the queue — no worker has picked it up yet.</summary>
    Pending,

    /// <summary>A worker is actively processing this job.</summary>
    Processing,

    /// <summary>All rows processed successfully (some may have been skipped).</summary>
    Completed,

    /// <summary>Processing failed with an unrecoverable error.</summary>
    Failed
}

/// <summary>
/// Represents an asynchronous background import job for a Manabox CSV export.
///
/// Lifecycle:
///   1. Controller validates the upload, writes a Pending record, enqueues the job ID.
///   2. <c>ImportWorkerService</c> picks up the ID, sets status = Processing, and processes
///      the CSV in 1,000-row batches. Progress (0–100) is updated after each batch.
///   3. On success: status = Completed, import statistics populated.
///      On error:   status = Failed, ErrorMessage populated.
///
/// Durability: because the row is persisted _before_ the job ID is enqueued in the
/// in-memory channel, a server restart cannot silently lose a job. The worker re-scans
/// for Pending / Processing jobs at startup and re-enqueues them.
/// </summary>
public class ImportJob
{
    /// <summary>Primary key (also the job token returned to the client).</summary>
    public Guid Id { get; set; }

    /// <summary>The user who submitted this import.</summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>Current lifecycle state.</summary>
    public ImportJobStatus Status { get; set; } = ImportJobStatus.Pending;

    /// <summary>Processing progress 0–100. Updated after each 1,000-row batch.</summary>
    public int Progress { get; set; }

    /// <summary>Original file name (display only).</summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>Import mode stored as string ("Accumulate" or "Replace").</summary>
    public string Mode { get; set; } = string.Empty;

    /// <summary>
    /// JSON-serialised <c>List&lt;string&gt;</c> of binder names the user selected.
    /// Stored as text because EF / Npgsql stores <c>string[]</c> as a Postgres array,
    /// but we want portable JSON that Blazor will also understand.
    /// </summary>
    public string IncludedBindersJson { get; set; } = "[]";

    /// <summary>
    /// Raw CSV file bytes, stored until the worker has finished processing.
    /// Max supported Manabox export is ~50 MB. Use <c>bytea</c> in Postgres.
    /// </summary>
    public byte[] CsvBytes { get; set; } = [];

    // ── Result fields (populated on Completed / partial on Failed) ──────────

    /// <summary>Number of new Paper collection entries created.</summary>
    public int Imported { get; set; }

    /// <summary>Number of existing Paper entries whose quantities were updated.</summary>
    public int Updated { get; set; }

    /// <summary>Number of rows skipped (Scryfall ID not found in local DB).</summary>
    public int Skipped { get; set; }

    /// <summary>JSON-serialised <c>List&lt;string&gt;</c> of skipped card names.</summary>
    public string SkippedCardsJson { get; set; } = "[]";

    /// <summary>Human-readable error description when <see cref="Status"/> is Failed.</summary>
    public string? ErrorMessage { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Navigation property
    public ApplicationUser User { get; set; } = null!;
}
