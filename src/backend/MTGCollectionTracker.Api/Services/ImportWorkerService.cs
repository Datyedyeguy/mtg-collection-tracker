using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MTGCollectionTracker.Data;
using MTGCollectionTracker.Data.Entities;
using MTGCollectionTracker.Shared.DTOs.Collections;

namespace MTGCollectionTracker.Api.Services;

/// <summary>
/// Long-running background service that processes Manabox CSV import jobs one at a time.
///
/// Architecture overview:
/// <list type="bullet">
///   <item><c>ImportsController</c> validates the upload, writes a Pending <see cref="ImportJob"/>
///         to the database, then enqueues the job ID via <see cref="IImportJobQueue"/>.</item>
///   <item>This service loops on the channel, picks up each job ID, and processes the CSV
///         in 1,000-row UNNEST batches — updating <see cref="ImportJob.Progress"/> (0–100) after
///         each batch so the client's polling endpoint shows real progress.</item>
///   <item>On startup, stale Pending / Processing jobs from a previous server run are
///         re-enqueued automatically, so a restart cannot silently lose a job.</item>
/// </list>
/// </summary>
public sealed class ImportWorkerService : BackgroundService
{
    private const int BatchSize = 1_000;

    private readonly IImportJobQueue _queue;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IManaboxCsvParser _parser;
    private readonly ILogger<ImportWorkerService> _logger;

    public ImportWorkerService(
        IImportJobQueue queue,
        IServiceScopeFactory scopeFactory,
        IManaboxCsvParser parser,
        ILogger<ImportWorkerService> logger)
    {
        _queue = queue;
        _scopeFactory = scopeFactory;
        _parser = parser;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await ReEnqueueStaleJobsAsync(stoppingToken);

        await foreach (var jobId in _queue.Reader.ReadAllAsync(stoppingToken))
        {
            try
            {
                await ProcessJobAsync(jobId, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // Graceful shutdown — the job stays in Processing state and will be
                // re-enqueued by ReEnqueueStaleJobsAsync on next startup.
                break;
            }
            catch (Exception ex)
            {
                // Unexpected exception outside the per-job try/catch — log and continue.
                _logger.LogError(ex, "Unhandled exception processing import job {JobId}", jobId);
            }
        }
    }

    // ── Startup re-scan ───────────────────────────────────────────────────────

    /// <summary>
    /// On startup, re-enqueue any jobs that were Pending or Processing when the server
    /// last shut down. This covers the case where a restart happens mid-import.
    /// Processing jobs are restarted from the beginning (idempotent import logic).
    /// </summary>
    private async Task ReEnqueueStaleJobsAsync(CancellationToken ct)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var staleIds = await db.ImportJobs
            .Where(j => j.Status == ImportJobStatus.Pending || j.Status == ImportJobStatus.Processing)
            .Select(j => j.Id)
            .ToListAsync(ct);

        if (staleIds.Count > 0)
        {
            _logger.LogInformation(
                "Re-enqueueing {Count} stale import job(s) found on startup: {Ids}",
                staleIds.Count, string.Join(", ", staleIds));
        }

        // Reset Processing → Pending so the job shows an accurate status during re-scan
        if (staleIds.Count > 0)
        {
            await db.ImportJobs
                .Where(j => staleIds.Contains(j.Id) && j.Status == ImportJobStatus.Processing)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(j => j.Status, ImportJobStatus.Pending)
                    .SetProperty(j => j.Progress, 0)
                    .SetProperty(j => j.UpdatedAt, DateTime.UtcNow), ct);

            foreach (var id in staleIds)
                _queue.Enqueue(id);
        }
    }

    // ── Per-job processing ────────────────────────────────────────────────────

    private async Task ProcessJobAsync(Guid jobId, CancellationToken ct)
    {
        _logger.LogInformation("Starting import job {JobId}", jobId);

        await using var scope = _scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var job = await db.ImportJobs.FindAsync([jobId], ct);
        if (job is null)
        {
            _logger.LogWarning("Import job {JobId} not found in database — skipping", jobId);
            return;
        }

        // Skip jobs already in a terminal state. This can happen when the startup
        // re-scan re-enqueues a Pending job that was also enqueued by the controller
        // in the same process lifetime (e.g. in unit tests, or if StartAsync is
        // called after a job has already been submitted).
        if (job.Status is ImportJobStatus.Completed or ImportJobStatus.Failed)
        {
            _logger.LogInformation(
                "Import job {JobId} is already {Status} — skipping duplicate enqueue",
                jobId, job.Status);
            return;
        }

        // Mark as Processing
        job.Status = ImportJobStatus.Processing;
        job.Progress = 0;
        job.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);

        try
        {
            await RunImportAsync(job, db, ct);
            job.Status = ImportJobStatus.Completed;
            job.Progress = 100;
            job.UpdatedAt = DateTime.UtcNow;
            await db.SaveChangesAsync(ct);
            _logger.LogInformation(
                "Import job {JobId} completed: {Imported} imported, {Updated} updated, {Skipped} skipped",
                jobId, job.Imported, job.Updated, job.Skipped);
        }
        catch (Exception ex) when (!ct.IsCancellationRequested)
        {
            _logger.LogError(ex, "Import job {JobId} failed", jobId);
            job.Status = ImportJobStatus.Failed;
            job.ErrorMessage = ex.Message;
            job.UpdatedAt = DateTime.UtcNow;
            await db.SaveChangesAsync(CancellationToken.None); // persist failure even if ct is cancelled
        }
        finally
        {
            // Clear the raw CSV bytes immediately after processing to reclaim storage.
            // The result fields remain for the polling endpoint to read.
            if (job.Status is ImportJobStatus.Completed or ImportJobStatus.Failed)
            {
                job.CsvBytes = [];
                await db.SaveChangesAsync(CancellationToken.None);
            }
        }
    }

    private async Task RunImportAsync(ImportJob job, AppDbContext db, CancellationToken ct)
    {
        var includedBinders = JsonSerializer.Deserialize<List<string>>(job.IncludedBindersJson)
            ?? [];
        var includedSet = new HashSet<string>(includedBinders, StringComparer.OrdinalIgnoreCase);
        var importMode = Enum.Parse<ImportMode>(job.Mode);

        // ── Step 1: Parse CSV and aggregate by ScryfallId ─────────────────────
        var invalidNames = new List<string>();
        var aggregated = new List<ManaboxRow>();

        using var csvStream = new System.IO.MemoryStream(job.CsvBytes);
        await foreach (var row in _parser.ParseAsync(csvStream, includedSet, invalidNames, ct))
            aggregated.Add(row);

        // ── Step 2: Bulk-lookup matched cards (single query) ──────────────────
        var scryfallIds = aggregated.Select(r => r.ScryfallId).ToList();

        Dictionary<Guid, Guid> cardMap = [];
        if (scryfallIds.Count > 0)
        {
            cardMap = await db.Cards
                .Where(c => scryfallIds.Contains(c.ScryfallId))
                .Select(c => new { c.Id, c.ScryfallId })
                .ToDictionaryAsync(c => c.ScryfallId, c => c.Id, ct);
        }

        // ── Step 3: Separate matched from skipped ─────────────────────────────
        var matched = new List<(Guid CardId, int Qty, int FoilQty)>(aggregated.Count);
        var skippedNames = new List<string>(invalidNames);

        foreach (var row in aggregated)
        {
            if (cardMap.TryGetValue(row.ScryfallId, out var cardId))
                matched.Add((cardId, row.Quantity, row.FoilQuantity));
            else
                skippedNames.Add(row.CardName);
        }

        job.Skipped = skippedNames.Count;
        job.SkippedCardsJson = JsonSerializer.Serialize(skippedNames);
        job.TotalCopies = matched.Sum(r => r.Qty + r.FoilQty);

        if (matched.Count == 0)
        {
            job.Imported = 0;
            job.Updated = 0;
            return;
        }

        // ── Step 4: Count pre-existing Paper entries (Accumulate mode only) ───
        if (importMode == ImportMode.Accumulate)
        {
            var matchedCardIds = matched.Select(r => r.CardId).ToList();
            var existingCount = await db.CollectionEntries.CountAsync(
                ce => ce.UserId == job.UserId
                   && ce.Platform == Platform.Paper
                   && matchedCardIds.Contains(ce.CardId), ct);

            job.Updated = existingCount;
            job.Imported = matched.Count - existingCount;
        }
        else
        {
            job.Imported = matched.Count;
            job.Updated = 0;
        }

        // ── Step 5: Batch UNNEST upsert ───────────────────────────────────────
        var conn = db.Database.GetDbConnection();
        if (conn.State != System.Data.ConnectionState.Open)
            await conn.OpenAsync(ct);

        int totalBatches = (int)Math.Ceiling(matched.Count / (double)BatchSize);

        if (importMode == ImportMode.Replace)
        {
            // Delete ALL Paper entries for this user before starting the batches.
            // Done once here (not per-batch) to ensure atomicity of the clear.
            using var delCmd = conn.CreateCommand();
            delCmd.CommandText = """
                DELETE FROM "CollectionEntries"
                WHERE "UserId" = @userId AND "Platform" = 'Paper'
                """;
            AddStringParam(delCmd, "userId", job.UserId);
            await delCmd.ExecuteNonQueryAsync(ct);
        }

        const string accumulateSql = """
            INSERT INTO "CollectionEntries"
                ("Id", "UserId", "CardId", "Platform", "Quantity", "FoilQuantity", "EtchedQuantity", "CreatedAt", "UpdatedAt")
            SELECT unnest(@ids::uuid[]), @userId, unnest(@cardIds::uuid[]), 'Paper',
                   unnest(@quantities::int[]), unnest(@foilQty::int[]), 0, now(), now()
            ON CONFLICT ("UserId", "CardId", "Platform") DO UPDATE
                SET "Quantity"     = "CollectionEntries"."Quantity"     + EXCLUDED."Quantity",
                    "FoilQuantity" = "CollectionEntries"."FoilQuantity" + EXCLUDED."FoilQuantity",
                    "UpdatedAt"    = now()
            """;

        const string replaceSql = """
            INSERT INTO "CollectionEntries"
                ("Id", "UserId", "CardId", "Platform", "Quantity", "FoilQuantity", "EtchedQuantity", "CreatedAt", "UpdatedAt")
            SELECT unnest(@ids::uuid[]), @userId, unnest(@cardIds::uuid[]), 'Paper',
                   unnest(@quantities::int[]), unnest(@foilQty::int[]), 0, now(), now()
            """;

        var insertSql = importMode == ImportMode.Accumulate ? accumulateSql : replaceSql;

        for (int batchIndex = 0; batchIndex < totalBatches; batchIndex++)
        {
            ct.ThrowIfCancellationRequested();

            var batch = matched.Skip(batchIndex * BatchSize).Take(BatchSize).ToList();

            // Build PostgreSQL array literals as strings — works with any DbCommand implementation
            var newIds    = string.Join(",", batch.Select(_ => Guid.NewGuid()));
            var cardIds   = string.Join(",", batch.Select(r => r.CardId));
            var quantities = string.Join(",", batch.Select(r => r.Qty));
            var foilQtys  = string.Join(",", batch.Select(r => r.FoilQty));

            using var cmd = conn.CreateCommand();
            cmd.CommandText = insertSql;
            AddStringParam(cmd, "ids",        $"{{{newIds}}}");
            AddStringParam(cmd, "userId",     job.UserId);
            AddStringParam(cmd, "cardIds",    $"{{{cardIds}}}");
            AddStringParam(cmd, "quantities", $"{{{quantities}}}");
            AddStringParam(cmd, "foilQty",    $"{{{foilQtys}}}");
            await cmd.ExecuteNonQueryAsync(ct);

            // Update progress in DB so the polling endpoint reflects real progress
            job.Progress = (int)Math.Round((batchIndex + 1) * 100.0 / totalBatches);
            job.UpdatedAt = DateTime.UtcNow;
            await db.SaveChangesAsync(ct);
        }
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    private static void AddStringParam(System.Data.Common.DbCommand cmd, string name, string value)
    {
        var p = cmd.CreateParameter();
        p.ParameterName = name;
        p.Value = value;
        cmd.Parameters.Add(p);
    }
}
