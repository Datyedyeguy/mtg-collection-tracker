using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MTGCollectionTracker.Api.Helpers;
using MTGCollectionTracker.Data;
using MTGCollectionTracker.Data.Entities;
using MTGCollectionTracker.Shared.DTOs.Collections;
using MTGCollectionTracker.Shared.Enums;
using DataPlatform = MTGCollectionTracker.Data.Entities.Platform;
using SharedPlatform = MTGCollectionTracker.Shared.Enums.Platform;

namespace MTGCollectionTracker.Api.Controllers;

/// <summary>
/// Endpoints for managing user card collections across platforms.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CollectionsController : ControllerBase
{
    private readonly AppDbContext _dbContext;

    public CollectionsController(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <summary>
    /// Get the user's collection, optionally filtered by platform with pagination.
    /// </summary>
    /// <param name="platform">Optional platform filter (Paper, Arena, Mtgo)</param>
    /// <param name="page">Page number (1-based, default: 1)</param>
    /// <param name="pageSize">Items per page (default: 50, max: 100)</param>
    /// <returns>Collection entries with metadata and pagination info</returns>
    [HttpGet]
    [ProducesResponseType(typeof(CollectionResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CollectionResponseDto>> GetCollection(
        [FromQuery] MTGCollectionTracker.Shared.Enums.Platform? platform = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        // Validate pagination parameters
        if (page < 1)
        {
            return BadRequest("Page number must be at least 1");
        }

        if (pageSize < 1 || pageSize > 100)
        {
            return BadRequest("Page size must be between 1 and 100");
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        // Base query for this user's collection
        var baseQuery = _dbContext.CollectionEntries
            .Where(ce => ce.UserId == userId);

        // Apply platform filter if specified
        if (platform.HasValue)
        {
            baseQuery = baseQuery.Where(ce => ce.Platform == (MTGCollectionTracker.Data.Entities.Platform)platform.Value);
        }

        // Calculate totals using database aggregation (efficient for large datasets)
        var totalUniqueCards = await baseQuery.CountAsync();
        var totalCards = await baseQuery.SumAsync(ce => ce.Quantity);

        // Calculate pagination metadata
        var totalPages = (int)Math.Ceiling(totalUniqueCards / (double)pageSize);

        // ⚠️ Platform is stored as a string in PostgreSQL (HasConversion<string>()).
        // Casting (SharedPlatform)ce.Platform inside a LINQ-to-SQL Select causes EF/Npgsql
        // to emit CAST("Platform" AS integer), which Postgres rejects with error 22P02.
        // Fix: project to an anonymous type via SQL (EF handles string→DataPlatform),
        // then cast DataPlatform→SharedPlatform in memory after ToListAsync.
        var rawEntries = await baseQuery
            .OrderBy(ce => ce.Card.Name)
            .ThenBy(ce => ce.Card.SetCode)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(ce => new
            {
                ce.Id,
                ce.CardId,
                CardName = ce.Card.Name,
                SetCode = ce.Card.SetCode,
                CollectorNumber = ce.Card.CollectorNumber,
                ce.Platform,
                ce.Quantity,
                ce.FoilQuantity,
                ce.AcquiredDate,
                ce.CreatedAt,
                ImageUris = ce.Card.ImageUris,
                Faces = ce.Card.Faces
            })
            .ToListAsync();

        var entryDtos = rawEntries
            .Select(ce => new CollectionEntryDto
            {
                Id = ce.Id,
                CardId = ce.CardId,
                CardName = ce.CardName,
                SetCode = ce.SetCode,
                CollectorNumber = ce.CollectorNumber,
                Platform = (SharedPlatform)ce.Platform, // safe: in-memory cast
                Quantity = ce.Quantity,
                FoilQuantity = ce.FoilQuantity,
                ImageUri = CardImageHelper.ExtractImageUri(ce.ImageUris, ce.Faces),
                AcquiredDate = ce.AcquiredDate,
                CreatedAt = ce.CreatedAt
            })
            .ToList();

        // Cards-by-platform totals — GroupBy on Platform has the same cast issue,
        // so pull (Platform, Quantity) pairs from SQL and aggregate in memory.
        var platformData = await _dbContext.CollectionEntries
            .Where(ce => ce.UserId == userId)
            .Select(ce => new { ce.Platform, ce.Quantity })
            .ToListAsync();

        var cardsByPlatform = platformData
            .GroupBy(x => x.Platform)
            .ToDictionary(
                g => (SharedPlatform)g.Key,
                g => g.Sum(x => x.Quantity));

        var response = new CollectionResponseDto
        {
            Entries = entryDtos,
            TotalCards = totalCards,
            TotalUniqueCards = totalUniqueCards,
            CardsByPlatform = cardsByPlatform,
            CurrentPage = page,
            PageSize = pageSize,
            TotalPages = totalPages
        };

        return Ok(response);
    }

    /// <summary>
    /// Add a card to the user's collection with upsert semantics.
    /// If the user already owns this card on the same platform, quantities are accumulated.
    /// </summary>
    /// <param name="request">Card ID, platform, and quantities to add</param>
    /// <returns>201 Created for a new entry, 200 OK when quantities were added to an existing entry</returns>
    [HttpPost]
    [ProducesResponseType(typeof(CollectionEntryDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(CollectionEntryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CollectionEntryDto>> AddToCollection([FromBody] AddToCollectionRequest request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        // Validate quantities
        if (request.Quantity < 0 || request.FoilQuantity < 0)
        {
            return BadRequest("Quantities cannot be negative.");
        }

        if (request.Quantity == 0 && request.FoilQuantity == 0)
        {
            return BadRequest("At least one quantity (Quantity or FoilQuantity) must be greater than zero.");
        }

        // Verify the card exists
        var cardExists = await _dbContext.Cards.AnyAsync(c => c.Id == request.CardId);
        if (!cardExists)
        {
            return NotFound($"Card {request.CardId} not found.");
        }

        var dataPlatform = (DataPlatform)request.Platform;

        // Upsert: find existing entry for this user+card+platform combination
        var existing = await _dbContext.CollectionEntries
            .FirstOrDefaultAsync(ce =>
                ce.UserId == userId &&
                ce.CardId == request.CardId &&
                ce.Platform == dataPlatform);

        CollectionEntryDto entryDto;
        bool isNew;

        if (existing != null)
        {
            // Accumulate quantities onto the existing entry
            existing.Quantity += request.Quantity;
            existing.FoilQuantity += request.FoilQuantity;
            existing.UpdatedAt = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync();

            entryDto = ToDto(existing);
            isNew = false;
        }
        else
        {
            var entry = new CollectionEntry
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                CardId = request.CardId,
                Platform = dataPlatform,
                Quantity = request.Quantity,
                FoilQuantity = request.FoilQuantity,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _dbContext.CollectionEntries.Add(entry);
            await _dbContext.SaveChangesAsync();

            // Reload with card navigation to populate CardName etc.
            await _dbContext.Entry(entry).Reference(e => e.Card).LoadAsync();

            entryDto = ToDto(entry);
            isNew = true;
        }

        if (isNew)
        {
            return CreatedAtAction(nameof(AddToCollection), entryDto);
        }

        return Ok(entryDto);
    }

    /// <summary>
    /// Get the authenticated user's ownership of a specific card across all platforms.
    /// </summary>
    /// <param name="cardId">The card's database ID</param>
    /// <returns>List of entries (one per platform where the user owns the card); empty if not owned</returns>
    [HttpGet("card/{cardId:guid}")]
    [ProducesResponseType(typeof(List<CollectionEntryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<List<CollectionEntryDto>>> GetCardOwnership(Guid cardId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var cardExists = await _dbContext.Cards.AnyAsync(c => c.Id == cardId);
        if (!cardExists)
        {
            return NotFound($"Card {cardId} not found.");
        }

        // ⚠️ Same Platform cast issue as GetCollection — project via SQL to anonymous type,
        // then cast in memory. See GetCollection for full explanation.
        var rawEntries = await _dbContext.CollectionEntries
            .Where(ce => ce.UserId == userId && ce.CardId == cardId)
            .OrderBy(ce => ce.Platform)
            .Select(ce => new
            {
                ce.Id,
                ce.CardId,
                CardName = ce.Card.Name,
                SetCode = ce.Card.SetCode,
                CollectorNumber = ce.Card.CollectorNumber,
                ce.Platform,
                ce.Quantity,
                ce.FoilQuantity,
                ce.AcquiredDate,
                ce.CreatedAt
            })
            .ToListAsync();

        var entries = rawEntries
            .Select(ce => new CollectionEntryDto
            {
                Id = ce.Id,
                CardId = ce.CardId,
                CardName = ce.CardName,
                SetCode = ce.SetCode,
                CollectorNumber = ce.CollectorNumber,
                Platform = (SharedPlatform)ce.Platform, // safe: in-memory cast
                Quantity = ce.Quantity,
                FoilQuantity = ce.FoilQuantity,
                AcquiredDate = ce.AcquiredDate,
                CreatedAt = ce.CreatedAt
            })
            .ToList();

        return Ok(entries);
    }

    /// <summary>
    /// Update the quantities of an existing collection entry.
    /// Uses absolute values — the entry's quantities are set to the provided values, not incremented.
    /// </summary>
    /// <param name="id">The collection entry ID</param>
    /// <param name="request">New quantity and foil quantity (absolute values)</param>
    /// <returns>200 OK with the updated entry, 404 if not found, 400 for invalid quantities</returns>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(CollectionEntryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CollectionEntryDto>> UpdateCollectionEntry(Guid id, [FromBody] UpdateCollectionEntryRequest request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        // Validate quantities
        if (request.Quantity < 0 || request.FoilQuantity < 0)
        {
            return BadRequest("Quantities cannot be negative.");
        }

        if (request.Quantity == 0 && request.FoilQuantity == 0)
        {
            return BadRequest("At least one quantity must be greater than zero. Use DELETE to remove the entry.");
        }

        // Find the entry — scoped to the authenticated user (returns 404 for other users' entries)
        var entry = await _dbContext.CollectionEntries
            .Include(ce => ce.Card)
            .FirstOrDefaultAsync(ce => ce.Id == id && ce.UserId == userId);

        if (entry is null)
        {
            return NotFound($"Collection entry {id} not found.");
        }

        entry.Quantity = request.Quantity;
        entry.FoilQuantity = request.FoilQuantity;
        entry.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync();

        return Ok(ToDto(entry));
    }

    /// <summary>
    /// Remove a card from the user's collection.
    /// </summary>
    /// <param name="id">The collection entry ID</param>
    /// <returns>204 No Content on success, 404 if not found</returns>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteCollectionEntry(Guid id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        // Find the entry — scoped to the authenticated user (returns 404 for other users' entries)
        var entry = await _dbContext.CollectionEntries
            .FirstOrDefaultAsync(ce => ce.Id == id && ce.UserId == userId);

        if (entry is null)
        {
            return NotFound($"Collection entry {id} not found.");
        }

        _dbContext.CollectionEntries.Remove(entry);
        await _dbContext.SaveChangesAsync();

        return NoContent();
    }

    // ── Private helpers ────────────────────────────────────────────────

    private static CollectionEntryDto ToDto(CollectionEntry entry) => new()
    {
        Id = entry.Id,
        CardId = entry.CardId,
        CardName = entry.Card?.Name ?? string.Empty,
        SetCode = entry.Card?.SetCode ?? string.Empty,
        CollectorNumber = entry.Card?.CollectorNumber ?? string.Empty,
        Platform = (SharedPlatform)entry.Platform,
        Quantity = entry.Quantity,
        FoilQuantity = entry.FoilQuantity,
        ImageUri = CardImageHelper.ExtractImageUri(entry.Card?.ImageUris, entry.Card?.Faces),
        AcquiredDate = entry.AcquiredDate,
        CreatedAt = entry.CreatedAt
    };
}
