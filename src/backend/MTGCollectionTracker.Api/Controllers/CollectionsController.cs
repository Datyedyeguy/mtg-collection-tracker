using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MTGCollectionTracker.Data;
using MTGCollectionTracker.Data.Entities;
using MTGCollectionTracker.Shared.DTOs.Collections;
using MTGCollectionTracker.Shared.Enums;

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

        // Get paginated entries with only necessary columns (projection)
        // This generates SQL SELECT with specific columns, not SELECT *
        var entryDtos = await baseQuery
            .OrderBy(ce => ce.Card.Name)
            .ThenBy(ce => ce.Card.SetCode)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(ce => new CollectionEntryDto
            {
                Id = ce.Id,
                CardId = ce.CardId,
                CardName = ce.Card.Name,
                SetCode = ce.Card.SetCode,
                CollectorNumber = ce.Card.CollectorNumber,
                Platform = (MTGCollectionTracker.Shared.Enums.Platform)ce.Platform,
                Quantity = ce.Quantity,
                AcquiredDate = ce.AcquiredDate,
                CreatedAt = ce.CreatedAt
            })
            .ToListAsync();

        // Calculate cards by platform (for full collection, not just this page)
        var cardsByPlatform = await _dbContext.CollectionEntries
            .Where(ce => ce.UserId == userId)
            .GroupBy(ce => ce.Platform)
            .Select(g => new
            {
                Platform = g.Key,
                Total = g.Sum(ce => ce.Quantity)
            })
            .ToListAsync();

        var response = new CollectionResponseDto
        {
            Entries = entryDtos,
            TotalCards = totalCards,
            TotalUniqueCards = totalUniqueCards,
            CardsByPlatform = cardsByPlatform.ToDictionary(
                x => (MTGCollectionTracker.Shared.Enums.Platform)x.Platform,
                x => x.Total),
            CurrentPage = page,
            PageSize = pageSize,
            TotalPages = totalPages
        };

        return Ok(response);
    }
}
