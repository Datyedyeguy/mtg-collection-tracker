using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MTGCollectionTracker.Api.Controllers;
using MTGCollectionTracker.Data;
using MTGCollectionTracker.Data.Entities;
using MTGCollectionTracker.Api.Tests.Infrastructure;
using MTGCollectionTracker.Shared.DTOs.Cards;
using Shouldly;
using Testcontainers.PostgreSql;

namespace MTGCollectionTracker.Api.Tests.Integration;

/// <summary>
/// Integration tests for CardsController backed by a real PostgreSQL instance
/// (started by IntegrationTestSetup via Testcontainers).
///
/// Purpose: catch bugs that the in-memory EF provider cannot reproduce.
/// Known example: GroupBy on UUID with MIN aggregate is not supported in Postgres.
/// The two-step deduplication query in SearchCards exists precisely because
/// MIN(Id) on a uuid column fails in Postgres but silently works in-memory.
///
/// JSONB column round-trips are also tested here because in-memory EF stores
/// string columns as plain strings without any JSON validation, masking issues
/// that would surface when Postgres validates or re-serializes JSONB values.
///
/// Isolation strategy: same as CollectionsIntegrationTests.
/// [DoNotParallelize] prevents TRUNCATE races; [TestInitialize] clears all data
/// before each test via TRUNCATE CASCADE.
/// </summary>
[DoNotParallelize]
[TestClass]
public class CardsIntegrationTests
{
    private static PostgreSqlContainer _container = null!;

    private AppDbContext _dbContext = null!;
    private CardsController _controller = null!;

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

        // Clear only the Cards table — no users or collection entries needed for card tests.
        // CASCADE handles anything that references Cards (CollectionEntries, etc.).
        await _dbContext.Database.ExecuteSqlRawAsync(
            @"TRUNCATE TABLE ""Cards"" CASCADE");

        _controller = new CardsController(_dbContext);
    }

    [TestCleanup]
    public void TearDown()
    {
        _dbContext.Dispose();
    }

    // ── JSONB column round-trip tests ─────────────────────────────────────────

    [TestMethod]
    [Description("ImageUris is stored as jsonb in Postgres. Verify the 'normal' URL survives the round-trip.")]
    public async Task GetCard_WithImageUris_ExtractsNormalUrlFromJsonbColumn()
    {
        // Arrange — card with a real-looking ImageUris JSON object
        const string expectedUrl = "https://cards.scryfall.io/normal/front/test-card.jpg";
        var imageUrisJson = JsonSerializer.Serialize(new
        {
            small = "https://cards.scryfall.io/small/front/test-card.jpg",
            normal = expectedUrl,
            large = "https://cards.scryfall.io/large/front/test-card.jpg"
        });

        var card = await SeedCardAsync("Lightning Bolt", "m21", "123", imageUrisJson: imageUrisJson);

        // Act
        var result = await _controller.GetCard(card.Id);

        // Assert
        var okResult = result.Result.ShouldBeOfType<OkObjectResult>();
        var dto = okResult.Value.ShouldBeOfType<CardDetailDto>();
        dto.ImageUri.ShouldBe(expectedUrl, "The 'normal' URL must survive the jsonb round-trip");
    }

    [TestMethod]
    [Description("Faces is stored as jsonb (serialized List<CardFaceDto>). Verify IsMultiFaced and face data survive the round-trip.")]
    public async Task GetCard_WithFacesJson_DeserializesFacesFromJsonbColumn()
    {
        // Arrange — a transform card with two faces (no top-level ImageUris)
        var faces = new List<CardFaceDto>
        {
            new()
            {
                Name = "Delver of Secrets",
                TypeLine = "Creature — Human Wizard",
                ManaCost = "{U}",
                OracleText = "At the beginning of your upkeep...",
                ImageUri = "https://cards.scryfall.io/normal/front/delver-front.jpg"
            },
            new()
            {
                Name = "Insectile Aberration",
                TypeLine = "Creature — Human Insect",
                ManaCost = null,
                OracleText = "Flying",
                ImageUri = "https://cards.scryfall.io/normal/front/delver-back.jpg"
            }
        };
        var facesJson = JsonSerializer.Serialize(faces);

        var card = await SeedCardAsync(
            "Delver of Secrets // Insectile Aberration", "isd", "51",
            imageUrisJson: null,  // transform cards have no top-level image
            facesJson: facesJson);

        // Act
        var result = await _controller.GetCard(card.Id);

        // Assert
        var okResult = result.Result.ShouldBeOfType<OkObjectResult>();
        var dto = okResult.Value.ShouldBeOfType<CardDetailDto>();

        dto.IsMultiFaced.ShouldBeTrue("Card with Faces JSON should be multi-faced");
        dto.Faces.ShouldNotBeNull();
        dto.Faces!.Count.ShouldBe(2);
        dto.Faces[0].Name.ShouldBe("Delver of Secrets");
        dto.Faces[1].Name.ShouldBe("Insectile Aberration");

        // Front-face image is extracted as the card's primary ImageUri when no top-level image exists
        dto.ImageUri.ShouldBe("https://cards.scryfall.io/normal/front/delver-front.jpg",
            "ImageUri should fall back to first face's ImageUri when card has no top-level ImageUris");
    }

    // ── Search deduplication tests ────────────────────────────────────────────

    /// <summary>
    /// Validates the two-step Oracle deduplication query in SearchCards runs correctly in Postgres.
    ///
    /// The comment in CardsController explains why this is necessary:
    /// "PostgreSQL has no MIN aggregate for uuid types."
    /// The in-memory provider works because Guid implements IComparable in .NET,
    /// but Postgres rejects a MIN(uuid_column) subquery.
    ///
    /// Fix: two queries — first collect OracleIds, then pick a representative Id
    /// per group in memory, then re-query by those Ids.
    /// </summary>
    [TestMethod]
    [Description("Regression: two-step Oracle deduplication must work in real Postgres (MIN on UUID fails there).")]
    public async Task SearchCards_WithMultiplePrintings_DeduplicatesToOneResultPerOracleId()
    {
        // Arrange — same card (same OracleId) printed in two different sets
        var oracleId = Guid.NewGuid();
        await SeedCardAsync("Lightning Bolt", "m21", "123", oracleId: oracleId);
        await SeedCardAsync("Lightning Bolt", "znr", "156", oracleId: oracleId);

        // Also seed an unrelated card to confirm it doesn't appear
        await SeedCardAsync("Counterspell", "m21", "46");

        // Act — search by name, deduplicated (default allPrintings=false)
        var result = await _controller.SearchCards(q: "Lightning Bolt");

        // Assert
        var okResult = result.Result.ShouldBeOfType<OkObjectResult>();
        var response = okResult.Value.ShouldBeOfType<CardSearchResponseDto>();

        response.TotalCards.ShouldBe(1, "Two printings of the same card must be deduplicated to one result");
        response.Cards.Count.ShouldBe(1);
        response.Cards[0].Name.ShouldBe("Lightning Bolt");
    }

    [TestMethod]
    public async Task SearchCards_WithAllPrintingsTrue_ReturnsAllPrintings()
    {
        // Arrange — same card in two sets
        var oracleId = Guid.NewGuid();
        await SeedCardAsync("Lightning Bolt", "m21", "123", oracleId: oracleId);
        await SeedCardAsync("Lightning Bolt", "znr", "156", oracleId: oracleId);

        // Act — allPrintings=true bypasses deduplication
        var result = await _controller.SearchCards(q: "Lightning Bolt", allPrintings: true);

        // Assert
        var okResult = result.Result.ShouldBeOfType<OkObjectResult>();
        var response = okResult.Value.ShouldBeOfType<CardSearchResponseDto>();

        response.TotalCards.ShouldBe(2, "With allPrintings=true, both printings should be returned");
    }

    [TestMethod]
    public async Task SearchCards_WithSetFilter_ReturnsOnlyCardsFromThatSet()
    {
        // Arrange — cards across two sets
        await SeedCardAsync("Lightning Bolt", "m21", "123");
        await SeedCardAsync("Counterspell", "m21", "46");
        await SeedCardAsync("Opt", "znr", "67");

        // Act — filter to m21 only
        var result = await _controller.SearchCards(set: "m21");

        // Assert
        var okResult = result.Result.ShouldBeOfType<OkObjectResult>();
        var response = okResult.Value.ShouldBeOfType<CardSearchResponseDto>();

        response.TotalCards.ShouldBe(2, "Only the two m21 cards should be returned");
        response.Cards.ShouldAllBe(c => c.SetCode == "m21");
    }

    [TestMethod]
    public async Task GetCard_NonExistentId_Returns404()
    {
        // Act
        var result = await _controller.GetCard(Guid.NewGuid());

        // Assert
        result.Result.ShouldBeOfType<NotFoundObjectResult>();
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    private async Task<Card> SeedCardAsync(
        string name,
        string setCode,
        string collectorNumber,
        Guid? oracleId = null,
        string? imageUrisJson = null,
        string? facesJson = null)
    {
        var card = new Card
        {
            Id = Guid.NewGuid(),
            ScryfallId = Guid.NewGuid(),
            OracleId = oracleId ?? Guid.NewGuid(),
            Name = name,
            SetCode = setCode,
            CollectorNumber = collectorNumber,
            Rarity = "common",
            TypeLine = "Instant",
            ImageUris = imageUrisJson,
            Faces = facesJson,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _dbContext.Cards.Add(card);
        await _dbContext.SaveChangesAsync();
        return card;
    }
}
