using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MTGCollectionTracker.Api.Services;
using MTGCollectionTracker.Data;
using MTGCollectionTracker.Data.Entities;
using MTGCollectionTracker.Shared;
using MTGCollectionTracker.Shared.DTOs.Collections;

namespace MTGCollectionTracker.Api.Controllers;

/// <summary>
/// Endpoints for submitting and polling background import jobs.
///
/// Flow:
///   POST /api/imports/manabox   → validate, persist ImportJob (Pending), enqueue → 202 Accepted
///   GET  /api/imports/{id}/status → poll until Completed or Failed
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ImportsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IImportJobQueue _queue;

    public ImportsController(AppDbContext db, IImportJobQueue queue)
    {
        _db = db;
        _queue = queue;
    }

    /// <summary>
    /// Submit a Manabox CSV for background import. Returns 202 Accepted immediately with a job ID.
    /// The client should poll <c>GET /api/imports/{jobId}/status</c> for progress.
    /// </summary>
    /// <param name="file">The Manabox CSV export file.</param>
    /// <param name="mode">Import mode: <c>Accumulate</c> (add to existing) or <c>Replace</c> (clear Paper first).</param>
    /// <param name="includedBinders">JSON array of binder names to import. Omit or pass empty array to import all.</param>
    [HttpPost("manabox")]
    [RequestSizeLimit(52_428_800)] // 50 MB
    [ProducesResponseType(typeof(ImportJobAcceptedDto), StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> SubmitManaboxImport(
        IFormFile file,
        [FromForm] string mode,
        [FromForm] string? includedBinders = null)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        if (file is null || file.Length == 0)
            return BadRequest("A non-empty CSV file is required.");

        if (!file.FileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
            return BadRequest("Only .csv files are accepted.");

        if (!Enum.TryParse<ImportMode>(mode, ignoreCase: true, out var importMode))
            return BadRequest($"Invalid mode '{mode}'. Accepted values: {string.Join(", ", Enum.GetNames<ImportMode>())}");

        // Buffer the whole file up-front so the background worker doesn't race the request stream.
        byte[] csvBytes;
        using (var ms = new MemoryStream((int)file.Length))
        {
            await file.CopyToAsync(ms);
            csvBytes = ms.ToArray();
        }

        var binderList = string.IsNullOrWhiteSpace(includedBinders)
            ? new List<string>()
            : JsonSerializer.Deserialize<List<string>>(includedBinders) ?? new List<string>();

        // Persist BEFORE enqueuing — guarantees the record exists even if the server
        // restarts between Enqueue() and the worker's first ReadAsync().
        var job = new ImportJob
        {
            Id                  = Guid.NewGuid(),
            UserId              = userId,
            Status              = ImportJobStatus.Pending,
            Progress            = 0,
            FileName            = file.FileName,
            Mode                = importMode.ToString(),
            IncludedBindersJson = JsonSerializer.Serialize(binderList),
            CsvBytes            = csvBytes,
            CreatedAt           = DateTime.UtcNow,
            UpdatedAt           = DateTime.UtcNow,
        };

        _db.ImportJobs.Add(job);
        await _db.SaveChangesAsync();

        _queue.Enqueue(job.Id);

        var statusUrl = ApiRoutes.ImportsStatus(job.Id);
        return Accepted(statusUrl, new ImportJobAcceptedDto
        {
            JobId     = job.Id,
            StatusUrl = statusUrl,
        });
    }

    /// <summary>
    /// Poll for the status of an import job.
    /// Returns 200 OK with current progress (0–100) and, once completed, the final result.
    /// Returns 404 if the job does not belong to the authenticated user.
    /// </summary>
    [HttpGet("{jobId:guid}/status")]
    [ProducesResponseType(typeof(ImportJobStatusDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ImportJobStatusDto>> GetStatus(Guid jobId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        // AsNoTracking bypasses the EF identity map so the status endpoint always
        // reflects what the worker last committed — same request sees fresh data.
        var job = await _db.ImportJobs
            .AsNoTracking()
            .FirstOrDefaultAsync(j => j.Id == jobId);
        if (job is null || job.UserId != userId)
            return NotFound();

        ManaboxImportResultDto? result = null;
        if (job.Status == ImportJobStatus.Completed)
        {
            var skippedCards = string.IsNullOrEmpty(job.SkippedCardsJson)
                ? new List<string>()
                : JsonSerializer.Deserialize<List<string>>(job.SkippedCardsJson) ?? new List<string>();

            result = new ManaboxImportResultDto
            {
                Imported     = job.Imported,
                Updated      = job.Updated,
                Skipped      = job.Skipped,
                TotalCopies  = job.TotalCopies,
                SkippedCards = skippedCards,
            };
        }

        return Ok(new ImportJobStatusDto
        {
            JobId    = job.Id,
            Status   = job.Status.ToString(),
            Progress = job.Progress,
            Result   = result,
            Error    = job.Status == ImportJobStatus.Failed ? job.ErrorMessage : null,
        });
    }
}
