using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MTGCollectionTracker.Api.Controllers;
using MTGCollectionTracker.Data;
using MTGCollectionTracker.Data.Entities;
using MTGCollectionTracker.Shared.DTOs.Collections;
using MTGCollectionTracker.Shared.Enums;
using MTGCollectionTracker.Api.Tests.Infrastructure;
using Shouldly;
using Testcontainers.PostgreSql;
using DataPlatform = MTGCollectionTracker.Data.Entities.Platform;
using SharedPlatform = MTGCollectionTracker.Shared.Enums.Platform;

namespace MTGCollectionTracker.Api.Tests.Integration;

/// <summary>
/// Integration tests for CollectionsController backed by a real PostgreSQL instance
/// (started by IntegrationTestSetup via Testcontainers).
///
/// Purpose: catch bugs that the in-memory EF provider cannot reproduce.
/// Known example (Feb 2026): casting (SharedPlatform)ce.Platform inside a LINQ-to-SQL
/// Select caused Npgsql to emit CAST("Platform" AS integer), which Postgres rejected
/// with error 22P02. All 12 unit tests passed; the endpoint was broken in production.
///
/// These tests do NOT replace the unit tests — they cover different failure modes.
/// Unit tests remain the primary tool for fast logic validation.
///
/// Isolation strategy:
/// - [DoNotParallelize] prevents concurrent TRUNCATE races with other integration tests.
/// - [TestInitialize] truncates all data before each test and creates a fresh test user,
///   so each test starts from a known clean state.
/// - A real ApplicationUser row is inserted because CollectionEntry has a genuine FK to
///   AspNetUsers — something the in-memory provider never enforced.
/// </summary>
[DoNotParallelize]
[TestClass]
public class CollectionsIntegrationTests
{
    private static PostgreSqlContainer _container = null!;

    private AppDbContext _dbContext = null!;
    private CollectionsController _controller = null!;

    private const string TestUserId = "test-user-integration-collections";

    [ClassInitialize]
    public static async Task ClassInitialize(TestContext _)
    {
        _container = new PostgreSqlBuilder("postgres:17-alpine").Build();
        await _container.StartAsync();
        await using var ctx = IntegrationTestSetup.CreateDbContext(_container.GetConnectionString());
        await ctx.Database.EnsureCreatedAsync();
    }

    [ClassCleanup]
    public static async Task ClassCleanup() => await _container.DisposeAsync();

    [TestInitialize]
    public async Task SetUp()
    {
        _dbContext = IntegrationTestSetup.CreateDbContext(_container.GetConnectionString());

        // Truncate all data from previous test run.
        // CASCADE handles FK-dependent tables (CollectionEntries, RefreshTokens, Identity tables).
        await _dbContext.Database.ExecuteSqlRawAsync(
            @"TRUNCATE TABLE ""AspNetUsers"", ""Cards"" CASCADE");

        // Create the test user. CollectionEntry has a real FK to AspNetUsers in Postgres —
        // unlike in-memory EF which silently ignores FK constraints.
        _dbContext.Users.Add(new ApplicationUser
        {
            Id = TestUserId,
            UserName = "integration_collections_user",
            NormalizedUserName = "INTEGRATION_COLLECTIONS_USER",
            Email = "collections@integration.test",
            NormalizedEmail = "COLLECTIONS@INTEGRATION.TEST",
            SecurityStamp = Guid.NewGuid().ToString(),
            ConcurrencyStamp = Guid.NewGuid().ToString()
        });
        await _dbContext.SaveChangesAsync();

        _controller = new CollectionsController(_dbContext);
        SetControllerUser(_controller, TestUserId);
    }

    [TestCleanup]
    public void TearDown()
    {
        _dbContext.Dispose();
    }

    // ── Platform enum tests ───────────────────────────────────────────────────

    /// <summary>
    /// Regression test for the February 2026 production bug.
    ///
    /// The in-memory EF provider performs casts in .NET, so it silently accepted
    /// (SharedPlatform)ce.Platform inside a LINQ Select. Npgsql translates this
    /// to CAST("Platform" AS integer) in SQL, which Postgres rejects with error
    /// 22P02 because Platform is stored as a string ("Paper", "Arena", "Mtgo").
    ///
    /// The fix projects via an anonymous type (SQL handles string→DataPlatform),
    /// then casts DataPlatform→SharedPlatform in memory after ToListAsync.
    /// This test confirms the fix is correct and the platform values survive
    /// the full round-trip through real Postgres.
    /// </summary>
    [TestMethod]
    [Description("Regression: Platform string→enum round-trip through Npgsql must not emit CAST(Platform AS integer).")]
    public async Task GetCollection_AllPlatforms_PlatformEnumRoundTripsThroughNpgsql()
    {
        // Arrange — one entry per platform
        var card = await SeedCardAsync("Lightning Bolt", "m21", "123");

        _dbContext.CollectionEntries.AddRange(
            MakeEntry(card.Id, TestUserId, DataPlatform.Paper, quantity: 4),
            MakeEntry(card.Id, TestUserId, DataPlatform.Arena, quantity: 1),
            MakeEntry(card.Id, TestUserId, DataPlatform.Mtgo, quantity: 2));
        await _dbContext.SaveChangesAsync();

        // Act — this call previously crashed Postgres with error 22P02
        var result = await _controller.GetCollection();

        // Assert
        var okResult = result.Result.ShouldBeOfType<OkObjectResult>();
        var response = okResult.Value.ShouldBeOfType<CollectionResponseDto>();

        response.TotalUniqueCards.ShouldBe(3);

        var platforms = response.Entries.Select(e => e.Platform).ToHashSet();
        platforms.ShouldContain(SharedPlatform.Paper, "Paper platform should survive the round-trip");
        platforms.ShouldContain(SharedPlatform.Arena, "Arena platform should survive the round-trip");
        platforms.ShouldContain(SharedPlatform.Mtgo, "Mtgo platform should survive the round-trip");
    }

    [TestMethod]
    public async Task GetCollection_WithPlatformFilter_ReturnsOnlyMatchingEntries()
    {
        // Arrange — two cards, each on a different platform
        var paperCard = await SeedCardAsync("Lightning Bolt", "m21", "123");
        var arenaCard = await SeedCardAsync("Counterspell", "m21", "46");

        _dbContext.CollectionEntries.AddRange(
            MakeEntry(paperCard.Id, TestUserId, DataPlatform.Paper, quantity: 2),
            MakeEntry(arenaCard.Id, TestUserId, DataPlatform.Arena, quantity: 1));
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _controller.GetCollection(platform: SharedPlatform.Arena);

        // Assert
        var okResult = result.Result.ShouldBeOfType<OkObjectResult>();
        var response = okResult.Value.ShouldBeOfType<CollectionResponseDto>();

        response.TotalUniqueCards.ShouldBe(1, "Only the Arena entry should be returned");
        response.Entries.ShouldAllBe(e => e.Platform == SharedPlatform.Arena);
    }

    // ── Upsert tests ─────────────────────────────────────────────────────────

    [TestMethod]
    public async Task AddToCollection_NewCard_Returns201AndPersistsEntry()
    {
        // Arrange
        var card = await SeedCardAsync("Lightning Bolt", "m21", "123");

        // Act
        var result = await _controller.AddToCollection(new AddToCollectionRequest
        {
            CardId = card.Id,
            Platform = SharedPlatform.Paper,
            Quantity = 3,
            FoilQuantity = 0
        });

        // Assert — 201 for new entries
        result.Result.ShouldBeOfType<CreatedAtActionResult>();

        // Confirm the entry was actually written to Postgres
        var dbEntry = await _dbContext.CollectionEntries
            .FirstOrDefaultAsync(ce => ce.CardId == card.Id && ce.UserId == TestUserId);
        dbEntry.ShouldNotBeNull();
        dbEntry!.Quantity.ShouldBe(3);
        dbEntry.Platform.ShouldBe(DataPlatform.Paper);
    }

    [TestMethod]
    public async Task AddToCollection_ExistingCard_AccumulatesQuantitiesAndReturns200()
    {
        // Arrange
        var card = await SeedCardAsync("Lightning Bolt", "m21", "123");

        await _controller.AddToCollection(new AddToCollectionRequest
        {
            CardId = card.Id,
            Platform = SharedPlatform.Paper,
            Quantity = 2,
            FoilQuantity = 1
        });

        // Act — add more copies of the same card on the same platform
        var result = await _controller.AddToCollection(new AddToCollectionRequest
        {
            CardId = card.Id,
            Platform = SharedPlatform.Paper,
            Quantity = 1,
            FoilQuantity = 2
        });

        // Assert — 200 (not 201) for accumulation
        result.Result.ShouldBeOfType<OkObjectResult>();

        // Only one row should exist (upsert, not duplicate insert)
        var entries = await _dbContext.CollectionEntries
            .Where(ce => ce.CardId == card.Id && ce.UserId == TestUserId)
            .ToListAsync();
        entries.Count.ShouldBe(1, "Upsert must not create a second row");
        entries[0].Quantity.ShouldBe(3, "2 + 1 = 3 nonfoil");
        entries[0].FoilQuantity.ShouldBe(3, "1 + 2 = 3 foil");
    }

    [TestMethod]
    public async Task AddToCollection_SameCardDifferentPlatforms_CreatesSeparateEntries()
    {
        // Arrange
        var card = await SeedCardAsync("Lightning Bolt", "m21", "123");

        // Act — same card, two different platforms
        await _controller.AddToCollection(new AddToCollectionRequest
        {
            CardId = card.Id,
            Platform = SharedPlatform.Paper,
            Quantity = 4
        });
        await _controller.AddToCollection(new AddToCollectionRequest
        {
            CardId = card.Id,
            Platform = SharedPlatform.Arena,
            Quantity = 1
        });

        // Assert — separate rows per platform (no upsert across platforms)
        var entries = await _dbContext.CollectionEntries
            .Where(ce => ce.CardId == card.Id && ce.UserId == TestUserId)
            .ToListAsync();
        entries.Count.ShouldBe(2, "Paper and Arena should be separate entries");
    }

    // ── Delete tests ──────────────────────────────────────────────────────────

    [TestMethod]
    public async Task DeleteCollectionEntry_OwnEntry_Returns204AndRemovesFromDb()
    {
        // Arrange
        var card = await SeedCardAsync("Lightning Bolt", "m21", "123");
        var entry = MakeEntry(card.Id, TestUserId, DataPlatform.Paper, quantity: 1);
        _dbContext.CollectionEntries.Add(entry);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _controller.DeleteCollectionEntry(entry.Id);

        // Assert
        result.ShouldBeOfType<NoContentResult>();

        var stillExists = await _dbContext.CollectionEntries.AnyAsync(ce => ce.Id == entry.Id);
        stillExists.ShouldBeFalse("The entry should be removed from the database");
    }

    [TestMethod]
    [Description("Security: deleting another user's entry must return 404 without revealing its existence.")]
    public async Task DeleteCollectionEntry_AnotherUsersEntry_Returns404AndLeavesEntryIntact()
    {
        // Arrange — create a second user and an entry they own
        const string otherUserId = "other-user-integration-collections";
        _dbContext.Users.Add(new ApplicationUser
        {
            Id = otherUserId,
            UserName = "other_user",
            SecurityStamp = Guid.NewGuid().ToString(),
            ConcurrencyStamp = Guid.NewGuid().ToString()
        });
        var card = await SeedCardAsync("Counterspell", "m21", "46");
        var otherEntry = MakeEntry(card.Id, otherUserId, DataPlatform.Paper, quantity: 2);
        _dbContext.CollectionEntries.Add(otherEntry);
        await _dbContext.SaveChangesAsync();

        // Act — attempt to delete as the primary test user (not the owner)
        var result = await _controller.DeleteCollectionEntry(otherEntry.Id);

        // Assert — 404, not 403 (no existence leakage)
        result.ShouldBeOfType<NotFoundObjectResult>();

        // Verify the entry was NOT deleted from the database
        var stillExists = await _dbContext.CollectionEntries.AnyAsync(ce => ce.Id == otherEntry.Id);
        stillExists.ShouldBeTrue("Another user's entry must not be deleted");
    }

    // ── Update tests ──────────────────────────────────────────────────────────

    [TestMethod]
    [Description("UpdateCollectionEntry uses absolute values, not incremental. Verifies against real Postgres.")]
    public async Task UpdateCollectionEntry_SetsAbsoluteQuantity_NotIncremental()
    {
        // Arrange
        var card = await SeedCardAsync("Lightning Bolt", "m21", "123");
        var entry = MakeEntry(card.Id, TestUserId, DataPlatform.Paper, quantity: 3, foilQuantity: 0);
        _dbContext.CollectionEntries.Add(entry);
        await _dbContext.SaveChangesAsync();

        // Act — set to 1 (absolute), not add 1
        var result = await _controller.UpdateCollectionEntry(entry.Id, new UpdateCollectionEntryRequest
        {
            Quantity = 1,
            FoilQuantity = 0
        });

        // Assert
        result.Result.ShouldBeOfType<OkObjectResult>();

        // Re-read from Postgres to confirm the persisted value (not just the in-memory tracked entity)
        await _dbContext.Entry(entry).ReloadAsync();
        entry.Quantity.ShouldBe(1, "Quantity should be 1 (absolute), not 4 (3 + 1)");
    }

    [TestMethod]
    public async Task UpdateCollectionEntry_AnotherUsersEntry_Returns404()
    {
        // Arrange
        const string otherUserId = "other-user-update-collections";
        _dbContext.Users.Add(new ApplicationUser
        {
            Id = otherUserId,
            UserName = "other_user_update",
            SecurityStamp = Guid.NewGuid().ToString(),
            ConcurrencyStamp = Guid.NewGuid().ToString()
        });
        var card = await SeedCardAsync("Island", "m21", "1");
        var otherEntry = MakeEntry(card.Id, otherUserId, DataPlatform.Paper, quantity: 1);
        _dbContext.CollectionEntries.Add(otherEntry);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _controller.UpdateCollectionEntry(otherEntry.Id, new UpdateCollectionEntryRequest
        {
            Quantity = 99,
            FoilQuantity = 0
        });

        // Assert
        result.Result.ShouldBeOfType<NotFoundObjectResult>();

        await _dbContext.Entry(otherEntry).ReloadAsync();
        otherEntry.Quantity.ShouldBe(1, "Another user's entry must not be modified");
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    private static void SetControllerUser(CollectionsController controller, string userId)
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId)
        }));
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = principal }
        };
    }

    private async Task<Card> SeedCardAsync(string name, string setCode, string collectorNumber)
    {
        var card = new Card
        {
            Id = Guid.NewGuid(),
            ScryfallId = Guid.NewGuid(),
            OracleId = Guid.NewGuid(),
            Name = name,
            SetCode = setCode,
            CollectorNumber = collectorNumber,
            Rarity = "common",
            TypeLine = "Instant",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _dbContext.Cards.Add(card);
        await _dbContext.SaveChangesAsync();
        return card;
    }

    private static CollectionEntry MakeEntry(
        Guid cardId,
        string userId,
        DataPlatform platform,
        int quantity = 1,
        int foilQuantity = 0) => new()
    {
        Id = Guid.NewGuid(),
        UserId = userId,
        CardId = cardId,
        Platform = platform,
        Quantity = quantity,
        FoilQuantity = foilQuantity,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
    };
}
